namespace MyNoSqlAccessor.AzureTable
{
    using System;

    public class TableEntityId : IComparable<TableEntityId>, IEquatable<TableEntityId>
    {
        public TableEntityId()
        {
        }

        public TableEntityId(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public int CompareTo(TableEntityId other)
        {
            var result = this.PartitionKey.CompareTo(other.PartitionKey);
            return result == 0 ? this.RowKey.CompareTo(other.RowKey) : result;
        }

        public bool Equals(TableEntityId other) => this.PartitionKey.Equals(other.PartitionKey) && this.RowKey.Equals(other.RowKey);
    }
}