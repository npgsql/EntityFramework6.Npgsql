﻿#region License
// The PostgreSQL License
//
// Copyright (C) 2016 The Npgsql Development Team
//
// Permission to use, copy, modify, and distribute this software and its
// documentation for any purpose, without fee, and without a written
// agreement is hereby granted, provided that the above copyright notice
// and this paragraph and the following two paragraphs appear in all copies.
//
// IN NO EVENT SHALL THE NPGSQL DEVELOPMENT TEAM BE LIABLE TO ANY PARTY
// FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES,
// INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOFTWARE AND ITS
// DOCUMENTATION, EVEN IF THE NPGSQL DEVELOPMENT TEAM HAS BEEN ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
//
// THE NPGSQL DEVELOPMENT TEAM SPECIFICALLY DISCLAIMS ANY WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS FOR A PARTICULAR PURPOSE. THE SOFTWARE PROVIDED HEREUNDER IS
// ON AN "AS IS" BASIS, AND THE NPGSQL DEVELOPMENT TEAM HAS NO OBLIGATIONS
// TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.
#endregion

using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Globalization;
using System.Text;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Spatial;
using System.Linq;
using JetBrains.Annotations;

namespace Npgsql
{
    /// <summary>
    /// Used to generate migration sql
    /// </summary>
    public class NpgsqlMigrationSqlGenerator : MigrationSqlGenerator
    {
        List<MigrationStatement> _migrationStatments;
        List<string> _addedSchemas;
        List<string> _addedExtensions;
        Version _serverVersion;

        /// <summary>
        /// Generates the migration sql.
        /// </summary>
        /// <param name="migrationOperations">The operations in the migration</param>
        /// <param name="providerManifestToken">The provider manifest token used for server versioning.</param>
        public override IEnumerable<MigrationStatement> Generate(
            [NotNull] IEnumerable<MigrationOperation> migrationOperations,
            [NotNull] string providerManifestToken)
        {
            _migrationStatments = new List<MigrationStatement>();
            _addedSchemas = new List<string>();
            _addedExtensions = new List<string>();
            _serverVersion = new Version(providerManifestToken);
            Convert(migrationOperations);
            return _migrationStatments;
        }

        #region MigrationOperation to MigrationStatement converters

        #region General

        protected virtual void Convert([NotNull] IEnumerable<MigrationOperation> operations)
        {
            foreach (var migrationOperation in operations)
            {
                if (migrationOperation is AddColumnOperation operation)
                    Convert(operation);
                else if (migrationOperation is AlterColumnOperation columnOperation)
                    Convert(columnOperation);
                else if (migrationOperation is CreateTableOperation tableOperation)
                    Convert(tableOperation);
                else if (migrationOperation is DropForeignKeyOperation keyOperation)
                    Convert(keyOperation);
                else if (migrationOperation is DropTableOperation dropTableOperation)
                    Convert(dropTableOperation);
                else if (migrationOperation is MoveTableOperation moveTableOperation)
                    Convert(moveTableOperation);
                else if (migrationOperation is RenameTableOperation renameTableOperation)
                    Convert(renameTableOperation);
                else if (migrationOperation is AddForeignKeyOperation foreignKeyOperation)
                    Convert(foreignKeyOperation);
                else if (migrationOperation is DropIndexOperation indexOperation)
                    Convert(indexOperation);
                else if (migrationOperation is SqlOperation sqlOperation)
                    AddStatment(sqlOperation.Sql, sqlOperation.SuppressTransaction);
                else if (migrationOperation is AddPrimaryKeyOperation primaryKeyOperation)
                    Convert(primaryKeyOperation);
                else if (migrationOperation is CreateIndexOperation createIndexOperation)
                    Convert(createIndexOperation);
                else if (migrationOperation is RenameIndexOperation renameIndexOperation)
                    Convert(renameIndexOperation);
                else if (migrationOperation is DropColumnOperation dropColumnOperation)
                    Convert(dropColumnOperation);
                else if (migrationOperation is DropPrimaryKeyOperation dropPrimaryKeyOperation)
                    Convert(dropPrimaryKeyOperation);
                else if (migrationOperation is HistoryOperation historyOperation)
                    Convert(historyOperation);
                else if (migrationOperation is RenameColumnOperation renameColumnOperation)
                    Convert(renameColumnOperation);
                else if (migrationOperation is UpdateDatabaseOperation databaseOperation)
                    Convert(databaseOperation.Migrations as IEnumerable<MigrationOperation>);
                else
                    throw new NotImplementedException("Unhandled MigrationOperation " + migrationOperation.GetType().Name + " in " + GetType().Name);
            }
        }

