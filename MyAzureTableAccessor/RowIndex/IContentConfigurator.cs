namespace MyNoSqlAccessor.AzureTable
{
    using System;

    public interface IContentConfigurator<TEntity>
    {
        IContentConfigurator<TEntity> UseJson() => this.Use(entity => entity);

        IContentConfigurator<TEntity> Use<TContent>(Func<TEntity, TContent> contentFunc);

        IContentConfigurator<TEntity> Default() => this.UseJson();
    }
}