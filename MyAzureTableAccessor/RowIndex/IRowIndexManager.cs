namespace MyNoSqlAccessor.AzureTable
{
    using System.Collections.Generic;

    public interface IRowIndexManager<TEntity>
    {
        public IRowIndex<TEntity> Get(string indexName);

        public string TableName { get => typeof(TEntity).Name.ToLower(); }

        public IEnumerable<IRowIndex<TEntity>> GetAll();
    }
}