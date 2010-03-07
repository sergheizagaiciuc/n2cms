﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using N2.Engine;

namespace N2.Tests.Engine.Services
{

	[Service(Key = "Sesame")]
	public class SelfService
	{
	}

	[Service]
	public class DependingService
	{
		public SelfService service;
		public DependingService(SelfService service)
		{
			this.service = service;
		}
	}

	[Service]
	public class GenericSelfService<T>
	{
	}

	[Service]
	public class DependingGenericSelfService<T>
	{
		public SelfService service;
		public DependingGenericSelfService(SelfService service)
		{
			this.service = service;
		}
	}

	[Service]
	public class GenericDependingService
	{
		public GenericSelfService<int> service;
		public GenericDependingService(GenericSelfService<int> service)
		{
			this.service = service;
		}
	}

	public interface IService
	{
	}

	[Service(typeof(IService))]
	public class InterfacedService : IService
	{
	}

	[Service(typeof(IService))]
	public class DecoratingService : IService
	{
		public IService decorated;

		public DecoratingService(IService decorated)
		{
			this.decorated = decorated;
		}
	}

	public interface IGenericService<T>
	{
	}

	[Service(typeof(IGenericService<>))]
	public class GenericInterfacedService<T> : IGenericService<T>
	{
	}

	[Service]
	public class GenericInterfaceDependingService
	{
		public IGenericService<int> service;
		public GenericInterfaceDependingService(IGenericService<int> service)
		{
			this.service = service;
		}
	}

	public class NonAttributed
	{
	}

}