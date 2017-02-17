---
layout: doc
title: Entity Framework 6
---

Npgsql has an Entity Framework 6 provider. You can use it by installing the
[EntityFramework6.Npgsql](https://www.nuget.org/packages/EntityFramework6.Npgsql/) nuget.

## Guid Support ##

Npgsql EF migrations support uses `uuid_generate_v4()` function to generate guids.
In order to have access to this function, you have to install the extension uuid-ossp through the following command:

```sql
create extension "uuid-ossp";
```

If you don't have this extension installed, when you run Npgsql migrations you will get the following error message:

```
ERROR:  function uuid_generate_v4() does not exist
```

If the database is being created by Npgsql Migrations, you will need to
[run the `create extension` command in the `template1` database](http://stackoverflow.com/a/11584751).
This way, when the new database is created, the extension will be installed already.

## Template Database ##

When the Entity Framework 6 provider creates a database, it issues a simple `CREATE DATABASE` command.
In PostgreSQL, this implicitly uses `template1` as the template - anything existing in `template1` will
be copied to your new database. If you wish to change the database used as a template, you can specify
the `EF Template Database` connection string parameter. For more info see the
[PostgreSQL docs](https://www.postgresql.org/docs/current/static/sql-createdatabase.html).

## Customizing DataReader Behavior ##

You can use [an Entity Framework 6 IDbCommandInterceptor](https://msdn.microsoft.com/en-us/library/dn469464(v=vs.113).aspx) to wrap the `DataReader` instance returned by Npgsql when Entity Framework executes queries. This is possible using a ```DbConfiguration``` class.

Example use cases:
- Forcing all returned ```DateTime``` and ```DateTimeOffset``` values to be in the UTC timezone.
- Preventing accidental insertion of DateTime values having ```DateTimeKind.Unspecified```.
- Forcing all postgres date/time types to be returned to Entity Framework as ```DateTimeOffset```.

[Here is an example of a fully implemented EF6 interceptor](sample-interceptor.md)

```c#
[DbConfigurationType(typeof(AppDbContextConfiguration))]
public class AppDbContext : DbContext
{
    // ...
}

public class AppDbContextConfiguration : DbConfiguration
{
    public AppDbContextConfiguration()
    {
        this.AddInterceptor(new MyEntityFrameworkInterceptor());
    }
}

class MyEntityFrameworkInterceptor : DbCommandInterceptor
{
    public override void ReaderExecuted(
        DbCommand command,
        DbCommandInterceptionContext<DbDataReader> interceptionContext)
    {
        if (interceptionContext.Result == null) return;
        interceptionContext.Result = new WrappingDbDataReader(interceptionContext.Result);
    }

    public override void ScalarExecuted(
        DbCommand command,
        DbCommandInterceptionContext<object> interceptionContext)
    {
        interceptionContext.Result = ModifyReturnValues(interceptionContext.Result);
    }
    
    static object ModifyReturnValues(object result)
    {
        // Transform and then
        return result;
    }
}

class WrappingDbDataReader : DbDataReader, IDataReader
{
    // Wrap an existing DbDataReader, proxy all calls to the underlying instance, 
    // modify return values and/or parameters as needed...
    public WrappingDbDataReader(DbDataReader reader)
    {
    }
}
```
