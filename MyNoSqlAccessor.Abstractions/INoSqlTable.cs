using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyNoSqlAccessor
{
    public interface INoSqlTable<TEntity>
    {
        Task CreateTableIfNotExistsAsync(CancellationToken token);

        Task DeleteTableIfExistsAsync(CancellationToken token);

        Task<TEntity> InsertOfReplaceEntityIndexAsync(TEntity insertOrRelaceEntity, string indexName, CancellationToken token);

        Task<TEntity> InsertEntityIndexAsync(TEntity inserting, string indexName, CancellationToken token);

        Task<TEntity> ReplaceEntityIndexAsync(TEntity replacement, string indexName, CancellationToken token);

        Task DeleteEntityIndexAsync(TEntity criterion, string indexName, CancellationToken token);

        Task<TEntity> GetEntityIndexAsync(TEntity criterion, string indexName, CancellationToken token);

        Task<IEnumerable<TEntity>> QueryEntitiesIndexByPrefixAsync(string indexName, TEntity prefix, int count, CancellationToken token);

        Task<IEnumerable<TEntity>> QueryEntitiesIndexByRangeAsync(string indexName, TEntity start, bool containStart, TEntity end, bool containEnd, int count, CancellationToken token);
    }
}
