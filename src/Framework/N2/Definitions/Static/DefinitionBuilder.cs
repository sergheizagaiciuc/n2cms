using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using N2.Details;
using N2.Edit;
using N2.Engine;
using N2.Web.UI;
using N2.Configuration;

namespace N2.Definitions.Static
{
	/// <summary>
	/// Inspects available types in the AppDomain and builds item definitions.
	/// </summary>
	[Service]
	public class DefinitionBuilder
	{
		private readonly DefinitionTable staticDefinitions;
		private readonly ITypeFinder typeFinder;
		private readonly EngineSection config;

		public DefinitionBuilder(DefinitionTable staticDefinitions, ITypeFinder typeFinder, EngineSection config)
		{
			this.staticDefinitions = staticDefinitions;
			this.typeFinder = typeFinder;
			this.config = config;
		}

		public DefinitionBuilder(ITypeFinder typeFinder, EngineSection config)
			: this(DefinitionTable.Instance, typeFinder, config)
		{
		}

		/// <summary>Builds item definitions in the current environment.</summary>
		/// <returns>A dictionary of item definitions in the current environment.</returns>
		public virtual IEnumerable<ItemDefinition> GetDefinitions()
		{
			List<ItemDefinition> definitions = FindDefinitions();
			ExecuteRefiners(definitions);
			
			return definitions;
		}

		protected List<ItemDefinition> FindDefinitions()
		{
			List<ItemDefinition> definitions = new List<ItemDefinition>();
			foreach (Type itemType in FindConcreteTypes())
			{
				var definition = staticDefinitions.GetDefinition(itemType).Clone();
				definitions.Add(definition);
			}

			foreach (DefinitionElement element in config.Definitions.RemovedElements)
			{
				ItemDefinition definition = definitions.Find(d => d.Discriminator == element.Name);
				if (definition == null)
					throw new ConfigurationErrorsException("The configuration element /n2/engine/definitions/remove references a definition '" + element.Name + "' that couldn't be found in the application. Either remove the <remove/> configuration or ensure that the corresponding content class exists.");

				definitions.Remove(definition);
			}

			foreach(DefinitionElement element in config.Definitions.AllElements)
			{
				ItemDefinition definition = definitions.Find(d => d.Discriminator == element.Name);
				if (definition != null)
					definitions.Remove(definition);
				else
				{
					Type itemType = EnsureType<ContentItem>(element.Type);

					definition = new ItemDefinition(itemType);
					definition.Discriminator = element.Name;
					definition.Initialize(itemType);
				}

				definition.SortOrder = element.SortOrder ?? definition.SortOrder;
				definition.Title = element.Title ?? definition.Title;
				definition.ToolTip = element.ToolTip ?? definition.ToolTip;
				definition.IsDefined = true;

				foreach (ContainableElement editable in element.Editables.AllElements)
				{
					AddContainable(definition, editable);
				}
				foreach(ContainableElement editable in element.Editables.RemovedElements)
				{
					RemoveContainable(definition, editable);
				}
				foreach (ContainableElement container in element.Containers.AllElements)
				{
					AddContainable(definition, container);
				}
				foreach (ContainableElement container in element.Containers.RemovedElements)
				{
					RemoveContainable(definition, container);
				}

				definitions.Add(definition);
			}

			definitions.Sort();
			return definitions;
		}

		void AddContainable(ItemDefinition definition, ContainableElement editable)
		{
			Type editableType = EnsureType<IEditable>(editable.Type);
			try
			{
				IContainable containable = Activator.CreateInstance(editableType) as IContainable;
				Utility.SetProperty(containable, "Name", editable.Name);
				Utility.SetProperty(containable, "ContainerName", editable.ContainerName);
				Utility.SetProperty(containable, "SortOrder", editable.SortOrder);
				foreach (string key in editable.EditableProperties.Keys)
				{
					Utility.SetProperty(containable, key, editable.EditableProperties[key]);
				}
				definition.Add(containable);
			}
			catch (MissingMethodException ex)
			{
				throw new ConfigurationErrorsException("The type '" + editable.Type + "' defined in the configuration does not have a parameterless public constructor. This is required for the type to be configurable.", ex);
			}
		}

		void RemoveContainable(ItemDefinition definition, ContainableElement editable)
		{
			definition.Remove(definition.Get(editable.Name));
		}

		Type EnsureType<T>(string typeName)
		{
			Type type = Type.GetType(typeName);
			if (type == null) throw new ConfigurationErrorsException("The configuration references a type '" + typeName + "' which could not be loaded. Check the spelling and ensure that the type is available to the application.");
			if (!typeof(T).IsAssignableFrom(type)) throw new ConfigurationErrorsException("The type '" + typeName + "' referenced by the configuration does not derive from the correct base class or interface '" + typeof(T).FullName + "'. Check the type's inheritance");
			return type;
		}

		protected void ExecuteRefiners(IList<ItemDefinition> definitions)
		{
			// these are executed one time per definition
			List<ISortableRefiner> globalRefiners = new List<ISortableRefiner>();
			foreach (Assembly a in typeFinder.GetAssemblies())
				foreach (IDefinitionRefiner refiner in a.GetCustomAttributes(typeof(IDefinitionRefiner), false))
					globalRefiners.Add(refiner);

			// build the whole list of refiners
			List<RefinerPair> refiners = new List<RefinerPair>();
			foreach (ItemDefinition definition in definitions)
			{
				foreach (IDefinitionRefiner refiner in globalRefiners)
					refiners.Add(new RefinerPair(definition, refiner));
				foreach (IDefinitionRefiner refiner in definition.ItemType.GetCustomAttributes(typeof(IDefinitionRefiner), false))
					refiners.Add(new RefinerPair(definition, refiner));
				foreach (IInheritableDefinitionRefiner refiner in definition.ItemType.GetCustomAttributes(typeof(IInheritableDefinitionRefiner), true))
					refiners.Add(new RefinerPair(definition, refiner));
			}

			// sort them and execute
			refiners.Sort((first, second) => first.Refiner.CompareTo(second.Refiner));
			foreach (RefinerPair pair in refiners)
				pair.Refiner.Refine(pair.Definition, definitions);
		}

		protected class RefinerPair
		{
			public RefinerPair(ItemDefinition definition, ISortableRefiner refiner)
			{
				Definition = definition;
				Refiner = refiner;
			}
			public ItemDefinition Definition { get; set; }
			public ISortableRefiner Refiner { get; set; }
		}

        /// <summary>Enumerates concrete item types provided by the type finder.</summary>
        /// <returns>An enumeration of types derived from <see cref="N2.ContentItem"/>.</returns>
		protected IEnumerable<Type> FindConcreteTypes()
		{
			foreach(Type t in typeFinder.Find(typeof (ContentItem)))
			{
				if(t != null && !t.IsAbstract && !t.ContainsGenericParameters)
				{
                    yield return t;
				}
			}
		}
	}
}
