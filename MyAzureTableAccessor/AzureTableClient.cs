using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Cosmos.Table;
using MyNoSqlAccessor.AzureTable.Extensions;

namespace MyNoSqlAccessor.AzureTable
{
    public class AzureTableClient<TEntity> : INoSqlTable<TEntity> where TEntity : new()
    {
        private static readonly Type EntityType = typeof(TEntity);
        private readonly IEntityConverter<TEntity> _converter;
        private readonly CloudTable _table;
        private readonly IRowIndexManager<TEntity> _rowIndexManager;

        public AzureTableClient(CloudTable table, IEntityConverter<TEntity> converter, IRowIndexManager<TEntity> rowIndexManager)
        {
            TypeChecker.EnsureEntityTypesSupported(EntityType);
            _table = table;
            _converter = converter;
            _rowIndexManager = rowIndexManager;
        }

        public Task CreateTableIfNotExistsAsync(CancellationToken token)
        {
            return _table.CreateIfNotExistsAsync(token);
        }

        public Task DeleteTableIfExistsAsync(CancellationToken token)
        {
            return _table.DeleteIfExistsAsync(token);
        }

        public Task<TEntity> InsertOfReplaceEntityIndexAsync(TEntity insertOrRelaceEntity, string indexName, CancellationToken token)
        {
            var tableEntity = PrepareTableEntity(insertOrRelaceEntity, indexName);
            tableEntity.ETag = EntityType.GetProperty("ETag").GetValue(insertOrRelaceEntity)?.ToString();
            TableOperation operation = TableOperation.InsertOrReplace(tableEntity);
            return ExecuteInnerAsync(operation, token);
        }

        public Task<TEntity> InsertEntityIndexAsync(TEntity inserting, string indexName, CancellationToken token)
        {
            var tableEntity = PrepareTableEntity(inserting, indexName);
            TableOperation operation = TableOperation.Insert(tableEntity);
            return ExecuteInnerAsync(operation, token);
        }

        public Task<TEntity> ReplaceEntityIndexAsync(TEntity replacement, string indexName, CancellationToken token)
        {
            var tableEntity = PrepareTableEntity(replacement, indexName);
            tableEntity.ETag = EntityType.GetProperty("ETag").GetValue(replacement)?.ToString();
            TableOperation operation = TableOperation.Replace(tableEntity);
            return ExecuteInnerAsync(operation, token);
        }

        public async Task DeleteEntityIndexAsync(TEntity criterion, string indexName, CancellationToken token)
        {
            var index = _rowIndexManager.Get(indexName);
            var id = index.GetIndexKeys(criterion);
            var tableEntity = new DynamicTableEntity();
            tableEntity.PartitionKey = id.PartitionKey;
            tableEntity.RowKey = id.RowKey;
            tableEntity.ETag = "*"; // force delete without validating ETag
            var operation = TableOperation.Delete(tableEntity);
            await _table.ExecuteAsync(operation, token);
        }

        public async Task<TEntity> GetEntityIndexAsync(TEntity criterion, string indexName, CancellationToken token)
        {
            var index = _rowIndexManager.Get(indexName);
            var id = index.GetIndexKeys(criterion);
            var operation = TableOperation.Retrieve(id.PartitionKey, id.RowKey);
            var tableResult = await _table.ExecuteAsync(operation, token);
            if (tableResult.HttpStatusCode == 404)
            {
                return default(TEntity);
            }
            else
            {
                return GetResultEntity(tableResult);
            }
        }

        public Task<IEnumerable<TEntity>> QueryEntitiesIndexByPrefixAsync(string indexName, TEntity prefix, int count, CancellationToken token)
        {
            var index = _rowIndexManager.Get(indexName);
            TEntity prefixLast = new TEntity();
            var basicRowkey = index.GetIndexKeys(prefix).RowKey.KeyEncode();
            var rowkeyPrefixStart = basicRowkey + char.MinValue;
            var rowkeyPrefixEnd = basicRowkey + char.MaxValue;
            return QueryEntitiesIndexByPartitionAndRangedRowkeysAsync(index.GetIndexKeys(prefix).PartitionKey, rowkeyPrefixStart, true, rowkeyPrefixEnd, true, count, token);
        }

