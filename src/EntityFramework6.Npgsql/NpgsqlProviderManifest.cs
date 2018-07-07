#region License
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
using System.Text;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Data;
using JetBrains.Annotations;
using NpgsqlTypes;

namespace Npgsql
{
    internal class NpgsqlProviderManifest : DbXmlEnabledProviderManifest
    {
        public Version Version { get; }

        public NpgsqlProviderManifest(string serverVersion)
            : base(CreateXmlReaderForResource("Npgsql.Resources.NpgsqlProviderManifest.Manifest.xml"))
        {
            Version = Version.TryParse(serverVersion, out var version)
                ? version
                : new Version(9, 5);
        }

        protected override XmlReader GetDbInformation([NotNull] string informationType)
        {
            if (informationType == StoreSchemaDefinition)
                return CreateXmlReaderForResource("Npgsql.Resources.NpgsqlSchema.ssdl");
            if (informationType == StoreSchemaDefinitionVersion3)
                return CreateXmlReaderForResource("Npgsql.Resources.NpgsqlSchemaV3.ssdl");
            if (informationType == StoreSchemaMapping)
                return CreateXmlReaderForResource("Npgsql.Resources.NpgsqlSchema.msl");

            throw new ArgumentOutOfRangeException(nameof(informationType));
        }

        const string MaxLengthFacet = "MaxLength";
        const string ScaleFacet = "Scale";
        const string PrecisionFacet = "Precision";
        const string FixedLengthFacet = "FixedLength";

        internal static NpgsqlDbType GetNpgsqlDbType(PrimitiveTypeKind primitiveType)
        {
            switch (primitiveType)
            {
            case PrimitiveTypeKind.Binary:
                return NpgsqlDbType.Bytea;
            case PrimitiveTypeKind.Boolean:
                return NpgsqlDbType.Boolean;
            case PrimitiveTypeKind.Byte:
            case PrimitiveTypeKind.SByte:
            case PrimitiveTypeKind.Int16:
                return NpgsqlDbType.Smallint;
            case PrimitiveTypeKind.DateTime:
                return NpgsqlDbType.Timestamp;
            case PrimitiveTypeKind.DateTimeOffset:
                return NpgsqlDbType.TimestampTZ;
            case PrimitiveTypeKind.Decimal:
                return NpgsqlDbType.Numeric;
            case PrimitiveTypeKind.Double:
                return NpgsqlDbType.Double;
            case PrimitiveTypeKind.Int32:
                return NpgsqlDbType.Integer;
            case PrimitiveTypeKind.Int64:
                return NpgsqlDbType.Bigint;
            case PrimitiveTypeKind.Single:
                return NpgsqlDbType.Real;
            case PrimitiveTypeKind.Time:
                return NpgsqlDbType.Interval;
            case PrimitiveTypeKind.Guid:
                return NpgsqlDbType.Uuid;
            case PrimitiveTypeKind.String:
                // Send strings as unknowns to be compatible with other datatypes than text
                return NpgsqlDbType.Unknown;
            default:
                return NpgsqlDbType.Unknown;
            }
        }

