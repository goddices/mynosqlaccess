using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace MyNoSqlAccessor.AzureTable
{
    public class TypeChecker
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
         *  
         * ensure each entity properties contains these types( nullble & non-nullble )
         */

        private static Type[] supportedTypes = new Type[]
        {
            typeof(string),
            typeof(int),typeof(int?),
            typeof(long),typeof(long?),
            typeof(Guid),typeof(Guid?),
            typeof(double),typeof(double?),
            typeof(DateTime),typeof(DateTime?),
            typeof(DateTimeOffset),typeof(DateTimeOffset?),
            typeof(bool),typeof(bool?),
            typeof(byte[])
        };

        public static void EnsureEntityTypesSupported(Type entityType)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (!properties.Any(x => x.Name == "ETag"))
            {
                throw new ArgumentException("public instance property named ETag is required");
            }
            var subset = properties.Select(p => p.PropertyType).Except(supportedTypes);
            if (subset.Count() != 0)
            {
                // Except is a collection operation means a collection of B-A
                // B-A == 0 means B is a subset of A , if not, B contains elements which A doesnot contain 
                throw new NotSupportedException(string.Join(',', subset.Select(x => x.Name)) + " types are not supported");
            }
        }
    }
}
