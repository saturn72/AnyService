![anyservice ci](https://github.com/saturn72/AnyService/workflows/anyservice%20ci/badge.svg)

# AnyService

Create asp.net core services FAST 🐱‍🏍

Made with 💕 using asp.net core

`AnyService` is simple to use, yet strong middleware for creating CRUD asp.net core services.
The boilerplate code is already in place. All you have to do now is to configure your objects and you are ready to go!

## Goal

`AnyService` main goal is to provide extremely easy and fast way to create CRUD based microservice using asp.net core technology.

## Main Features

- Fully comaptible with `asp.net core` - `AnyService` is framework on top of `asp.net core`
- Audity management - auto manage for creation, update and deletion of an entity
- Permission management - entity can be restricted as private or public over simple configuration
- Event sourcing - Main `CRUD` events over an entity are publish
- Modularity - build in modules system
- Full test coverage - code is fully tested so you can rely infrastructure was tested and verified!
- Caching - to minimize database calls and increase performance\*

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
3. Create `DbContext` (it must have `public DbSet<UserPermissions> UserPermissions { get; set; }` in order to manage user permissions on an entity)

```
public class SampleAppDbContext : DbContext, IAnyServiceDbContext
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
    
  var options = new DbContextOptionsBuilder<AvosetDbContext>()
      .UseInMemoryDatabase(databaseName: dbName).Options;
  services.AddTransient<DbContext>(sp  => new SampleAppDbContext>(options)); //inject dbContext instance
  services.AddTransient(typeof(IRepository<Model>), typeof(EfRepository<>); //inject Generic repository
  services.AddTransient<IFileStoreManager, EfFileStoreManager>(); //injects file storage
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

#### 6. The final step is to add `AnyService` middlewares to pipeline

Add the following line to `Configure` method of `Startup.cs`

```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
  //other middleware before
  app.UseAnyService();
  //other middleware after
}
```

#### 7. Start your app

Now hit F5 to start your application and perform _CRUD_ operations on `dependent` URI.

## CRUD Flow

### Create

TBD

### Read By Id
1. Validate user is Authenticated and has permission to read entity
2. Entity Details is fetched more database
3. Event with `entity-read` key is fired
4. Entity details id returned to user 

### Read All (with filter)
Read all api has 2 scenarios to be used. The first is when a user fetches all entities under his account and the second is when a user fetches ALL public entities of the endpoint.
#### User fetches entities under his account
1. Validate user is Authenticated
2. Entity Details are queried from database, based on user permissions and filter
3. Event with `entity-read` key is fired
4. Entities details returned to user 
#### User fetches entities under his account (`__public` route)
1. Validate user is Authenticated and that the entity has public API capability.
2. All endpoint's entities are queried from database based on filter
3. Event with `entity-read` key is fired
4. Entities details returned to user 

### Update

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

## Response Mapping

By Default `ServiceResponse` is returned from business logic layer (`CrudService<>`) to the entity controller(`GenericController`). The controller maps the returned `ServiceResponse` to `IActionResult` object and return to caller.
2 types of response mappers exist in `AnyService`:
 1. `DataOnlyServiceResponseMapper` (the default mapper) that returns `ServiceResponse`'s data only (recomended for production)
 2. `DefaultServiceResponseMapper` which returns all the `ServiceResponse`'s details (recomended for debugging and response monitoring)
 

To modify the `IServiceResponseMapper` used for an entity, set the value of `AnyService.EntityConfigRecord.ResponseMapperType` to the required type.

## Authorization (Permission Management)

Authorization has 2 aspects addressed by `AnyService`:
### 1. Configuring user's access to specific resource (URI)
#### _Can user perform CRUD operation on a given URI?_

This snippet shows how to set role based authorization on controller and its methods

```
public void ConfigureServices(IServiceCollection services)
{
  ...
  var config = new AnyServiceConfig
              {
                EntityConfigRecords = new[]{
                    new EntityConfigRecord
                    {
                        Type = typeof(Product),
                        Authorization = new AuthorizationInfo
                        {
                            ControllerAuthorizationNode = new AuthorizationNode //only users with role "admin" can access the controller
                            {
                                Roles = new []{"admin"},
                            },
                            PostAuthorizeNode = new AuthorizationNode //only users with role "creator" can access the controller
                            {
                                Roles = new []{"creator"},
                            },
                            GetAuthorizeNode = new AuthorizationNode //only users with role "reader" can access the controller
                            {
                                Roles = new []{"reader"},
                            },
                            PutAuthorizeNode = new AuthorizationNode //only users with role "updater" can access the controller
                            {
                                Roles = new []{"updater"},
                            },
                            DeleteAuthorizeNode = new AuthorizationNode //only users with role "deleter" can access the controller
                            {
                                Roles = new []{"deleter"},
                            }
                        }
                    }

                }
            };
    services.AddAnyService(config);
    ...
}          
```
### 2. Manage CRUD Permissions on an authorized resource (URI)
#### _Given that a user has permission to perform CRUD operation on given URI (entity), can the user perform read/update/delete operations on specific URI's entity?_

Once user has permission to create an entity on a resource, `AnyService` takes care of managing permissions over the created entity.
`AnyService` supports 2 permission management patterns:

1. All CRUD operations can be performed by the creator and only by the creator.
   This means the entities created by a priliged user are fully private and are **managed and accessed** by this user (entity's creator) and only by this user.

2. Create, Update and Delete operations can be performed by the creator and only by the creator while Read operation can be performed by all users.
   This means the entities created by a priliged user are **managed** privatly by this user (entity's creator) and only by this user, but are publicly **accessed** by all users.


### Disable/Modify Default Authorization Behavior
#### Disable Default Authorization 
`AnyService` uses the `DefaultAuthorizationMiddleware` to verify user has the required `Claim`s to access entity's `URI` (controller and/or controller's method).
To disable Permission Management, set the value of `AnyServiceOptions.UseAuthorizationMiddleware` to `false`.

#### Permission Management
Another aspect of authrization management is managing user **permissions** to perform `CRUD`'s on `URI` or an entity. 
There are several options to modify permission management:
- To disable Permission Management, set the value of `ManageEntityPermissions` of `AnyServiceOptions` class to `false`.
- To modify default permission logic you may:
 - Create your own implementation of `IPermissionManager`
 - Create your own permission middleware

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

If you want to utilize `AnyService` automatic setup and/or require custom controller, it is required to configure your entity in `AnyServiceConfig` (simply by adding it to the `entityConfigRecords` collection).
To add custom controller (in case you want to override the configured route, for instance), set the property `ControllerType` with the custom controller type you want to use.

```
[Route("api/my-great-route")]
public class MyCustomController : ControllerBase
{
  ... all HTTP method handlers
}

public void ConfigureServices(IServiceCollection services)
{
  ...
  var anyServiceConfig = new AnyServiceConfig
  {
    EntityConfigRecords = new[]
    {
      new EntityConfigRecord
      {
        Type =typeof(Value),
        Route = "/api/my-great-route",
        ControllerType = typeof(MyCustomController),
      }
    },
  };
  ...
}
```

## Reserve Paths

Any resource that starts with `__`

## Contact Me

Feel free to write me directly to roi@saturn72.com 📧