        void AddStatment(string sql, bool suppressTransacion = false)
            => _migrationStatments.Add(new MigrationStatement
                {
                    Sql = sql,
                    SuppressTransaction = suppressTransacion,
                    BatchTerminator = ";"
                });

        void AddStatment(StringBuilder sql, bool suppressTransacion = false)
            => AddStatment(sql.ToString(), suppressTransacion);

        #endregion

        #region History

        protected virtual void Convert(HistoryOperation historyOperation)
        {
            foreach (var command in historyOperation.CommandTrees)
            {
                var npgsqlCommand = new NpgsqlCommand();
                NpgsqlServices.Instance.TranslateCommandTree(_serverVersion, command, npgsqlCommand, false);
                AddStatment(npgsqlCommand.CommandText);
            }
        }

        #endregion

        #region Tables

        protected virtual void Convert(CreateTableOperation createTableOperation)
        {
            var sql = new StringBuilder();
            var dotIndex = createTableOperation.Name.IndexOf('.');
            if (dotIndex != -1)
                CreateSchema(createTableOperation.Name.Remove(dotIndex));

            sql.Append("CREATE TABLE ");
            AppendTableName(createTableOperation.Name, sql);
            sql.Append('(');
            foreach (var column in createTableOperation.Columns)
            {
                AppendColumn(column, sql);
                sql.Append(",");
            }
            if (createTableOperation.Columns.Any())
                sql.Remove(sql.Length - 1, 1);
            if (createTableOperation.PrimaryKey != null)
            {
                sql.Append(",");
                sql.Append("CONSTRAINT ");
                sql.Append('"');
                sql.Append(createTableOperation.PrimaryKey.Name);
                sql.Append('"');
                sql.Append(" PRIMARY KEY ");
                sql.Append("(");
                foreach (var column in createTableOperation.PrimaryKey.Columns)
                {
                    sql.Append('"');
                    sql.Append(column);
                    sql.Append("\",");
                }
                sql.Remove(sql.Length - 1, 1);
                sql.Append(")");
            }
            sql.Append(")");
            AddStatment(sql);
        }

        protected virtual void Convert(DropTableOperation dropTableOperation)
        {
            var  sql = new StringBuilder();
            sql.Append("DROP TABLE ");
            AppendTableName(dropTableOperation.Name, sql);
            AddStatment(sql);
        }

        protected virtual void Convert(RenameTableOperation renameTableOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(renameTableOperation.Name, sql);
            sql.Append(" RENAME TO ");
            AppendTableName(renameTableOperation.NewName, sql);
            AddStatment(sql);
        }

        void CreateSchema(string schemaName)
        {
            if (schemaName == "public" || _addedSchemas.Contains(schemaName))
                return;
            _addedSchemas.Add(schemaName);
            if (_serverVersion.Major > 9 || (_serverVersion.Major == 9 && _serverVersion.Minor >= 3))
                AddStatment("CREATE SCHEMA IF NOT EXISTS " + QuoteIdentifier(schemaName));
            else
            {
                //TODO: CREATE PROCEDURE that checks if schema already exists on servers < 9.3
                AddStatment("CREATE SCHEMA " + QuoteIdentifier(schemaName));
            }
        }

        //void CreateExtension(string exensionName)
        //{
        //    //This is compatible only with server 9.1+
        //    if (serverVersion.Major > 9 || (serverVersion.Major == 9 && serverVersion.Minor >= 1))
        //    {
        //        if (addedExtensions.Contains(exensionName))
        //            return;
        //        addedExtensions.Add(exensionName);
        //        AddStatment("CREATE EXTENSION IF NOT EXISTS \"" + exensionName + "\"");
        //    }
        //}

        protected virtual void Convert(MoveTableOperation moveTableOperation)
        {
            var sql = new StringBuilder();
            var newSchema = moveTableOperation.NewSchema ?? "dbo";
            CreateSchema(newSchema);
            sql.Append("ALTER TABLE ");
            AppendTableName(moveTableOperation.Name, sql);
            sql.Append(" SET SCHEMA ");
            AppendQuotedIdentifier(newSchema, sql);
            AddStatment(sql);
        }

        #endregion

        #region Columns

        protected virtual void Convert(AddColumnOperation addColumnOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(addColumnOperation.Table, sql);
            sql.Append(" ADD ");
            AppendColumn(addColumnOperation.Column, sql);
            AddStatment(sql);
        }

