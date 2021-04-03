using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyNoSqlAccessor.AzureTable
{
    public class AutoEntityConverter<TEntity> : IEntityConverter<TEntity> where TEntity : new()
    {
        /* 
         * EntityProperty supported types
         * EntityProperty(string input); 3
         * EntityProperty(long? input); 2
         * EntityProperty(int? input); 1
         * EntityProperty(Guid? input); 4
         * EntityProperty(double? input); 5
         * EntityProperty(DateTime? input); 6 
         * EntityProperty(DateTimeOffset? input);7
         * EntityProperty(bool? input); 8 
         * EntityProperty(byte[] input); 9
         */

        private static Dictionary<Type, Dictionary<string, PropertyInfo>> _EntityMetaInfoCache =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        public AutoEntityConverter()
        {
            // type checker
            TypeChecker.EnsureEntityTypesSupported(typeof(TEntity));

            var entityType = typeof(TEntity);
            if (!_EntityMetaInfoCache.ContainsKey(entityType))
            {
                var propertiesCache = new Dictionary<string, PropertyInfo>();
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertiesCache.Clear();
                foreach (var prop in properties)
                {
                    propertiesCache[prop.Name] = prop;
                }
                _EntityMetaInfoCache.Add(entityType, propertiesCache);
            }
        }

        public TEntity Deserialize(IDictionary<string, EntityProperty> values)
        {
            TEntity result = new TEntity();
            var properties = GetPropertyDict();
            foreach (var (name, property) in properties)
            {
                // result.XxxProperty= values["namename"].Int32Value.Value;
                // result.XxxProperty= values["namename"].GuidValue.Value;
                //property.SetValue(result, GetEntityPropertyValue(values[name], property.PropertyType));
                property.SetValue(result, values[name].PropertyAsObject);

                var propType = property.PropertyType;
                // 1 int 32
                if (propType == typeof(int) || propType == typeof(int?))
                {
                    property.SetValue(result, values[name].Int32Value);
                }
                // 2 int 64
                else if (propType == typeof(long) || propType == typeof(long?))
                {
                    property.SetValue(result, values[name].Int64Value);
                }
                // 3 string
                else if (propType == typeof(string))
                {
                    property.SetValue(result, values[name].StringValue);
                }
                // 4 Guid
                else if (propType == typeof(Guid) || propType == typeof(Guid?))
                {
                    property.SetValue(result, values[name].GuidValue);
                }
                // 5 double
                else if (propType == typeof(double) || propType == typeof(double?))
                {
                    property.SetValue(result, values[name].DoubleValue);
                }
                // 6 datetime
                else if (propType == typeof(DateTime) || propType == typeof(DateTime?))
                {
                    property.SetValue(result, values[name].DateTime);
                }
                // 7 datetimeoffset
                else if (propType == typeof(DateTimeOffset) || propType == typeof(DateTimeOffset?))
                {
                    property.SetValue(result, values[name].DateTimeOffsetValue);
                }
                // 8 bool//  
                else if (propType == typeof(bool) || propType == typeof(bool?))
                {
                    property.SetValue(result, values[name].BooleanValue);
                }
                // 9 byte[]
                else if (propType == typeof(byte[]))
                {
                    property.SetValue(result, values[name].BinaryValue);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            return result;
        }

        //private object GetEntityPropertyValue(EntityProperty entityProperty, Type type)
        //{
        //    if (type == typeof(Int32))
        //    {
        //        return entityProperty.Int32Value;
        //    }
        //    else
        //    {
        //        return entityProperty.PropertyAsObject;
        //    }
        //}

        public IDictionary<string, EntityProperty> Serialize(TEntity entity)
        {
            Dictionary<string, EntityProperty> propertyValueDictionary = new Dictionary<string, EntityProperty>();
            var properties = GetPropertyDict();
            foreach (var (name, property) in properties)
            {
                //e["Name"] = new EntityProperty(entity.Name);

                var propType = property.PropertyType;
                // 1 int32
                if (propType == typeof(int) || propType == typeof(int?))
                {
                    propertyValueDictionary[name] = new EntityProperty((int?)property.GetValue(entity));
                }
                // 2 int 64
                else if (propType == typeof(long) || propType == typeof(long?))
                {
                    propertyValueDictionary[name] = new EntityProperty((long?)property.GetValue(entity));
                }
                // 3 string
                else if (propType == typeof(string))
                {
                    propertyValueDictionary[name] = new EntityProperty((string)property.GetValue(entity));
                }
                // 4 guid
                else if (propType == typeof(Guid) || propType == typeof(Guid?))
                {
                    propertyValueDictionary[name] = new EntityProperty((Guid?)property.GetValue(entity));
                }
                //  5 double
                else if (propType == typeof(double) || propType == typeof(double?))
                {
                    propertyValueDictionary[name] = new EntityProperty((double?)property.GetValue(entity));
                }
                // 6 datetime
                else if (propType == typeof(DateTime) || propType == typeof(DateTime?))
                {
                    propertyValueDictionary[name] = new EntityProperty((DateTime?)property.GetValue(entity));
                }
                // 7 datetimeoffset
                else if (propType == typeof(DateTimeOffset) || propType == typeof(DateTimeOffset?))
                {
                    propertyValueDictionary[name] = new EntityProperty((DateTimeOffset?)property.GetValue(entity));
                }
                // 8 bool 
                else if (propType == typeof(bool) || propType == typeof(bool?))
                {
                    propertyValueDictionary[name] = new EntityProperty((bool?)property.GetValue(entity));
                }
                // 9 byte[]
                else if (propType == typeof(byte[]))
                {
                    propertyValueDictionary[name] = new EntityProperty((byte[])property.GetValue(entity));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            return propertyValueDictionary;
        }

        private IDictionary<string, PropertyInfo> GetPropertyDict()
        {
            return _EntityMetaInfoCache[typeof(TEntity)];
        }
    }
}
