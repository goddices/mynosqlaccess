namespace MyNoSqlAccessor.AzureTable
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RowIndex<TEntity> : IRowIndex<TEntity>
    {
        internal enum KeyType
        {
            ParitionKey,
            RowKey,
        }

        internal class ContentConfigurator : IContentConfigurator<TEntity>
        {
            private readonly RowIndex<TEntity> rowIndex;

            public ContentConfigurator(RowIndex<TEntity> rowIndex)
            {
                this.rowIndex = rowIndex;
            }

            public IContentConfigurator<TEntity> Use<TContent>(Func<TEntity, TContent> contentFunc)
            {
                this.rowIndex.contentFunc = e =>
                    typeof(TContent) == typeof(string) ? (dynamic)contentFunc(e) : JsonConvert.SerializeObject(contentFunc(e));
                return this;
            }
        }

        internal class KeyConfigurator : IKeyConfigurator<TEntity>
        {
            private readonly RowIndex<TEntity> rowIndex;
            private readonly KeyType keyType;

            public KeyConfigurator(RowIndex<TEntity> rowIndex, KeyType keyType)
            {
                this.rowIndex = rowIndex;
                this.keyType = keyType;
            }


            public IKeyConfigurator<TEntity> Add<T>(Func<TEntity, T> prefixFunc)
            {
                this.rowIndex.Add(this.keyType, prefixFunc);
                return this;
            }
        }

        private readonly IList<Func<TEntity, string>> partitionKeyFunctions = new List<Func<TEntity, string>>();
        private readonly IList<Func<TEntity, string>> rowKeyFunctions = new List<Func<TEntity, string>>();
        private Func<TEntity, string> contentFunc;

        public RowIndex(string name)
        {
            this.Name = name;
        }


        private IRowIndex<TEntity> Add<TResult>(KeyType type, Func<TEntity, TResult> keyFunc)
        {
            (type switch
            {
                KeyType.ParitionKey => this.partitionKeyFunctions,
                KeyType.RowKey => this.rowKeyFunctions,
                _ => null,
            })
            .Add(entity => KeyHelper.ToKey(keyFunc(entity)));

            return this;
        }

        private IRowIndex<TEntity> Chain(Action action)
        {
            if (action != null) { action(); }
            return this;
        }

        public IRowIndex<TEntity> ForPartition(Action<IKeyConfigurator<TEntity>> partitionConfig) =>
            this.Chain(() => partitionConfig(new KeyConfigurator(this, KeyType.ParitionKey)));

        public IRowIndex<TEntity> ForRowKey(Action<IKeyConfigurator<TEntity>> rowKeyConfig) =>
            this.Chain(() => rowKeyConfig(new KeyConfigurator(this, KeyType.RowKey)));

        public string Name { get; set; }

        public TableEntityId GetIndexKeys(TEntity entity)
        {
            var partitionKey = string.Join(KeyHelper.KeyCombinationChar, this.partitionKeyFunctions.Select(f => f(entity)));
            var rowKey = string.Join(KeyHelper.KeyCombinationChar, this.rowKeyFunctions.Select(f => f(entity)));
            return new TableEntityId() { PartitionKey = partitionKey, RowKey = rowKey };
        }

        public string GetIndexContent(TEntity entity) => this.contentFunc(entity);
    }
}