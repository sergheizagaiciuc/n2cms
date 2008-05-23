﻿using System;
using System.Collections.Generic;
using System.Text;
using N2.Persistence;
using N2.Collections;
using N2.Web;

namespace N2.Edit
{
	public class TreeSorter : ITreeSorter
	{
		IPersister persister;
		IEditManager editManager;
		IWebContext webContext;

		public TreeSorter(IPersister persister, IEditManager editManager, IWebContext webContext)
		{
			this.persister = persister;
			this.editManager = editManager;
			this.webContext = webContext;
		}

		#region ITreeSorter Members

		public void MoveUp(ContentItem item)
		{
			if (item.Parent != null)
			{
				ItemFilter filter = editManager.GetEditorFilter(webContext.User);
				IList<ContentItem> siblings = item.Parent.Children;
				IList<ContentItem> filtered = new ItemList(siblings, filter);

				int index = filtered.IndexOf(item);
				if (index > 0)
				{
					MoveTo(item, NodePosition.Before, filtered[index - 1]);
				}
			}
		}

		public void MoveDown(ContentItem item)
		{
			if (item.Parent != null)
			{
				ItemFilter filter = editManager.GetEditorFilter(webContext.User);
				IList<ContentItem> siblings = item.Parent.Children;
				IList<ContentItem> filtered = new ItemList(siblings, filter);
				int index = filtered.IndexOf(item);
				if (index + 1 < filtered.Count)
				{
					MoveTo(item, NodePosition.After, filtered[index + 1]);
				}
			}
		}

		public void MoveTo(ContentItem item, int index)
		{
			IList<ContentItem> siblings = item.Parent.Children;
			Utility.MoveToIndex(siblings, item, index);
			foreach (ContentItem updatedItem in Utility.UpdateSortOrder(siblings))
			{
				persister.Save(updatedItem);
			}
		}

		public void MoveTo(ContentItem item, NodePosition position, ContentItem relativeTo)
		{
			if (item.Parent != relativeTo.Parent)
				item.AddTo(relativeTo.Parent);

			IList<ContentItem> siblings = item.Parent.Children;
			
			int itemIndex = siblings.IndexOf(item);
			int relativeToIndex = siblings.IndexOf(relativeTo);
			
			if(itemIndex < relativeToIndex && position == NodePosition.Before)
				MoveTo(item, relativeToIndex - 1);
			else if (itemIndex > relativeToIndex && position == NodePosition.After)
				MoveTo(item, relativeToIndex + 1);
			else
				MoveTo(item, relativeToIndex);
		}
		#endregion
	}
}