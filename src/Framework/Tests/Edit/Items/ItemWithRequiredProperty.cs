using System;
using System.Collections.Generic;
using System.Text;

namespace N2.Tests.Edit.Items
{
	public class ItemWithRequiredProperty : N2.ContentItem
	{
		[N2.Details.EditableTextBox("Username", 10, Required = true, Validate = true, ValidationExpression = ".*")]
		public virtual string Username
		{
			get { return (string)(GetDetail("Username") ?? string.Empty); }
			set { SetDetail("Username", value); }
		}
	}
}
