# AnyService

Create asp.net core services FAST 🐱‍🏍

Made with 💕 using asp.net core

`AnyService` is simple to use, yet strong middleware for creating CRUD asp.net core services.
The boilerplate code is already in place. All you have to do now is to configure your objects and you are ready to go!

## Goal

`AnyService` main goal is to provide extremely easy and fast way to create CRUD based microservice using asp.net core technology.

## Getting Started

\*Note: fully configured example may be found in `AnyService.SampleApp` project.

init step - Create new `webapi` project by using `dotnet new webapi --name AnyService.SampleApp` command.

1. Add reference to `AnyService` **_Note: nuget package would be created in near future, meanwhile create git submodule in your project_**
2. Create your dependent model. This dependent model is used to perform all `CRUD` operations

```
public class DependentModel : IDomainModelBase //this must be implemented for Repository operations
{
    public string Id { get; set; }
    public string Value { get; set; }
}
```

3. Create validator. The validator role is to provide the busines logic for `CRUD` operations.

```
public class DependentModelValidator : ICrudValidator<DependentModel>
{
    public Type Type => typeof(DependentModel);
    public Task<bool> ValidateForCreate(DependentModel model, ServiceResponse serviceResponse)
    {
        return Task.FromResult(true); //always permit to create model
    }

    public Task<bool> ValidateForDelete(string id, ServiceResponse serviceResponse)
    {
        return Task.FromResult(true);//always permit to delete model
    }

    public Task<bool> ValidateForGet(ServiceResponse serviceResponse)
    {
        return Task.FromResult(true);//always permit to read model
    }

    public Task<bool> ValidateForUpdate(DependentModel model, ServiceResponse serviceResponse)
    {
        return Task.FromResult(true);//always permit to update model
    }
}
```

4. Add `AnyService` components to `Startup.cs` file: In `ConfigureServices` method, add the following lines:

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  services
    .AddMvc()
    .AddControllersAsServices(); //resolve controllers dynamically using dependency injection

  var entities = new[] { typeof(DependentModel) };
  var validators = new[] { new DependentModelValidator() };

  services.AddAnyService(Configuration, entities, validators);
  ...
}
```

5. Configure your `IRepository` implementation by adding the following to `ConfigureServices` method (this is `LiteDb` `IRepository` pattern implementation)

```
public void ConfigureServices(IServiceCollection services)
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

6. Last step is to add `AnyService` middleware to middlewares pipeline. Add the following line to `Configure` method of `Startup.cs`

```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
  ...
  app.UseMiddleware<AnyServiceMiddleware>();
  ...
}
```

Now start your application and perform _CRUD_ operations on `dependent` URI.

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

## Authentication

Authentication is fully handled by `asp.net core`. Please refer to `asp.net core` documentation to implement your authentication model.

## Authorization

Unlike authentication, authorization is fully handled by `AnyService`.
Authorization has 2 aspects addressed by `AnyService`:

1. Configuring user's access to specific resource (URI) aka _Can user perform CRUD operation on a given URI?_

2. Manage CRUD Permissions on an authorized resource (URI) aka _Given that a user has permission to perform CRUD operation on given URI, can the user perform read/update/delete operations on specific entity?_

### Configuring user's access to specific resource (URI)

Configuring authrozation for specific controller is done by sending an instance of `AuthorizeAttribute` to the registration of you model.
You may set authorization to controller and override each CRUD method.

TBD - add example for controller authorization
TBD - add example for controller authorization with CRUD method override
TBD - add example for CRUD method authorization

### Manage CRUD Permissions on an authorized resource (URI)

By default once a user creates an entity on a resource, `AnyService` takes care to manage permissions over the created entity. Once entity is created, the creator, and only the creator has the previliges to perform CRUD operations on it, while other users are blocked.

TBD - Show how to disable the authz behavior in configuration

## Model Validation

Whe `POST` and `PUT` Http methods are used, the data within request's body is called model.
By default `AnyService` does not perform any model validation, and leaves it to the client side.
If you want to perform model validation, you should implement `ICrudValidator<T>` generic interface.

TBD - add example here

## Audity

TBD

## Combine Custom Controllers with `AnyEntity` Middleware

TBD

## Reserve Paths

Any resource that starts with `__`

## Contact Me

Feel free to write me directly to roi@saturn72.com 📧