        protected virtual void Convert(DropColumnOperation dropColumnOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(dropColumnOperation.Table, sql);
            sql.Append(" DROP COLUMN \"");
            sql.Append(dropColumnOperation.Name);
            sql.Append('"');
            AddStatment(sql);
        }

        protected virtual void Convert(AlterColumnOperation alterColumnOperation)
        {
            var sql = new StringBuilder();

            //TYPE
            AppendAlterColumn(alterColumnOperation, sql);
            sql.Append(" TYPE ");
            AppendColumnType(alterColumnOperation.Column, sql, false);
            AddStatment(sql);
            sql.Clear();

            //NOT NULL
            AppendAlterColumn(alterColumnOperation, sql);
            if (alterColumnOperation.Column.IsNullable != null && !alterColumnOperation.Column.IsNullable.Value)
                sql.Append(" SET NOT NULL");
            else
                sql.Append(" DROP NOT NULL");
            AddStatment(sql);
            sql.Clear();

            //DEFAULT
            AppendAlterColumn(alterColumnOperation, sql);
            if (alterColumnOperation.Column.DefaultValue != null)
            {
                sql.Append(" SET DEFAULT ");
                AppendValue(alterColumnOperation.Column.DefaultValue, sql);
            }
            else if (!string.IsNullOrWhiteSpace(alterColumnOperation.Column.DefaultValueSql))
            {
                sql.Append(" SET DEFAULT ");
                sql.Append(alterColumnOperation.Column.DefaultValueSql);
            }
            else if (alterColumnOperation.Column.IsIdentity)
            {
                sql.Append(" SET DEFAULT ");
                switch (alterColumnOperation.Column.Type)
                {
                    case PrimitiveTypeKind.Byte:
                    case PrimitiveTypeKind.SByte:
                    case PrimitiveTypeKind.Int16:
                    case PrimitiveTypeKind.Int32:
                    case PrimitiveTypeKind.Int64:
                        //TODO: need function CREATE SEQUENCE IF NOT EXISTS and set to it...
                        //Until this is resolved changing IsIdentity from false to true
                        //on types int2, int4 and int8 won't switch to type serial2, serial4 and serial8
                        throw new NotImplementedException("Not supporting creating sequence for integer types");
                    case PrimitiveTypeKind.Guid:
                        //CreateExtension("uuid-ossp");
                        //If uuid-ossp is not enabled migrations throw exception
                        AddStatment("select * from uuid_generate_v4()");
                        sql.Append("uuid_generate_v4()");
                        break;
                    default:
                        throw new NotImplementedException("Not supporting creating IsIdentity for " + alterColumnOperation.Column.Type);
                }
            }
            else
                sql.Append(" DROP DEFAULT");
            AddStatment(sql);
        }

        void AppendAlterColumn(AlterColumnOperation alterColumnOperation, StringBuilder sql)
        {
            sql.Append("ALTER TABLE ");
            AppendTableName(alterColumnOperation.Table, sql);
            sql.Append(" ALTER COLUMN \"");
            sql.Append(alterColumnOperation.Column.Name);
            sql.Append('"');
        }

        protected virtual void Convert(RenameColumnOperation renameColumnOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(renameColumnOperation.Table, sql);
            sql.Append(" RENAME COLUMN \"");
            sql.Append(renameColumnOperation.Name);
            sql.Append("\" TO \"");
            sql.Append(renameColumnOperation.NewName);
            sql.Append('"');
            AddStatment(sql);
        }

        #endregion

        #region Keys and indexes

