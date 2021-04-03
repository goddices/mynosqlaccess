using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MyNoSqlAccessor.AzureTable
{
    public class AzureTableClientFactory : INoSqlTableFactory
    {
        private readonly IServiceProvider _provider;
        private readonly string _storageConnectionString;

        public AzureTableClientFactory(string connectionString, IServiceProvider provider)
        {
            _provider = provider;
            _storageConnectionString = connectionString;
        }

        public INoSqlTable<TEntity> CreateClient<TEntity>() where TEntity : new()
        {
            var storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(typeof(TEntity).Name.ToLower());
            var entityConverter = _provider.GetRequiredService<IEntityConverter<TEntity>>();
            var rowIndexManager = _provider.GetRequiredService<IRowIndexManager<TEntity>>();
            return new AzureTableClient<TEntity>(table, entityConverter, rowIndexManager);
        }
    }
}
