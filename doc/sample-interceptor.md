### Sample Entity Framework 6 Interceptor ###

The following is a sample interceptor class that forces all returned date/time types from Npgsql to be converted to UTC (if local) or specified as being UTC (if ```DateTimeKind.Unknown```).

```c#
using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.IO;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;

namespace My.Interceptors
{
    class EnforceDateTimeUtcInterceptor : DbCommandInterceptor
    {
        public override void NonQueryExecuting(
            DbCommand command,
            DbCommandInterceptionContext<int> interceptionContext)
        {
            EnsureThatAllDateTimePropertiesAreUtc(command.Parameters);
        }

        public override void ReaderExecuting(
            DbCommand command,
            DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            EnsureThatAllDateTimePropertiesAreUtc(command.Parameters);
        }

        public override void ScalarExecuting(
            DbCommand command,
            DbCommandInterceptionContext<object> interceptionContext)
        {
            EnsureThatAllDateTimePropertiesAreUtc(command.Parameters);
        }

        public override void ReaderExecuted(
            DbCommand command,
            DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            if (interceptionContext.Result == null) return;
            interceptionContext.Result = new EnforcingDbDataReader(interceptionContext.Result);
        }

        public override void ScalarExecuted(
            DbCommand command,
            DbCommandInterceptionContext<object> interceptionContext)
        {
            interceptionContext.Result = ConvertToUtcOrReturn(interceptionContext.Result);
        }

        static void EnsureThatAllDateTimePropertiesAreUtc(DbParameterCollection parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            foreach (DbParameter parameter in parameters)
            {
                var dateTime = parameter.Value as DateTime?;
                if (dateTime != null && dateTime.Value != DateTime.MinValue && dateTime.Value.Kind != DateTimeKind.Utc)
                {
                    throw new InvalidOperationException(
                        $"Parameter '{parameter.ParameterName}' must have a Utc value. A value of {dateTime.Value.Kind} was found instead.");
                }

                var dateTimeOffset = parameter.Value as DateTimeOffset?;
                if (dateTimeOffset != null && dateTimeOffset.Value.Offset != TimeSpan.Zero)
                {
                    throw new InvalidOperationException(
                        $"Parameter '{parameter.ParameterName}' must have a Utc value. An offset of {dateTimeOffset.Value.Offset} was found instead.");
                }
            }
        }

        static object ConvertToUtcOrReturn(object obj)
        {
            if (obj == null) return null;

            var dateTime = obj as DateTime?;
            if (dateTime != null)
            {
                return ConvertToUtc(dateTime.Value);
            }

            var dateTimeOffset = obj as DateTimeOffset?;
            if (dateTimeOffset != null)
            {
                return dateTimeOffset.Value.ToUniversalTime();
            }

            return obj;
        }

        static DateTime ConvertToUtc(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue) return dateTime;

            return dateTime.Kind == DateTimeKind.Local
                ? dateTime.ToUniversalTime()
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        class EnforcingDbDataReader : DbDataReader, IDataReader
        {
            readonly DbDataReader _reader;

            public override int Depth => this._reader.Depth;
            public override int FieldCount => this._reader.FieldCount;
            public override bool HasRows => this._reader.HasRows;
            public override bool IsClosed => this._reader.IsClosed;
            public override int RecordsAffected => this._reader.RecordsAffected;
            public override int VisibleFieldCount => this._reader.VisibleFieldCount;
            public override object this[string name] => ConvertToUtcOrReturn(this._reader[name]);
            public override object this[int ordinal] => ConvertToUtcOrReturn(this._reader[ordinal]);

            public EnforcingDbDataReader(DbDataReader reader)
            {
                if (reader == null) throw new ArgumentNullException(nameof(reader));
                this._reader = reader;
            }

            public override void Close()
            {
                this._reader.Close();
            }

            public override ObjRef CreateObjRef(Type requestedType)
            {
                return this._reader.CreateObjRef(requestedType);
            }

            public override bool GetBoolean(int ordinal)
            {
                return this._reader.GetBoolean(ordinal);
            }

            public override byte GetByte(int ordinal)
            {
                return this._reader.GetByte(ordinal);
            }

            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
            {
                return this._reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
            }

            public override char GetChar(int ordinal)
            {
                return this._reader.GetChar(ordinal);
            }

            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
            {
                return this._reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
            }

            IDataReader IDataRecord.GetData(int i)
            {
                return ((IDataRecord)this._reader).GetData(i);
            }

            public override string GetDataTypeName(int ordinal)
            {
                return this._reader.GetDataTypeName(ordinal);
            }

            public override DateTime GetDateTime(int ordinal)
            {
                return ConvertToUtc(this._reader.GetDateTime(ordinal));
            }

            public override decimal GetDecimal(int ordinal)
            {
                return this._reader.GetDecimal(ordinal);
            }

            public override double GetDouble(int ordinal)
            {
                return this._reader.GetDouble(ordinal);
            }

            public override IEnumerator GetEnumerator()
            {
                return this._reader.GetEnumerator();
            }

            public override Type GetFieldType(int ordinal)
            {
                return this._reader.GetFieldType(ordinal);
            }

            public override T GetFieldValue<T>(int ordinal)
            {
                return (T)ConvertToUtcOrReturn(this._reader.GetFieldValue<T>(ordinal));
            }

            public override async Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
            {
                return (T)ConvertToUtcOrReturn(
                    await this._reader
                        .GetFieldValueAsync<T>(ordinal, cancellationToken)
                        .ConfigureAwait(true));
            }

            public override float GetFloat(int ordinal)
            {
                return this._reader.GetFloat(ordinal);
            }

            public override Guid GetGuid(int ordinal)
            {
                return this._reader.GetGuid(ordinal);
            }

            public override short GetInt16(int ordinal)
            {
                return this._reader.GetInt16(ordinal);
            }

            public override int GetInt32(int ordinal)
            {
                return this._reader.GetInt32(ordinal);
            }

            public override long GetInt64(int ordinal)
            {
                return this._reader.GetInt64(ordinal);
            }

            public override string GetName(int ordinal)
            {
                return this._reader.GetName(ordinal);
            }

            public override int GetOrdinal(string name)
            {
                return this._reader.GetOrdinal(name);
            }

            public override Type GetProviderSpecificFieldType(int ordinal)
            {
                return this._reader.GetProviderSpecificFieldType(ordinal);
            }

            public override object GetProviderSpecificValue(int ordinal)
            {
                return ConvertToUtcOrReturn(this._reader.GetProviderSpecificValue(ordinal));
            }

            public override int GetProviderSpecificValues(object[] values)
            {
                var result = this._reader.GetProviderSpecificValues(values);
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = ConvertToUtcOrReturn(values[i]);
                }
                return result;
            }

            public override DataTable GetSchemaTable()
            {
                return this._reader.GetSchemaTable();
            }

            public override Stream GetStream(int ordinal)
            {
                return this._reader.GetStream(ordinal);
            }

            public override string GetString(int ordinal)
            {
                return this._reader.GetString(ordinal);
            }

            public override TextReader GetTextReader(int ordinal)
            {
                return this._reader.GetTextReader(ordinal);
            }

            public override object GetValue(int ordinal)
            {
                return ConvertToUtcOrReturn(this._reader.GetValue(ordinal));
            }

            public override int GetValues(object[] values)
            {
                var result = this._reader.GetValues(values);
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = ConvertToUtcOrReturn(values[i]);
                }
                return result;
            }

            public override object InitializeLifetimeService()
            {
                return this._reader.InitializeLifetimeService();
            }

            public override bool IsDBNull(int ordinal)
            {
                return this._reader.IsDBNull(ordinal);
            }

            public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
            {
                return this._reader.IsDBNullAsync(ordinal, cancellationToken);
            }

            public override bool NextResult()
            {
                return this._reader.NextResult();
            }

            public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
            {
                return this._reader.NextResultAsync(cancellationToken);
            }

            public override bool Read()
            {
                return this._reader.Read();
            }

            public override Task<bool> ReadAsync(CancellationToken cancellationToken)
            {
                return this._reader.ReadAsync(cancellationToken);
            }
        }
    }
}
```
