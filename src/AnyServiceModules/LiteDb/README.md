# LiteDB Repository

This directory contains the source code and sample for ysing LiteDB as persistency layer (`IRepository`) and file storage (`IFileStoreManager`).

Please refer to snippet below and sample app.

## Configuring `liteDB`
1. add reference to `AnyService.LiteDB` nuget (see [here](https://www.nuget.org/packages/anyservice.litedb)) 
2. Add the following lines to `ConfigureServices` method.

```
public void ConfigureServices(IServiceCollection services)
{
    ...
    var liteDbName = "your-db-name.db";
    
    // configure file storage
    services.AddTransient<IFileStoreManager>(sp => new LiteDbFileStoreManager(liteDbName));
    //configure db repositories
    services.AddTransient<IRepository<UserPermissions>>(sp => new LiteDbRepository<UserPermissions>(liteDbName));
    services.AddTransient<IRepository<DependentModel>>(sp => new LiteDbRepository<DependentModel>(liteDbName));

    // some custom db config
    using var db = new LiteDatabase(liteDbName);
    var mapper = BsonMapper.Global;

    mapper.Entity<DependentModel>().Id(d => d.Id);
    ...
}
```
