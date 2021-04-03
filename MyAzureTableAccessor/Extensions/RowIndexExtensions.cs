using Microsoft.Extensions.DependencyInjection;
using System;

namespace MyNoSqlAccessor.AzureTable.Extensions
{
    public static class RowIndexExtensions
    {
        public static IServiceCollection AddIndexManager(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped(typeof(IRowIndexManager<>), typeof(RowIndexManager<>));

        public static IServiceCollection AddIndex<TEntity>(
            this IServiceCollection serviceCollection, string name, Action<IRowIndex<TEntity>> configIndex) =>
            serviceCollection.AddScoped<IRowIndex<TEntity>>(p =>
            {
                var rowIndex = new RowIndex<TEntity>(name);
                configIndex(rowIndex);
                return rowIndex;
            });
    }
}