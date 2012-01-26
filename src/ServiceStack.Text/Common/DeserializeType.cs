//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2011 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

#if !XBOX
using System.Linq.Expressions;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace ServiceStack.Text.Common
{
	internal static class DeserializeType<TSerializer>
		where TSerializer : ITypeSerializer
	{
		private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

		private static readonly string TypeAttrInObject = Serializer.TypeAttrInObject;

		public static ParseStringDelegate GetParseMethod(Type type)
		{
			EmptyCtorDelegate ctorFn = TypeConfig.ConstructorFactory.Get(type);

			if ((!type.IsClass || type.IsAbstract || type.IsInterface) && ctorFn == null) return null;

			var propertyInfos = type.GetSerializableProperties();
			if (propertyInfos.Length == 0)
			{
				ctorFn = ctorFn ?? ReflectionExtensions.GetConstructorMethodToCache(type);
				return value => ctorFn();
			}

			var map = new Dictionary<string, TypeAccessor>(StringComparer.OrdinalIgnoreCase);

			foreach (var propertyInfo in propertyInfos)
			{
				map[propertyInfo.Name] = TypeAccessor.Create(Serializer, type, propertyInfo);
			}

			ctorFn = ctorFn ?? ReflectionExtensions.GetConstructorMethodToCache(type);

			return value => StringToType(type, value, ctorFn, map);
		}

		public static object ObjectStringToType(string strType)
		{
			var type = ExtractType(strType);
			if (type != null)
			{
				var parseFn = Serializer.GetParseFn(type);
				var propertyValue = parseFn(strType);
				return propertyValue;
			}

			return strType;
		}

		public static Type ExtractType(string strType)
		{
			if (strType != null
				&& strType.Length > TypeAttrInObject.Length
				&& strType.Substring(0, TypeAttrInObject.Length) == TypeAttrInObject)
			{
				var propIndex = TypeAttrInObject.Length;
				var typeName = Serializer.EatValue(strType, ref propIndex);
				typeName = Serializer.ParseString(typeName);
				var type = AssemblyUtils.FindType(typeName);

				if (type == null)
					Tracer.Instance.WriteWarning("Could not find type: " + typeName);

				return type;
			}
			return null;
		}

		public static object ParseAbstractType<T>(string value)
		{
			if (typeof(T).IsAbstract)
			{
				if (string.IsNullOrEmpty(value)) return null;
				var concreteType = ExtractType(value);
				if (concreteType != null)
				{
					return Serializer.GetParseFn(concreteType)(value);
				}
				Tracer.Instance.WriteWarning(
					"Could not deserialize Abstract Type with unknown concrete type: " + typeof(T).FullName);
			}
			return null;
		}

		private static object StringToType(Type type, string strType,
		   EmptyCtorDelegate ctorFn,
		   Dictionary<string, TypeAccessor> typeAccessorMap)
		{
			var index = 0;

			if (strType == null)
				return null;

			if (!Serializer.EatMapStartChar(strType, ref index))
				throw new SerializationException(string.Format(
					"Type definitions should start with a '{0}', expecting serialized type '{1}', got string starting with: {2}",
					JsWriter.MapStartChar, type.Name, strType.Substring(0, strType.Length < 50 ? strType.Length : 50)));

			if (strType == JsWriter.EmptyMap) return ctorFn();

			object instance = null;

			var strTypeLength = strType.Length;
			while (index < strTypeLength)
			{
				var propertyName = Serializer.EatMapKey(strType, ref index);

				Serializer.EatMapKeySeperator(strType, ref index);

				var propertyValueStr = Serializer.EatValue(strType, ref index);
				var possibleTypeInfo = propertyValueStr != null && propertyValueStr.Length > 1 && propertyValueStr[0] == '_';

				if (possibleTypeInfo && propertyName == JsWriter.TypeAttr)
				{
					var typeName = Serializer.ParseString(propertyValueStr);
					instance = ReflectionExtensions.CreateInstance(typeName);
					if (instance == null)
					{
						Tracer.Instance.WriteWarning("Could not find type: " + propertyValueStr);
					}
					else
					{
						//If __type info doesn't match, ignore it.
						if (!type.IsInstanceOfType(instance))
							instance = null;
					}

					Serializer.EatItemSeperatorOrMapEndChar(strType, ref index);
					continue;
				}

				if (instance == null) instance = ctorFn();

				TypeAccessor typeAccessor;
				typeAccessorMap.TryGetValue(propertyName, out typeAccessor);

				var propType = possibleTypeInfo ? ExtractType(propertyValueStr) : null;
				if (propType != null)
				{
					try
					{
						if (typeAccessor != null)
						{
							var parseFn = Serializer.GetParseFn(propType);
							var propertyValue = parseFn(propertyValueStr);
							typeAccessor.SetProperty(instance, propertyValue);
						}

						Serializer.EatItemSeperatorOrMapEndChar(strType, ref index);

						continue;
					}
					catch
					{
						Tracer.Instance.WriteWarning("WARN: failed to set dynamic property {0} with: {1}", propertyName, propertyValueStr);
					}
				}

				if (typeAccessor != null && typeAccessor.GetProperty != null)
				{
					try
					{
						var propertyValue = typeAccessor.GetProperty(propertyValueStr);

						if (typeAccessor.SetProperty != null)
						{
							typeAccessor.SetProperty(instance, propertyValue);
						}
					}
					catch
					{
						Tracer.Instance.WriteWarning("WARN: failed to set property {0} with: {1}", propertyName, propertyValueStr);
					}
				}

				Serializer.EatItemSeperatorOrMapEndChar(strType, ref index);
			}

			return instance;
		}

		internal class TypeAccessor
		{
			internal ParseStringDelegate GetProperty;
			internal SetPropertyDelegate SetProperty;

			public static TypeAccessor Create(ITypeSerializer serializer, Type type, PropertyInfo propertyInfo)
			{
				return new TypeAccessor
				{
					GetProperty = serializer.GetParseFn(propertyInfo.PropertyType),
					SetProperty = GetSetPropertyMethod(type, propertyInfo),
				};
			}
		}

		internal static SetPropertyDelegate GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
		{
			var setMethodInfo = propertyInfo.GetSetMethod(true);
			if (setMethodInfo == null) return null;

#if SILVERLIGHT || MONOTOUCH || XBOX
			return (instance, value) => setMethodInfo.Invoke(instance, new[] {value});
#else
			var oInstanceParam = Expression.Parameter(typeof(object), "oInstanceParam");
			var oValueParam = Expression.Parameter(typeof(object), "oValueParam");

			var instanceParam = Expression.Convert(oInstanceParam, type);
			var useType = propertyInfo.PropertyType;

			var valueParam = Expression.Convert(oValueParam, useType);
			var exprCallPropertySetFn = Expression.Call(instanceParam, setMethodInfo, valueParam);

			var propertySetFn = Expression.Lambda<SetPropertyDelegate>
				(
					exprCallPropertySetFn,
					oInstanceParam,
					oValueParam
				).Compile();

			return propertySetFn;
#endif
		}
	}
}