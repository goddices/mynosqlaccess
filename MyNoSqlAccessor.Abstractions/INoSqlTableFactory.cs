namespace MyNoSqlAccessor
{
    public interface INoSqlTableFactory
    {
        INoSqlTable<TEntity> CreateClient<TEntity>() where TEntity : new();
    }
}
