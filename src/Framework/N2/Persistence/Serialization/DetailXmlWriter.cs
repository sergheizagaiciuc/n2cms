using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using N2.Details;
using System.Collections.Generic;

namespace N2.Persistence.Serialization
{
	public class DetailXmlWriter : IXmlWriter
	{
		string applicationPath = N2.Web.Url.ApplicationPath ?? "/";

		public virtual void Write(ContentItem item, XmlTextWriter writer)
		{
			using (new ElementWriter("details", writer))
			{
                foreach (ContentDetail detail in GetDetails(item))
				{
					WriteDetail(item, detail, writer);
				}
			}
		}

        protected virtual IEnumerable<ContentDetail> GetDetails(ContentItem item)
        {
            return item.Details;
        }

		public virtual void WriteDetail(ContentItem item, ContentDetail detail, XmlTextWriter writer)
		{
			using (ElementWriter detailElement = new ElementWriter("detail", writer))
			{
				detailElement.WriteAttribute("name", detail.Name);
				detailElement.WriteAttribute("typeName", SerializationUtility.GetTypeAndAssemblyName(detail.ValueType));
			
				if (detail.ValueType == typeof(object))
				{
					string base64representation = SerializationUtility.ToBase64String(detail.Value);
					detailElement.Write(base64representation);
				}
				else if (detail.ValueType == typeof(ContentItem))
				{
					detailElement.Write(detail.LinkValue.HasValue ? detail.LinkValue.Value.ToString() : "0");
				}
				else if (detail.ValueType == typeof(string))
				{
					string value = detail.StringValue;

					if (!string.IsNullOrEmpty(value))
					{
						if (value.StartsWith(applicationPath, StringComparison.InvariantCultureIgnoreCase))
						{
							var pi = item.GetContentType().GetProperty(detail.Name);
							if (pi != null)
							{
								var transformers = pi.GetCustomAttributes(typeof(IRelativityTransformer), false);
								foreach (IRelativityTransformer transformer in transformers)
								{
									if (transformer.RelativeWhen == RelativityMode.Always || transformer.RelativeWhen == RelativityMode.ImportingOrExporting)
										value = transformer.Rebase(value, applicationPath, "~/");
								}
							}
						}

						detailElement.WriteCData(value);
					}
				}
				else if(detail.ValueType == typeof(DateTime)) {
					detailElement.Write(ElementWriter.ToUniversalString(detail.DateTimeValue));
				}
				else {
					detailElement.Write(detail.Value.ToString());
				}
			}
		}
	}
}
