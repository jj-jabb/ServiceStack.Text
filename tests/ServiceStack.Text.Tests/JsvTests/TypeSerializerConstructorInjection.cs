using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsvTests
{
	[TestFixture]
	public class TypeSerializerConstructorInjection
	{
		interface IFoo
		{
			Guid Id { get; set; }
			List<IBar> Bars { get; set; }
		}

		interface IBar
		{
			string Name { get; set; }
			int Amount { get; set; }
		}

		class Foo : IFoo
		{
			public Foo()
			{
				Bars = new List<IBar>();
			}

			public Guid Id { get; set; }
			public List<IBar> Bars { get; set; }
		}

		class Bar : IBar
		{
			public string Name { get; set; }
			public int Amount { get; set; }
		}

		class ConstructorFactory : IConstructorFactory
		{
			static Dictionary<Type, EmptyCtorDelegate> constructors;
			static readonly ConstructorFactory instance;

			static ConstructorFactory()
			{
				constructors = new Dictionary<Type, EmptyCtorDelegate>();
				instance = new ConstructorFactory();
			}

			public static ConstructorFactory Instance
			{
				get
				{
					return instance;
				}
			}

			public static void Set<T>(EmptyCtorDelegate ctor)
			{
				constructors[typeof(T)] = ctor;
			}

			public EmptyCtorDelegate Get(Type type)
			{
				EmptyCtorDelegate ctorFn;
				constructors.TryGetValue(type, out ctorFn);
				return ctorFn;
			}
		}

		[Test]
		public void Can_inject_constructor()
		{
			JsConfig.ExcludeTypeInfo = true;

			ConstructorFactory.Set<IFoo>(() => new Foo());

			TypeConfig.ConstructorFactory = ConstructorFactory.Instance;

			ConstructorFactory.Set<IFoo>(() => new Foo());
			ConstructorFactory.Set<IBar>(() => new Bar());

			var expectedFoo = new Foo { Id = new Guid("{51517605-8c5e-470d-8516-4da01a0ddeab}") };
			expectedFoo.Bars.Add(new Bar { Name = "Apple", Amount = 4 });
			expectedFoo.Bars.Add(new Bar { Name = "Orange", Amount = 6 });

			var expectedFooStr = TypeSerializer.SerializeToString<Foo>(expectedFoo);
			var testFoo = TypeSerializer.DeserializeFromString<Foo>(expectedFooStr);

			var actualFoo = TypeSerializer.DeserializeFromString<IFoo>(expectedFooStr);

			var actualFooStr = TypeSerializer.SerializeToString<Foo>((Foo)actualFoo);

			Assert.AreEqual(expectedFooStr, actualFooStr);
		}
	}
}
