using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ServiceStack.Text
{
    static class TypeConfig
    {
        static Dictionary<Type, EmptyCtorDelegate> constructors = new Dictionary<Type, EmptyCtorDelegate>();
        static Dictionary<Type, bool> trimNamesAndValues = new Dictionary<Type, bool>();

        public static EmptyCtorDelegate Get(Type type)
        {
            EmptyCtorDelegate func = null;
            constructors.TryGetValue(type, out func);
            return func;
        }

        public static void Set<T>(Type type, Func<T> func)
        {
            if (func == null)
                constructors[type] = null;
            else
                constructors[type] = () => func();
        }

        public static bool HasConstructorFor(Type type)
        {
            return constructors.ContainsKey(type);
        }
    }

	public static class TypeConfig<T>
    {
        public static Func<T> Constructor { set { TypeConfig.Set(typeof(T), value); } }

		public static PropertyInfo[] Properties = new PropertyInfo[0];

		static TypeConfig()
		{
			var excludedProperties = JsConfig<T>.ExcludePropertyNames ?? new string[0];
			Properties = typeof(T).GetSerializableProperties()
				.Where(x => !excludedProperties.Contains(x.Name)).ToArray();
		}
	}
}