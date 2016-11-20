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
