namespace MyNoSqlAccessor.AzureTable
{
    using System;
    using System.Collections.Generic;

    public interface IRowIndex<TEntity>
    {
        string Name { get; set; }

        IRowIndex<TEntity> ForPartition(Action<IKeyConfigurator<TEntity>> partitionConfig);

        IRowIndex<TEntity> ForRowKey(Action<IKeyConfigurator<TEntity>> rowKeyConfig);

        TableEntityId GetIndexKeys(TEntity entity);

        string GetIndexContent(TEntity entity);
         
    }
}