        protected virtual void Convert(AddForeignKeyOperation addForeignKeyOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(addForeignKeyOperation.DependentTable, sql);
            sql.Append(" ADD CONSTRAINT \"");
            sql.Append(addForeignKeyOperation.Name);
            sql.Append("\" FOREIGN KEY (");
            foreach (var column in addForeignKeyOperation.DependentColumns)
            {
                sql.Append('"');
                sql.Append(column);
                sql.Append("\",");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append(") REFERENCES ");
            AppendTableName(addForeignKeyOperation.PrincipalTable, sql);
            sql.Append(" (");
            foreach (var column in addForeignKeyOperation.PrincipalColumns)
            {
                sql.Append('"');
                sql.Append(column);
                sql.Append("\",");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");

            if (addForeignKeyOperation.CascadeDelete)
                sql.Append(" ON DELETE CASCADE");
            AddStatment(sql);
        }

        protected virtual void Convert(DropForeignKeyOperation dropForeignKeyOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(dropForeignKeyOperation.DependentTable, sql);
            sql.Append(_serverVersion.Major < 9
                ? " DROP CONSTRAINT \""  //TODO: http://piecesformthewhole.blogspot.com/2011/04/dropping-foreign-key-if-it-exists-in.html ?
                : " DROP CONSTRAINT IF EXISTS \""
            );
            sql.Append(dropForeignKeyOperation.Name);
            sql.Append('"');
            AddStatment(sql);
        }

        protected virtual void Convert(CreateIndexOperation createIndexOperation)
        {
            var sql = new StringBuilder();
            sql.Append("CREATE ");

            if (createIndexOperation.IsUnique)
                sql.Append("UNIQUE ");

            sql.Append("INDEX \"");
            sql.Append(GetTableNameFromFullTableName(createIndexOperation.Table) + "_" + createIndexOperation.Name);
            sql.Append("\" ON ");
            AppendTableName(createIndexOperation.Table, sql);
            sql.Append(" (");
            foreach (var column in createIndexOperation.Columns)
            {
                sql.Append('"');
                sql.Append(column);
                sql.Append("\",");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            AddStatment(sql);
        }

        protected virtual void Convert(RenameIndexOperation renameIndexOperation)
        {
            var sql = new StringBuilder();

            sql.Append(_serverVersion.Major > 9 || (_serverVersion.Major == 9 && _serverVersion.Minor >= 2)
                ? "ALTER INDEX IF EXISTS "
                : "ALTER INDEX ");

            sql.Append(GetSchemaNameFromFullTableName(renameIndexOperation.Table));
            sql.Append(".\"");
            sql.Append(GetTableNameFromFullTableName(renameIndexOperation.Table) + "_" + renameIndexOperation.Name);
            sql.Append("\" RENAME TO \"");
            sql.Append(GetTableNameFromFullTableName(renameIndexOperation.Table) + "_" + renameIndexOperation.NewName);
            sql.Append('"');
            AddStatment(sql);
        }

        string GetSchemaNameFromFullTableName(string tableFullName)
        {
            var dotIndex = tableFullName.IndexOf('.');
            return dotIndex != -1 ? tableFullName.Remove(dotIndex) : "dto";   
            //TODO: Check always setting dto schema if no schema in table name is not bug
        }

        /// <summary>
        /// Removes schema prefix e.g. "dto.Blogs" returns "Blogs" and "Posts" returns "Posts"
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        string GetTableNameFromFullTableName(string tableName)
        {
            var dotIndex = tableName.IndexOf('.');
            return dotIndex != -1 ? tableName.Substring(dotIndex + 1) : tableName;
        }

        protected virtual void Convert(DropIndexOperation dropIndexOperation)
        {
            var sql = new StringBuilder();
            sql.Append("DROP INDEX IF EXISTS ");
            sql.Append(GetSchemaNameFromFullTableName(dropIndexOperation.Table));
            sql.Append(".\"");
            sql.Append(GetTableNameFromFullTableName(dropIndexOperation.Table) + "_" + dropIndexOperation.Name);
            sql.Append('"');
            AddStatment(sql);
        }

        protected virtual void Convert(AddPrimaryKeyOperation addPrimaryKeyOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(addPrimaryKeyOperation.Table, sql);
            sql.Append(" ADD CONSTRAINT \"");
            sql.Append(addPrimaryKeyOperation.Name);
            sql.Append("\" PRIMARY KEY ");

            sql.Append("(");
            foreach (var column in addPrimaryKeyOperation.Columns)
            {
                sql.Append('"');
                sql.Append(column);
                sql.Append("\",");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append(")");
            AddStatment(sql);
        }

        protected virtual void Convert(DropPrimaryKeyOperation dropPrimaryKeyOperation)
        {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE ");
            AppendTableName(dropPrimaryKeyOperation.Table, sql);
            sql.Append(" DROP CONSTRAINT \"");
            sql.Append(dropPrimaryKeyOperation.Name);
            sql.Append('"');
            AddStatment(sql);
        }

        #endregion

        #endregion

        #region Misc functions

        /// <summary>
        /// Quotes an identifier for Postgres SQL and appends it to a <see cref="StringBuilder" />
        /// </summary>
        /// <param name="identifier">The identifier to be quoted.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> used for building the query.</param>
        /// <returns>The quoted identifier.</returns>
        void AppendQuotedIdentifier(string identifier, StringBuilder builder)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("Value cannot be null or empty", nameof(identifier));

            if (identifier[identifier.Length - 1] == '"' && identifier[0] == '"')
                builder.Append(identifier);
            else
            {
                builder.Append('"');
                builder.Append(identifier);
                builder.Append('"');
            }
        }

        /// <summary>
        /// Quotes an identifier for Postgres SQL
        /// </summary>
        /// <param name="identifier">The identifier to be quoted.</param>
        /// <returns>The quoted identifier.</returns>
        string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("Value cannot be null or empty", nameof(identifier));

            return identifier[identifier.Length - 1] == '"' && identifier[0] == '"'
                ? identifier : $"\"{identifier}\"";
        }

        void AppendColumn(ColumnModel column, StringBuilder sql)
        {
            sql.Append('"');
            sql.Append(column.Name);
            sql.Append("\" ");
            AppendColumnType(column, sql, true);

            if (column.IsNullable != null && !column.IsNullable.Value)
                sql.Append(" NOT NULL");

            if (column.DefaultValue != null)
            {
                sql.Append(" DEFAULT ");
                AppendValue(column.DefaultValue, sql);
            }
            else if (!string.IsNullOrWhiteSpace(column.DefaultValueSql))
            {
                sql.Append(" DEFAULT ");
                sql.Append(column.DefaultValueSql);
            }
            else if (column.IsIdentity)
            {
                switch (column.Type)
                {
                case PrimitiveTypeKind.Guid:
                    //CreateExtension("uuid-ossp");
                    //If uuid-ossp is not enabled migrations throw exception
                    AddStatment("select * from uuid_generate_v4()");
                    sql.Append(" DEFAULT uuid_generate_v4()");
                    break;
                case PrimitiveTypeKind.Byte:
                case PrimitiveTypeKind.SByte:
                case PrimitiveTypeKind.Int16:
                case PrimitiveTypeKind.Int32:
                case PrimitiveTypeKind.Int64:
                    //TODO: Add support for setting "SERIAL"
                    break;
                }
            }
        }

        void AppendColumnType(ColumnModel column, StringBuilder sql, bool setSerial)
        {
            if (column.StoreType != null)
            {
                sql.Append(column.StoreType);
                return;
            }

            switch (column.Type)
            {
            case PrimitiveTypeKind.Binary:
                sql.Append("bytea");
                break;
            case PrimitiveTypeKind.Boolean:
                sql.Append("boolean");
                break;
            case PrimitiveTypeKind.DateTime:
                sql.Append(column.Precision != null
                    ? $"timestamp({column.Precision})"
                    : "timestamp"
                );
                break;
            case PrimitiveTypeKind.Decimal:
                //TODO: Check if inside min/max
                if (column.Precision == null && column.Scale == null)
                    sql.Append("numeric");
                else
                {
                    sql.Append("numeric(");
                    sql.Append(column.Precision ?? 19);
                    sql.Append(',');
                    sql.Append(column.Scale ?? 4);
                    sql.Append(')');
                }
                break;
            case PrimitiveTypeKind.Double:
                sql.Append("float8");
                break;
            case PrimitiveTypeKind.Guid:
                sql.Append("uuid");
                break;
            case PrimitiveTypeKind.Single:
                sql.Append("float4");
                break;
            case PrimitiveTypeKind.Byte://postgres doesn't support sbyte :(
            case PrimitiveTypeKind.SByte://postgres doesn't support sbyte :(
            case PrimitiveTypeKind.Int16:
                sql.Append(setSerial
                    ? column.IsIdentity ? "serial2" : "int2"
                    : "int2"
                );
                break;
            case PrimitiveTypeKind.Int32:
                sql.Append(setSerial
                    ? column.IsIdentity ? "serial4" : "int4"
                    : "int4"
                );
                break;
            case PrimitiveTypeKind.Int64:
                sql.Append(setSerial
                    ? column.IsIdentity ? "serial8" : "int8"
                    : "int8"
                );
                break;
            case PrimitiveTypeKind.String:
                if (column.IsFixedLength.HasValue &&
                    column.IsFixedLength.Value &&
                    column.MaxLength.HasValue)
                {
                    sql.Append($"char({column.MaxLength.Value})");
                }
                else if (column.MaxLength.HasValue)
                    sql.Append($"varchar({column.MaxLength})");
                else
                    sql.Append("text");
                break;
            case PrimitiveTypeKind.Time:
                if (column.Precision != null)
                {
                    sql.Append("interval(");
                    sql.Append(column.Precision);
                    sql.Append(')');
                }
                else
                    sql.Append("interval");
                break;
            case PrimitiveTypeKind.DateTimeOffset:
                if (column.Precision != null)
                {
                    sql.Append("timestamptz(");
                    sql.Append(column.Precision);
                    sql.Append(')');
                }
                else
                {
                    sql.Append("timestamptz");
                }
                break;
            case PrimitiveTypeKind.Geometry:
                sql.Append("point");
                break;
            //case PrimitiveTypeKind.Geography:
            //    break;
            //case PrimitiveTypeKind.GeometryPoint:
            //    break;
            //case PrimitiveTypeKind.GeometryLineString:
            //    break;
            //case PrimitiveTypeKind.GeometryPolygon:
            //    break;
            //case PrimitiveTypeKind.GeometryMultiPoint:
            //    break;
            //case PrimitiveTypeKind.GeometryMultiLineString:
            //    break;
            //case PrimitiveTypeKind.GeometryMultiPolygon:
            //    break;
            //case PrimitiveTypeKind.GeometryCollection:
            //    break;
            //case PrimitiveTypeKind.GeographyPoint:
            //    break;
            //case PrimitiveTypeKind.GeographyLineString:
            //    break;
            //case PrimitiveTypeKind.GeographyPolygon:
            //    break;
            //case PrimitiveTypeKind.GeographyMultiPoint:
            //    break;
            //case PrimitiveTypeKind.GeographyMultiLineString:
            //    break;
            //case PrimitiveTypeKind.GeographyMultiPolygon:
            //    break;
            //case PrimitiveTypeKind.GeographyCollection:
            //    break;
            default:
                throw new ArgumentException("Unhandled column type:" + column.Type);
            }
        }

        void AppendTableName(string tableName, StringBuilder sql)
        {
            var dotIndex = tableName.IndexOf('.');
            if (dotIndex == -1)
                AppendQuotedIdentifier(tableName, sql);
            else
            {
                AppendQuotedIdentifier(tableName.Remove(dotIndex), sql);
                sql.Append('.');
                AppendQuotedIdentifier(tableName.Substring(dotIndex + 1), sql);
            }
        }

        #endregion

        #region Value appenders

        void AppendValue(byte[] values, StringBuilder sql)
        {
            if (values.Length == 0)
                sql.Append("''");
            else
            {
                sql.Append("E'\\\\");
                foreach (var value in values)
                    sql.Append(value.ToString("X2"));
                sql.Append("'");
            }
        }

        void AppendValue(bool value, StringBuilder sql)
            => sql.Append(value ? "TRUE" : "FALSE");

        void AppendValue(DateTime value, StringBuilder sql)
        {
            sql.Append("'");
            sql.Append(new NpgsqlTypes.NpgsqlDateTime(value));
            sql.Append("'");
        }

        void AppendValue(DateTimeOffset value, StringBuilder sql)
        {
            sql.Append("'");
            sql.Append(new NpgsqlTypes.NpgsqlDateTime(value.UtcDateTime));
            sql.Append("'");
        }

        void AppendValue(Guid value, StringBuilder sql)
        {
            sql.Append("'");
            sql.Append(value);
            sql.Append("'");
        }

        void AppendValue(string value, StringBuilder sql)
        {
            sql.Append("'");
            sql.Append(value);
            sql.Append("'");
        }

        void AppendValue(TimeSpan value, StringBuilder sql)
        {
            sql.Append("'");
            sql.Append(new NpgsqlTypes.NpgsqlTimeSpan(value));
            sql.Append("'");
        }

        void AppendValue(DbGeometry value, StringBuilder sql)
        {
            sql.Append("'");
            sql.Append(value);
            sql.Append("'");
        }

        void AppendValue(object value, StringBuilder sql)
        {
            if (value is byte[] bytes)
                AppendValue(bytes, sql);
            else if (value is bool b)
                AppendValue(b, sql);
            else if (value is DateTime time)
                AppendValue(time, sql);
            else if (value is DateTimeOffset offset)
                AppendValue(offset, sql);
            else if (value is Guid guid)
                AppendValue(guid, sql);
            else if (value is string s)
                AppendValue(s, sql);
            else if (value is TimeSpan timeSpan)
                AppendValue(timeSpan, sql);
            else if (value is DbGeometry geometry)
                AppendValue(geometry, sql);
            else
                sql.Append(string.Format(CultureInfo.InvariantCulture, "{0}", value));
        }

        #endregion
    }
}
