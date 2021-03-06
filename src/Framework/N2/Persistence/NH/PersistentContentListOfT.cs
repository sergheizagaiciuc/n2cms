﻿using System;
using System.Collections.Generic;
using System.Linq;
using N2.Collections;
using NHibernate;
using NHibernate.Collection.Generic;
using NHibernate.Engine;

namespace N2.Persistence.NH
{
	public class PersistentContentList<T> : PersistentGenericBag<T>, IContentList<T> where T : class, INameable
	{
		public PersistentContentList(ISessionImplementor session)
			: base(session)
		{
		}

		public PersistentContentList(ISessionImplementor session, IList<T> collection)
			: base(session, collection)
		{
		}

		#region IList overrides

		new public int Count
		{
			get
			{
				if (this.WasInitialized)
					return base.Count;

				return Convert.ToInt32(((ISession)Session).CreateFilter(this, "select count(*)").UniqueResult());
			}
		}

		new public T this[int index]
		{
			get { return base[index] as T; }
			set { base[index] = value; }
		}

		#endregion

		#region INamedList<T> Members

		public void Add(string key, T value)
		{
			EnsureName(key, value);

			Add(value);
		}

		private IList<T> List
		{
			get { return this; }
		}

		public bool ContainsKey(string key)
		{
			return List.Any(i => i.Name == key);
		}

		public ICollection<string> Keys
		{
			get { return List.Select(i => i.Name).ToList(); }
		}

		public bool Remove(string key)
		{
			var item = this[key];
			if (item != null)
				this.Remove(item);
			return item != null;
		}

		public bool TryGetValue(string key, out T value)
		{
			value = List.FirstOrDefault(i => i.Name == key);
			return value != null;
		}

		public ICollection<T> Values
		{
			get { return List.ToList(); }
		}

		public T this[string key]
		{
			get
			{
				return FindNamed(key);
			}
			set
			{
				EnsureName(key, value);

				var result = List.Select((item, index) => new { item, index }).FirstOrDefault(i => i.item.Name == key);
				if (result == null)
					Add(key, value);
				else
					this[result.index] = value;
			}
		}

		public T FindNamed(string name)
		{
			if (WasInitialized) return List.FirstOrDefault(i => i.Name == name);

			return ((ISession)Session).CreateFilter(this, "where Name=:name").SetParameter("name", name).UniqueResult<T>();
		}

		#endregion

		#region IPageableList<T> Members

		public virtual IList<T> FindRange(int skip, int take)
		{
			if (this.WasInitialized)
				return this.Skip(skip)
					.Take(take)
					.ToList();

			IQuery pagedList = ((ISession)Session)
				.CreateFilter(this, "")
				.SetFirstResult(skip)
				.SetMaxResults(take)
				.SetCacheable(true);

			return pagedList.List<T>();
		}

		#endregion

		private static void EnsureName(string key, T value)
		{
			if (value.Name != key)
				throw new InvalidOperationException("Cannot add value with differnet name (" + key + " != " + value.Name + ")");
		}
	}
}
