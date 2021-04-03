namespace MyNoSqlAccessor.AzureTable
{
    using System;

    public interface IKeyConfigurator<TEntity>
    {
        IKeyConfigurator<TEntity> Add<T>(Func<TEntity, T> prefixFunc);

        IKeyConfigurator<TEntity> Id()
        {
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty?.PropertyType == typeof(Guid))
            {
                return Add(e => (Guid)idProperty.GetValue(e));
            }
            else
            {
                throw new Exception($"The type {typeof(TEntity)} doesn't have an Id with Guid type.");
            }
        }

        IKeyConfigurator<TEntity> Const<T>(T key) => Add(e => key);

        IKeyConfigurator<TEntity> Instance() => Const("instance");
    }
}