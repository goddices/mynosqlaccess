namespace MyNoSqlAccessor.AzureTable
{
    using System.Collections.Generic;
    using System.Linq;

    public class RowIndexManager<TEntity> : IRowIndexManager<TEntity>
    {
        private readonly Dictionary<string, IRowIndex<TEntity>> indices;

        public RowIndexManager(IEnumerable<IRowIndex<TEntity>> indices)
        {
            this.indices = indices.ToDictionary(i => i.Name);
        }

        public IRowIndex<TEntity> Get(string indexName) => this.indices[indexName];

        public IEnumerable<IRowIndex<TEntity>> GetAll() => this.indices.Values;
    }
}