        public override TypeUsage GetEdmType([NotNull] TypeUsage storeType)
        {
            if (storeType == null)
                throw new ArgumentNullException(nameof(storeType));

            var storeTypeName = storeType.EdmType.Name;
            var primitiveType = StoreTypeNameToEdmPrimitiveType[storeTypeName];
            // TODO: come up with way to determin if unicode is used
            var isUnicode = true;
            Facet facet;

            switch (storeTypeName)
            {
            case "bool":
            case "int2":
            case "int4":
            case "int8":
            case "float4":
            case "float8":
            case "uuid":
                return TypeUsage.CreateDefaultTypeUsage(primitiveType);
            case "numeric":
                {
                    byte scale;
                    byte precision;
                    if (storeType.Facets.TryGetValue(ScaleFacet, false, out facet) &&
                        !facet.IsUnbounded && facet.Value != null)
                    {
                        scale = (byte)facet.Value;
                        if (storeType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                            !facet.IsUnbounded && facet.Value != null)
                        {
                            precision = (byte)facet.Value;
                            return TypeUsage.CreateDecimalTypeUsage(primitiveType, precision, scale);
                        }
                    }
                    return TypeUsage.CreateDecimalTypeUsage(primitiveType);
                }
            case "bpchar":
                if (storeType.Facets.TryGetValue(MaxLengthFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                    return TypeUsage.CreateStringTypeUsage(primitiveType, isUnicode, true, (int)facet.Value);
                else
                    return TypeUsage.CreateStringTypeUsage(primitiveType, isUnicode, true);
            case "varchar":
                if (storeType.Facets.TryGetValue(MaxLengthFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                    return TypeUsage.CreateStringTypeUsage(primitiveType, isUnicode, false, (int)facet.Value);
                else
                    return TypeUsage.CreateStringTypeUsage(primitiveType, isUnicode, false);
            case "text":
            case "xml":
                return TypeUsage.CreateStringTypeUsage(primitiveType, isUnicode, false);
            case "timestamp":
                // TODO: make sure the arguments are correct here
                if (storeType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                {
                    return TypeUsage.CreateDateTimeTypeUsage(primitiveType, (byte)facet.Value);
                }
                else
                {
                    return TypeUsage.CreateDateTimeTypeUsage(primitiveType, null);
                }
            case "date":
                return TypeUsage.CreateDateTimeTypeUsage(primitiveType, 0);
            case "timestamptz":
                if (storeType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                {
                    return TypeUsage.CreateDateTimeOffsetTypeUsage(primitiveType, (byte)facet.Value);
                }
                else
                {
                    return TypeUsage.CreateDateTimeOffsetTypeUsage(primitiveType, null);
                }
            case "time":
            case "interval":
                if (storeType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                {
                    return TypeUsage.CreateTimeTypeUsage(primitiveType, (byte)facet.Value);
                }
                else
                {
                    return TypeUsage.CreateTimeTypeUsage(primitiveType, null);
                }
            case "bytea":
                {
                    if (storeType.Facets.TryGetValue(MaxLengthFacet, false, out facet) &&
                        !facet.IsUnbounded && facet.Value != null)
                    {
                        return TypeUsage.CreateBinaryTypeUsage(primitiveType, false, (int)facet.Value);
                    }
                    return TypeUsage.CreateBinaryTypeUsage(primitiveType, false);
                }
            case "rowversion":
                {
                    return TypeUsage.CreateBinaryTypeUsage(primitiveType, true, 8);
                }
                //TypeUsage.CreateBinaryTypeUsage
                //TypeUsage.CreateDateTimeTypeUsage
                //TypeUsage.CreateDecimalTypeUsage
                //TypeUsage.CreateStringTypeUsage
            }

            throw new NotSupportedException("Not supported store type: " + storeTypeName);
        }

        public override TypeUsage GetStoreType([NotNull] TypeUsage edmType)
        {
            if (edmType == null)
                throw new ArgumentNullException(nameof(edmType));

            var primitiveType = edmType.EdmType as PrimitiveType;
            if (primitiveType == null)
                throw new ArgumentException("Store does not support specified edm type");

            // TODO: come up with way to determin if unicode is used
            var isUnicode = true;
            Facet facet;

            switch (primitiveType.PrimitiveTypeKind)
            {
            case PrimitiveTypeKind.Boolean:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["bool"]);
            case PrimitiveTypeKind.Int16:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int2"]);
            case PrimitiveTypeKind.Int32:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int4"]);
            case PrimitiveTypeKind.Int64:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int8"]);
            case PrimitiveTypeKind.Single:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["float4"]);
            case PrimitiveTypeKind.Double:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["float8"]);
            case PrimitiveTypeKind.Decimal:
                {
                    byte scale;
                    byte precision;
                    if (edmType.Facets.TryGetValue(ScaleFacet, false, out facet) &&
                        !facet.IsUnbounded && facet.Value != null)
                    {
                        scale = (byte)facet.Value;
                        if (edmType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                            !facet.IsUnbounded && facet.Value != null)
                        {
                            precision = (byte)facet.Value;
                            return TypeUsage.CreateDecimalTypeUsage(StoreTypeNameToStorePrimitiveType["numeric"], precision, scale);
                        }
                    }
                    return TypeUsage.CreateDecimalTypeUsage(StoreTypeNameToStorePrimitiveType["numeric"]);
                }
            case PrimitiveTypeKind.String:
                {
                    // TODO: could get character, character varying, text
                    if (edmType.Facets.TryGetValue(FixedLengthFacet, false, out facet) &&
                        !facet.IsUnbounded && facet.Value != null && (bool)facet.Value)
                    {
                        PrimitiveType characterPrimitive = StoreTypeNameToStorePrimitiveType["bpchar"];
                        if (edmType.Facets.TryGetValue(MaxLengthFacet, false, out facet) &&
                            !facet.IsUnbounded && facet.Value != null)
                        {
                            return TypeUsage.CreateStringTypeUsage(characterPrimitive, isUnicode, true, (int)facet.Value);
                        }
                        // this may not work well
                        return TypeUsage.CreateStringTypeUsage(characterPrimitive, isUnicode, true);
                    }
                    if (edmType.Facets.TryGetValue(MaxLengthFacet, false, out facet) &&
                        !facet.IsUnbounded && facet.Value != null)
                    {
                        return TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["varchar"], isUnicode, false, (int)facet.Value);
                    }
                    // assume text since it is not fixed length and has no max length
                    return TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["text"], isUnicode, false);
                }
            case PrimitiveTypeKind.DateTime:
                if (edmType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                {
                    return TypeUsage.CreateDateTimeTypeUsage(StoreTypeNameToStorePrimitiveType["timestamp"], (byte)facet.Value);
                }
                else
                {
                    return TypeUsage.CreateDateTimeTypeUsage(StoreTypeNameToStorePrimitiveType["timestamp"], null);
                }
            case PrimitiveTypeKind.DateTimeOffset:
                if (edmType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                {
                    return TypeUsage.CreateDateTimeOffsetTypeUsage(StoreTypeNameToStorePrimitiveType["timestamptz"], (byte)facet.Value);
                }
                else
                {
                    return TypeUsage.CreateDateTimeOffsetTypeUsage(StoreTypeNameToStorePrimitiveType["timestamptz"], null);
                }
            case PrimitiveTypeKind.Time:
                if (edmType.Facets.TryGetValue(PrecisionFacet, false, out facet) &&
                    !facet.IsUnbounded && facet.Value != null)
                {
                    return TypeUsage.CreateTimeTypeUsage(StoreTypeNameToStorePrimitiveType["interval"], (byte)facet.Value);
                }
                else
                {
                    return TypeUsage.CreateTimeTypeUsage(StoreTypeNameToStorePrimitiveType["interval"], null);
                }
            case PrimitiveTypeKind.Binary:
                {
                    if (edmType.Facets.TryGetValue(MaxLengthFacet, false, out facet) &&
                        !facet.IsUnbounded && facet.Value != null)
                    {
                        return TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["bytea"], false, (int)facet.Value);
                    }
                    return TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["bytea"], false);
                }
            case PrimitiveTypeKind.Guid:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["uuid"]);
            case PrimitiveTypeKind.Byte:
            case PrimitiveTypeKind.SByte:
                return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int2"]);
            }

            throw new NotSupportedException("Not supported edm type: " + edmType);
        }

        static XmlReader CreateXmlReaderForResource(string resourceName)
        {
            var stream = Assembly.GetAssembly(typeof(NpgsqlProviderManifest)).GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Could not find resource {resourceName} in assembly, please report issue");
            return XmlReader.Create(stream);
        }

        public override bool SupportsEscapingLikeArgument(out char escapeCharacter)
        {
            escapeCharacter = '\\';
            return true;
        }

        public override string EscapeLikeArgument([NotNull] string argument)
            => argument.Replace("\\","\\\\").Replace("%", "\\%").Replace("_", "\\_");

        public override bool SupportsInExpression() => true;

        public override ReadOnlyCollection<EdmFunction> GetStoreFunctions()
            => new[] { typeof(NpgsqlTextFunctions).GetTypeInfo(), typeof(NpgsqlTypeFunctions) }
                .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Select(x => new { Method = x, DbFunction = x.GetCustomAttribute<DbFunctionAttribute>() })
                .Where(x => x.DbFunction != null)
                .Select(x => CreateComposableEdmFunction(x.Method, x.DbFunction))
                .ToList()
                .AsReadOnly();

        static EdmFunction CreateComposableEdmFunction([NotNull] MethodInfo method, [NotNull] DbFunctionAttribute dbFunctionInfo)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            if (dbFunctionInfo == null)
                throw new ArgumentNullException(nameof(dbFunctionInfo));

            return EdmFunction.Create(
                dbFunctionInfo.FunctionName,
                dbFunctionInfo.NamespaceName,
                DataSpace.SSpace,
                new EdmFunctionPayload
                {
                    ParameterTypeSemantics = ParameterTypeSemantics.AllowImplicitConversion,
                    Schema = string.Empty,
                    IsBuiltIn = true,
                    IsAggregate = false,
                    IsFromProviderManifest = true,
                    StoreFunctionName = dbFunctionInfo.FunctionName,
                    IsComposable = true,
                    ReturnParameters = new[]
                    {
                        FunctionParameter.Create(
                            "ReturnType",
                            MapTypeToEdmType(method.ReturnType),
                            ParameterMode.ReturnValue)
                    },
                    Parameters = method.GetParameters().Select(
                        x => FunctionParameter.Create(
                            x.Name,
                            MapTypeToEdmType(x.ParameterType),
                            ParameterMode.In)).ToList()
                },
                new List<MetadataProperty>());
        }

        static EdmType MapTypeToEdmType(Type type)
        {
            var fromClrType = PrimitiveType
                .GetEdmPrimitiveTypes()
                .FirstOrDefault(t => t.ClrEquivalentType == type);

            if (fromClrType != null)
                return fromClrType;

            if (type.IsEnum)
                return MapTypeToEdmType(Enum.GetUnderlyingType(type));

            throw new NotSupportedException($"Unsupported type for mapping to EdmType: {type.FullName}");
        }
    }
}
