﻿using System;
using System.Web;
using System.Text.RegularExpressions;
using N2.Web.UI.WebControls;
using N2.Web;
using N2.Engine;

namespace N2.Edit
{
	/// <summary>
	/// Class used to supply plugins with contextual information.
	/// </summary>
	public class PluginContext
	{
		public PluginContext(ContentItem selected, ContentItem memorizedItem, ContentItem startItem, ContentItem rootItem, ControlPanelState state, 
			IEngine engine, HttpContextBase httpContext)
		{
			State = state;
			Selected = selected;
			Memorized = memorizedItem;
			Start = startItem;
			Root = rootItem;

			Engine = engine;
			HttpContext = httpContext;
		}

		public ControlPanelState State { get; set;}
		public ContentItem Selected { get; set; }
		public ContentItem Memorized { get; set; }
		public ContentItem Start { get; set; }
		public ContentItem Root { get; set; }
		public HttpContextBase HttpContext { get; set; }
		public IEngine Engine { get; set; }
		
		static readonly Regex expressionExpression = new Regex("{(?<expr>[^})]+)}");

		public string Format(string format, bool urlEncode)
		{
			format = Url.ResolveTokens(format).Replace("{selected}", "{Selected.Path}")
				.Replace("{memory}", "{Memorized.Path}")
				.Replace("{action}", "{Action}");

			return expressionExpression.Replace(format, delegate(Match m) { return Evaluate(m.Groups["expr"].Value, urlEncode); });
		}

		public string Rebase(string url)
		{
			if (String.IsNullOrEmpty(url))
				url = "empty.aspx";

			string rebasedUrl = Engine.ManagementPaths.ResolveResourceUrl(url);
			return rebasedUrl;
		}

		string Evaluate(string expression, bool urlEncode)
		{
			object value = Utility.Evaluate(this, expression);
			
			if (value == null)
				return null;

			if(urlEncode)
				return HttpUtility.UrlEncode(value.ToString());

			return value.ToString();
		}
	}
}
