![anyservice ci](https://github.com/saturn72/AnyService/workflows/anyservice%20ci/badge.svg)

# AnyService

Create asp.net core services FAST 🐱‍🏍

Made with 💕 using asp.net core

`AnyService` is simple to use, yet strong middleware for creating CRUD asp.net core services.
The boilerplate code is already in place. All you have to do now is to configure your objects and you are ready to go!

## Goal

`AnyService` main goal is to provide extremely easy and fast way to create CRUD based microservice using asp.net core technology.

## Features

* Audity management - auto manage for creation, update and deletion of an entity 
* Permission management - entity can be restricted as private or public over simple configuration
* Event sourcing - Main `CRUD` events over an entity are publish 
* Modularity - build in modules system
* Full test coverage - code is fully tested so you can rely infrastructure was tested and verified!
* Caching - to minimize database calls and increase performance* 

## Getting Started

\*Note: fully configured example may be found in `AnyService.SampleApp` project.

init step - Create new `webapi` project by using `dotnet new webapi --name AnyService.SampleApp` command.

#### 1. Add reference to `AnyService` nuget package

(see [here](https://www.nuget.org/packages/anyservice/))

#### 2. Create a model (entity) that you want to use as resource

This model is used to perform all `CRUD` operations of the web service. It must implement `IDomainModelBase` to "glue" it to `AnyService`'s business logic.

```
public class DependentModel : IDomainModelBase //Your model must implement IDomainModelBase
{
    public string Id { get; set; }
    public string Value { get; set; }
}
```

#### 3. Add `AnyService` components to `Startup.cs` file

In `ConfigureServices` method, add the following lines:

```
public void ConfigureServices(IServiceCollection services)
{
  ...

  var entities = new[] { typeof(DependentModel) }; //list all your entities
  services.AddAnyService(entities);
  ...
}
```

#### 4. Configure caching and persistency functions

**Persistency** is achieved by `IRepository` implementation. Below is `EntityFramework` (`InMemory` provider) example.

1. Add reference to `AnyService.EntityFramework` nuget package (see [here](https://www.nuget.org/packages/anyservice.entityframework))
2. Add reference to `Microsoft.EntityFrameworkCore.InMemory` nuget packages (see [here](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/?tabs=dotnet-core-cli)) 
3. Create `DbContext`
```
public class SampleAppDbContext : DbContext
    {
        public SampleAppDbContext(DbContextOptions<SampleAppDbContext> options) : base(options)
        { }
        public DbSet<UserPermissions> UserPermissions { get; set; }
        public DbSet<DependentModel> DependentModel { get; set; }
        public DbSet<Dependent2> Dependent2s { get; set; }
        public DbSet<MultipartSampleModel> MultipartSampleModels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPermissions>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<DependentModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<Dependent2>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
            modelBuilder.Entity<MultipartSampleModel>(b => b.Property(u => u.Id).ValueGeneratedOnAdd());
        }
    }
 ```
4. Add the following lines to `ConfigureServices` method.

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  var dbName = "anyservice-testsapp-db";
  services.AddTransient<IRepository<DependentModel>>(sp => new AnyService.EntityFramework.EfRepository<DependentModel>(liteDbName));
  services.AddTransient<IRepository<UserPermissions>>(sp => new Repository<UserPermissions>(liteDbName));
  using (var db = new LiteDatabase(liteDbName))
  {
      var mapper = BsonMapper.Global;
      mapper.Entity<DependentModel>().Id(d => d.Id);
  }
  ...
}
```

**Caching** is achieved by `ICacheManager` implementation. Here is `EasyCaching.InMemory` example: : add reference to `AnyService.EasyCaching` nuget (see [here](https://www.nuget.org/packages/anyservice.easycaching)) and to `EasyCaching.InMemory` (see[here](https://www.nuget.org/packages/EasyCaching.InMemory/)) and adding the following lines to `ConfigureServices` method.

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  var easycachingconfig = new EasyCachingConfig();
  services.AddSingleton(easycachingconfig);
  services.AddEasyCaching(options => options.UseInMemory("default"));
  services.AddSingleton<ICacheManager, EasyCachingCacheManager>();
  ...
}
```

#### 5. Add Authentication

`AnyService` must have authentication configured (otherwise 401 Unauthorized response is always returned). `AnyService` provides mocked `AuthenticationHandler` that injects claims to Request's User. This done by using `AddAlwaysPassAuthentication` extension method.

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  services.AddAlwaysPassAuthentication("abcd-1234", null); //
  ...
}
```

#### 6. The final step is to add `AnyService` middleware to pipeline

Add the following line to `Configure` method of `Startup.cs`

```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
  //other middleware before
  app.UseMiddleware<AnyServiceMiddleware>();
  //other middleware after
}
```

#### 7. Start your app

Now hit F5 to start your application and perform _CRUD_ operations on `dependent` URI.

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

Authenticating a user is mandatory in `AnyService`. Some of the main reasons are:

- `UserId` is required to manage permissions access over entities
- A user-based-event is raised whenever `CRUD` operation is performed
- `Audity` feature data management heavily relies on user's info
- etc.

Authentication is added and configured using `asp.net core`'s default authentication configuration mechanism.
**important:** user's id claim type must be "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" (defined by `System.Security.Claims.ClaimTypes.NameIdentifier` constant).

For development purposes, you may configure `AlwaysPassAuthenticationHandler` which, as implied, always approves incoming request's user as authenticated one, with pre-configured id value and claims.
To use `AlwaysPassAuthenticationHandler` simply use `AddAlwaysPassAuthentication` extension method.

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  var userId = "some-user-id";
  var claims = new []
  {
    new KeyValuePair<string, string>("claim1-key", "claim1-value"),
    new KeyValuePair<string, string>("claim1-key", "claim1-value"),
    new KeyValuePair<string, string>("claim1-key", "claim1-value"),
  };
  services.AddAlwaysPassAuthentication(userId, claims); // This
  ...
}
```

## Authorization (Permission Management)

By default Authorization is fully handled by `AnyService`. To disable this, set the value of `ManageEntityPermissions` of `AnyServiceOptions` class to `false`.

Authorization has 2 aspects addressed by `AnyService`:

### 1. Configuring user's access to specific resource (URI), hence: _Can user perform CRUD operation on a given URI?_

<!--
Configuring authrozation for specific controller is done by sending an instance of `AuthorizeAttribute` to the registration of you model.
You may set authorization to controller and override each CRUD method. -->

TBD - add example for controller authorization
TBD - add example for controller authorization with CRUD method override
TBD - add example for CRUD method authorization

### 2. Manage CRUD Permissions on an authorized resource (URI), hence: _Given that a user has permission to perform CRUD operation on given URI (entity), can the user perform read/update/delete operations on specific URI's entity?_

Once user has permission to create an entity on a resource, `AnyService` takes care of managing permissions over the created entity.
`AnyService` supports 2 permission management patterns:

1. All CRUD operations can be performed by the creator and only by the creator.
   This means the entities created by a priliged user are fully private and are **managed and accessed** by this user (entity's creator) and only by this user.

2. Create, Update and Delete operations can be performed by the creator and only by the creator while Read operation can be performed by all users.
   This means the entities created by a priliged user are **managed** privatly by this user (entity's creator) and only by this user, but are publicly **accessed** by all users.

TBD - Show how to disable the authz behavior in configuration

## Model Validation

Whe `POST` and `PUT` Http methods are used, the data within request's body is called model.
By default `AnyService` does not perform any model validation, and leaves it to the client side.
If you want to perform model validation, you should implement `ICrudValidator<T>` generic interface.

TBD - add example here

## Audity

TBD

## CRUD Events

`AnyService` raises event whenever CRUD operation is preformed.

TBD - show how to consume event
TBD - show how to modify event key

## `AnyServiceConfig` - Customize default values

You are able to customize all default values of `AnyService`.
Most of `AnyService` properties can be modified by creating instance of `AnyServiceConfig` and set relevant properties. Then send it to `AddAnyService` extension method.

In the example below we modify entity route.
By default route is set to entity's name (using `Type.Name`).
We use `HeatMapInfo` entity which by default gets the route `/heatmapinfo` for its `CRUD` operations. By setting the `EntityConfigRecord.Route` property to `/heatmap`, `CRUD` operations are performed in `/heatmap` route.

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  var anyServiceConfig = new AnyServiceConfig
  {
    EntityConfigRecords = new[]
    {
      new EntityConfigRecord
      {
        Type =typeof(HeatMapInfo),
        Route = "/heatmap"
      }
    },
  };
  ...
}
```

## Combine Custom Controllers with `AnyEntity` Middleware

TBD

## Reserve Paths

Any resource that starts with `__`

## Contact Me

Feel free to write me directly to roi@saturn72.com 📧