        public Task<IEnumerable<TEntity>> QueryEntitiesIndexByRangeAsync(string indexName, TEntity start, bool containStart, TEntity end, bool containEnd, int count, CancellationToken token)
        {
            if (start == null && end == null) throw new ArgumentNullException("start end both null");

            var index = _rowIndexManager.Get(indexName);
            TableEntityId startRowId = null, endRowId = null;
            if (start != null && end != null)
            {
                startRowId = start != null ? index.GetIndexKeys(start) : null;
                endRowId = end != null ? index.GetIndexKeys(end) : null;
                if (startRowId.PartitionKey != endRowId.PartitionKey) throw new ArgumentOutOfRangeException("start != end");
            }
            var rowkeyStart = startRowId == null ? null : index.GetIndexKeys(start).RowKey;
            var rowkeyEnd = endRowId == null ? null : index.GetIndexKeys(end).RowKey;
            return QueryEntitiesIndexByPartitionAndRangedRowkeysAsync((startRowId ?? endRowId).PartitionKey, rowkeyStart, containStart, rowkeyEnd, containEnd, count, token); ;
        }

        // TODO: ranged paritionkeys and ranged rowkeys  partition >= start and partition <= end  and rowkey>=r1 & rowkey<=r2
        // TODO: same partitionkey and many rowkeys   partition=xxx and rowkey = r1 and rowkey = r2 and ... r3 ...
        private async Task<IEnumerable<TEntity>> QueryEntitiesIndexByPartitionAndRangedRowkeysAsync(string partitionKey, string rowkeyStart, bool containStart, string rowkeyEnd, bool containEnd, int count, CancellationToken token)
        {
            var results = new List<TEntity>();
            TableContinuationToken conToken = null;
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey);
            if (rowkeyStart != null)
                filter = TableQuery.CombineFilters(filter, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", containStart ? QueryComparisons.GreaterThanOrEqual : QueryComparisons.GreaterThan, rowkeyStart));
            if (rowkeyEnd != null)
                filter = TableQuery.CombineFilters(filter, TableOperators.And, TableQuery.GenerateFilterCondition("RowKey", containEnd ? QueryComparisons.LessThanOrEqual : QueryComparisons.LessThan, rowkeyEnd));

            var cloudQuery = new TableQuery<DynamicTableEntity>().Where(filter);
            do
            {
                var queryResult = await _table.ExecuteQuerySegmentedAsync(
                    cloudQuery.Take(count),
                    conToken,
                    null,
                    null,
                    token);

                results.AddRange(queryResult.Results.Select(at => GetResultEntity(at)));

                conToken = queryResult.ContinuationToken;// get token from Azure if null stop
            }
            while (conToken != null && (results.Count < count));
            return results;
        }

        private TEntity GetResultEntity(TableResult tableResult)
        {
            if (tableResult.Result == null)
            {
                return default(TEntity);
            }
            var tableEntity = tableResult.Result as DynamicTableEntity;
            return GetResultEntity(tableEntity);
        }

        private TEntity GetResultEntity(DynamicTableEntity dynamicTableEntity)
        {
            var etag = dynamicTableEntity.ETag;
            if (!dynamicTableEntity.Properties.ContainsKey(nameof(DynamicTableEntity.ETag)))
            {
                dynamicTableEntity.Properties.Add(nameof(DynamicTableEntity.ETag), new EntityProperty(etag));
            }
            var resultEntity = _converter.Deserialize(dynamicTableEntity.Properties);
            EntityType.GetProperty("ETag").SetValue(resultEntity, etag);
            return resultEntity;
        }

        private DynamicTableEntity PrepareTableEntity(TEntity entity, string indexName)
        {
            var index = _rowIndexManager.Get(indexName);
            var id = index.GetIndexKeys(entity);
            var tableEntity = new DynamicTableEntity();
            tableEntity.PartitionKey = id.PartitionKey;
            tableEntity.RowKey = id.RowKey;
            tableEntity.Properties = _converter.Serialize(entity);
            return tableEntity;
        }

        private async Task<TEntity> ExecuteInnerAsync(TableOperation operation, CancellationToken token)
        {
            try
            {
                var tableResult = await _table.ExecuteAsync(operation, token);
                var resultEntity = GetResultEntity(tableResult);
                EntityType.GetProperty("ETag").SetValue(resultEntity, tableResult.Etag);
                return resultEntity;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict &&
                    operation.OperationType == TableOperationType.Insert)
                {
                    throw new HttpRequestException(nameof(HttpStatusCode.Conflict), e);
                }
                else if (e.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.PreconditionFailed && operation.OperationType == TableOperationType.Replace)
                {
                    throw new HttpRequestException(nameof(HttpStatusCode.PreconditionFailed), e);
                }
                else
                {
                    throw new HttpRequestException("Unknown", e);
                }
            }
        }
    }
}
