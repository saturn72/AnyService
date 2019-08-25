# AnyService
Create asp.net core services FAST 🐱‍🏍 

Made with 💕 using asp.net core

`AnyService` is simple framework for creating CRUD asp.net core service.
The boilerplate code is already in place. All you have to do now is to create some basic code and you are ready to go!

## Goal
`AnyService` main goal is to provide extremely easy and fast way to create CRUD based microservice using asp.net core technology.

## CRUD Flow
### Create
TBD
### Read By Id
TBD
### Read All (with filter)
TBD
### Update
TBD
### Patch
TBD
### Delete
TDB

## Getting Started
Before you begin, this is part of the `AnyService.SampleApp` project.

1. Add reference to `AnyService` ***Note: nuget package would be created in near future, meanwhile create git submodule in your project***
2. Create your dependent model 
3. Create validator
4. Add `AnyService` components to `Startup.cs` file.
In `ConfigureServices` method, add the follwing lines:
```
{
  ...
  services.AddControllersAsServices(); //resolve controllers dynamically using dependency injection
  var entities = new[] { typeof(DependentModel) };
  var validators = new[] { new DependentModelValidator() };
    
  services.AddAnyService(Configuration, entities, validators);   
  ...
}
```
5. Configure your `IRepository` implementation by adding the following to `ConfigureServices` method (below is `LiteDb` `IRepository` implementation)
```
{
  ...
  var liteDbName = "anyservice-testsapp.db";
  services.AddTransient<IRepository<DependentModel>>(sp => new AnyService.LiteDbRepository.Repository<DependentModel>(liteDbName));
  using (var db = new LiteDatabase(liteDbName))
  {
      var mapper = BsonMapper.Global;
      mapper.Entity<DependentModel>().Id(d => d.Id);
  }
  ...
}
```
6. Add `AnyService` middleware by adding the following line to `Configure` method of `Startup.cs`
```
{
  ...
  app.UseMiddleware<AnyServiceMiddleware>();
  ...
}
```

TBD

## Combine Custom Controllers with `AnyEntity` Middleware
TBD

## Audity
TBD

## Caching
TBD

## Contact Me
Feel free to write me directly to roi@saturn72.com 📧
