using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceStack.Text
{
	public static class TypeConfig
	{
		sealed class DefaultConstructorFactory : IConstructorFactory
		{
			static readonly DefaultConstructorFactory instance;

			static DefaultConstructorFactory()
			{
				instance = new DefaultConstructorFactory();
			}

			public static DefaultConstructorFactory Instance
			{
				get
				{
					return instance;
				}
			}

			DefaultConstructorFactory() { }

			public EmptyCtorDelegate Get(Type type)
			{
				return null;
			}
		}

		static IConstructorFactory factory;
		static TypeConfig() { factory = DefaultConstructorFactory.Instance; }

		public static IConstructorFactory ConstructorFactory
		{
			get
			{
				return factory;
			}
			set
			{
				factory = value ?? DefaultConstructorFactory.Instance;
			}
		}
	}

	public static class TypeConfig<T>
	{
		public static PropertyInfo[] Properties = new PropertyInfo[0];

		static TypeConfig()
		{
			var excludedProperties = JsConfig<T>.ExcludePropertyNames ?? new string[0];
			Properties = typeof(T).GetSerializableProperties()
				.Where(x => !excludedProperties.Contains(x.Name)).ToArray();
		}
	}
}