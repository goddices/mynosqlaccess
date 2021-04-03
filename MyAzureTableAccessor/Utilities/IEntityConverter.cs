using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyNoSqlAccessor.AzureTable
{
    public interface IEntityConverter<TEntity>
    {
        /// <summary>
        /// Deserialize key-value structured azure table (DynamicTableEntity) as Entity
        /// </summary> 
        TEntity Deserialize(IDictionary<string, EntityProperty> values);

        /// <summary>
        /// Serialize Entity as key-value structured azure table (DynamicTableEntity)
        /// </summary> 
        IDictionary<string, EntityProperty> Serialize(TEntity entity);
    }
}
