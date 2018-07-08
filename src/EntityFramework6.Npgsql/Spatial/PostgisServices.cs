using System;
using System.Data.Entity.Spatial;
using NpgsqlTypes;

namespace Npgsql.Spatial
{
    /// <summary>
    /// A class exposing spatial services.
    /// </summary>
    public class PostgisServices : DbSpatialServices
    {
        /// <summary>
        /// Returns the well known binary value of the geometry input.
        /// </summary>
        public override byte[] AsBinary(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_AsBinary(:p1)";
                return (byte[])cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Returns the well known binary value of the geography input.
        /// </summary>
        public override byte[] AsBinary(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the geographical markup language representation of the geometry input.
        /// </summary>
        public override string AsGml(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_AsGml(:p1)";
                return (string)cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Returns the geographical markup language representation of the geography input.
        /// </summary>
        public override string AsGml(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the well known text representation of the geometry input.
        /// </summary>
        public override string AsText(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_AsText(:p1)";
                return (string)cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Returns the well known text representation of the geography input.
        /// </summary>
        public override string AsText(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns a geometry that represents all points whose distance from this Geometry is less than or equal to distance.
        /// </summary>
        public override DbGeometry Buffer(DbGeometry geometryValue, double distance)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Double, distance);
                cmd.CommandText = "SELECT ST_Buffer(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns a geometry that represents all points whose distance from this Geometry is less than or equal to distance.
        /// Calculations are in the Spatial Reference System of this Geometry. Uses a planar transform wrapper.
        /// </summary>
        public override DbGeography Buffer(DbGeography geographyValue, double distance)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns true if and only if no points of B lie in the exterior of A, and at least one point of the interior of B lies in the interior of A
        /// </summary>
        public override bool Contains(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Contains(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <inheritdoc />
        public override object CreateProviderValue(DbGeometryWellKnownValue wellKnownValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, wellKnownValue.WellKnownBinary);
                cmd.CommandText = "SELECT ST_GeomFromWkb(:p1)";
                return cmd.ExecuteScalar();
            }
        }

        /// <inheritdoc />
        public override object CreateProviderValue(DbGeographyWellKnownValue wellKnownValue)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeometryWellKnownValue CreateWellKnownValue(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_AsText(:p1)";
                var d = new DbGeometryWellKnownValue();
                d.WellKnownText = (string)cmd.ExecuteScalar();
                cmd.CommandText = "SELECT ST_AsBinary(:p1)";
                d.WellKnownBinary = (byte[])cmd.ExecuteScalar();
                cmd.CommandText = "SELECT ST_SRID(:p1)";
                d.CoordinateSystemId = (int)cmd.ExecuteScalar();
                return d;
            }
        }

        /// <inheritdoc />
        public override DbGeographyWellKnownValue CreateWellKnownValue(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        ///  Returns TRUE if the supplied geometries have some, but not all, interior points in commo
        /// </summary>
        public override bool Crosses(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Crosses(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Returns a geometry that represents that part of geometry A that does not intersect with geometry B.
        /// </summary>
        public override DbGeometry Difference(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Difference(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns a geometry that represents that part of geometry A that does not intersect with geometry B.
        /// </summary>
        public override DbGeography Difference(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        ///  Returns TRUE if the Geometries do not "spatially intersect" - if they do not share any space together.
        /// </summary>
        public override bool Disjoint(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Disjoint(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        ///  Returns TRUE if the Geometries do not "spatially intersect" - if they do not share any space together.
        /// </summary>
        public override bool Disjoint(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        ///  Returns the 2-dimensional cartesian minimum distance (based on spatial ref) between two geometries in projected units.
        /// </summary>
        public override double Distance(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Distance(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetDouble(0);
                }
            }
        }

        /// <summary>
        ///  Returns the 2-dimensional cartesian minimum distance (based on spatial ref) between two geometries in projected units.
        /// </summary>
        public override double Distance(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        /// Given a geometry collection, returns the index-nth geometry.
        /// </summary>
        public override DbGeometry ElementAt(DbGeometry geometryValue, int index)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, index);
                cmd.CommandText = "SELECT ST_GeometryN(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        ///  Given a geography collection, returns the index-nth geography.
        /// </summary>
        public override DbGeography ElementAt(DbGeography geographyValue, int index)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyCollectionFromBinary(byte[] geographyCollectionWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyCollectionFromText(string geographyCollectionWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromBinary(byte[] wellKnownBinary)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromBinary(byte[] wellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromGml(string geographyMarkup)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromGml(string geographyMarkup, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromProviderValue(object providerValue)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromText(string wellKnownText)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyFromText(string wellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyLineFromBinary(byte[] lineWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyLineFromText(string lineWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyMultiLineFromBinary(byte[] multiLineWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyMultiLineFromText(string multiLineWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyMultiPointFromBinary(byte[] multiPointWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyMultiPointFromText(string multiPointWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyMultiPolygonFromBinary(byte[] multiPolygonWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyMultiPolygonFromText(string multiPolygonWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyPointFromBinary(byte[] pointWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyPointFromText(string pointWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyPolygonFromBinary(byte[] polygonWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography GeographyPolygonFromText(string polygonWellKnownText, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <summary>
        /// Get the geometry collection from a well know binary representation.
        /// </summary>
        public override DbGeometry GeometryCollectionFromBinary(byte[] geometryCollectionWellKnownBinary, int coordinateSystemId)
            => throw new NotImplementedException();

        /// <summary>
        /// Get the geometry collection from a well know binary representation.
        /// </summary>
        public override DbGeometry GeometryCollectionFromText(string geometryCollectionWellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, geometryCollectionWellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_GeomCollFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the geometry from its well known binary representation
        /// </summary>
        public override DbGeometry GeometryFromBinary(byte[] wellKnownBinary)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, wellKnownBinary);
                cmd.CommandText = "SELECT ST_GeomFromWKB(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the geometry from its well known binary representation
        /// </summary>
        public override DbGeometry GeometryFromBinary(byte[] wellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, wellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_GeomFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the geometry from a geometic markup language representation.
        /// </summary>
        public override DbGeometry GeometryFromGml(string geometryMarkup)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, geometryMarkup);
                cmd.CommandText = "SELECT ST_GeomFromGML(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the geometry from a geometic markup language representation.
        /// </summary>
        public override DbGeometry GeometryFromGml(string geometryMarkup, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, geometryMarkup);
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_GeomFromGML(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Wrap a npgsql geometry in a DbGeometry structure.
        /// </summary>
        public override DbGeometry GeometryFromProviderValue(object providerValue)
            => CreateGeometry(this, providerValue);

        /// <summary>
        /// Get the geometry from a well known text value.
        /// </summary>
        public override DbGeometry GeometryFromText(string wellKnownText)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, wellKnownText);
                cmd.CommandText = "SELECT ST_GeomFromText(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the geometry from a well known text value.
        /// </summary>
        public override DbGeometry GeometryFromText(string wellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, wellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_GeomFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a line from its well known binary value.
        /// </summary>
        public override DbGeometry GeometryLineFromBinary(byte[] lineWellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, lineWellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_LineFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a line from its well known text value.
        /// </summary>
        public override DbGeometry GeometryLineFromText(string lineWellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, lineWellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_LineFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a multiline from its well known binary value.
        /// </summary>
        public override DbGeometry GeometryMultiLineFromBinary(byte[] multiLineWellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, multiLineWellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_MLineFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a multiline from a well known text value.
        /// </summary>
        public override DbGeometry GeometryMultiLineFromText(string multiLineWellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, multiLineWellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_MLineFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a multipoint from its well known binaryrepresentation.
        /// </summary>
        public override DbGeometry GeometryMultiPointFromBinary(byte[] multiPointWellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, multiPointWellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_MPointFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a multipoint from its well known text representation.
        /// </summary>
        public override DbGeometry GeometryMultiPointFromText(string multiPointWellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, multiPointWellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_MPointFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a multipolygon from its well known binary value.
        /// </summary>
        public override DbGeometry GeometryMultiPolygonFromBinary(byte[] multiPolygonWellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, multiPolygonWellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_MPolyFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a multipolygon from its well known text value.
        /// </summary>
        public override DbGeometry GeometryMultiPolygonFromText(string multiPolygonKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, multiPolygonKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_MPolyFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a point from its well known binary value.
        /// </summary>
        public override DbGeometry GeometryPointFromBinary(byte[] pointWellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, pointWellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT st_GeomFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a point from its well known text value.
        /// </summary>
        /// <param name="pointWellKnownText"></param>
        /// <param name="coordinateSystemId"></param>
        /// <returns></returns>
        public override DbGeometry GeometryPointFromText(string pointWellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, pointWellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_PointFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a polygon from its well known binary value.
        /// </summary>
        public override DbGeometry GeometryPolygonFromBinary(byte[] polygonWellKnownBinary, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Bytea, polygonWellKnownBinary);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_GeomFromWKB(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get a polygon from its well known text value.
        /// </summary>
        public override DbGeometry GeometryPolygonFromText(string polygonWellKnownText, int coordinateSystemId)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Text, polygonWellKnownText);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, coordinateSystemId);
                cmd.CommandText = "SELECT ST_GeometryFromText(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns the area of the surface if it is a polygon or multi-polygon.
        /// </summary>
        public override double? GetArea(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Area(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.IsDBNull(0) ? new double?() : rdr.GetDouble(0);
                }
            }
        }

        /// <summary>
        ///  Returns the area of the surface if it is a polygon or multi-polygon.
        /// </summary>
        public override double? GetArea(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        ///  Returns the closure of the combinatorial boundary of the geometry.
        /// </summary>
        public override DbGeometry GetBoundary(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Boundary(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns the centroid of the geometry.
        /// </summary>
        public override DbGeometry GetCentroid(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Centroid(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the convex hull of the geometry.
        /// </summary>
        public override DbGeometry GetConvexHull(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_ConvexHull(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the SRID of the geometry.
        /// </summary>
        public override int GetCoordinateSystemId(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_SRID(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetInt32(0);
                }
            }
        }

        /// <summary>
        /// Get the SRID of the geography.
        /// </summary>
        public override int GetCoordinateSystemId(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Get the geometry dimension.
        /// </summary>
        public override int GetDimension(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Dimension(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetInt32(0);
                }
            }
        }

        /// <summary>
        /// Get the geograpy dimension.
        /// </summary>
        public override int GetDimension(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Get the element count of the geometry collection.
        /// </summary>
        public override int? GetElementCount(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_NumGeometries(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.IsDBNull(0) ? new int() : rdr.GetInt32(0);
                }
            }
        }

        /// <summary>
        /// Get the element count of the geometry collection.
        /// </summary>
        public override int? GetElementCount(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the elevation of the geometry
        /// </summary>
        public override double? GetElevation(DbGeometry geometryValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the elevation of the geography.
        /// </summary>
        public override double? GetElevation(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Get the endpoint of the geometry.
        /// </summary>
        public override DbGeometry GetEndPoint(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_EndPoint(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the endpoint of the geography.
        /// </summary>
        public override DbGeography GetEndPoint(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Get the envelope of the geometry.
        /// </summary>
        public override DbGeometry GetEnvelope(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Envelope(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the exterior ring of the geometry.
        /// </summary>
        public override DbGeometry GetExteriorRing(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_ExteriorRing(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Get the ring count of the geometry.
        /// </summary>
        public override int? GetInteriorRingCount(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_NumInteriorRing(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetInt32(0);
                }
            }
        }

        /// <summary>
        /// Check if the geometry is closed.
        /// </summary>
        public override bool? GetIsClosed(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_IsClosed(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Check if the geography is closed;
        /// </summary>
        public override bool? GetIsClosed(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Chekc if the geometry is empty.
        /// </summary>
        public override bool GetIsEmpty(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_IsEmpty(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Check if the geography is empty.
        /// </summary>
        public override bool GetIsEmpty(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Check if the geometry is a linestring, simple and closed.
        /// </summary>
        public override bool? GetIsRing(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_IsRing(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Check if the geometry is simple.
        /// </summary>
        public override bool GetIsSimple(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_IsSimple(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Check if the geometry is valid.
        /// </summary>
        public override bool GetIsValid(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_IsValid(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Returns the latitude of the geography.
        /// </summary>
        public override double? GetLatitude(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the length of the geometry.
        /// </summary>
        public override double? GetLength(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Length(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.IsDBNull(0) ? new double?() : rdr.GetDouble(0);
                }
            }
        }

        /// <summary>
        /// Returns the length of the geography.
        /// </summary>
        public override double? GetLength(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the longitutde of the geography.
        /// </summary>
        public override double? GetLongitude(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override double? GetMeasure(DbGeometry geometryValue)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override double? GetMeasure(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the point count of the geometry.
        /// </summary>
        public override int? GetPointCount(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_NPoints(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.IsDBNull(0) ? new int?() : rdr.GetInt32(0);
                }
            }
        }

        /// <summary>
        /// Returns the point count of the geography.
        /// </summary>
        public override int? GetPointCount(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns a POINT guaranteed to lie on the geometry surface.
        /// </summary>
        public override DbGeometry GetPointOnSurface(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_PointOnSurface(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// returns the spatial type of the geometry.
        /// </summary>
        public override string GetSpatialTypeName(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT GeometryType(:p1)";
                return (string)cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Returns the spatial type of the geography.
        /// </summary>
        public override string GetSpatialTypeName(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the start point of the geometry.
        /// </summary>
        public override DbGeometry GetStartPoint(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_StartPoint(:p1)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns the start point of the geography.
        /// </summary>
        public override DbGeography GetStartPoint(DbGeography geographyValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns a point X coordinate.
        /// </summary>
        public override double? GetXCoordinate(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_X(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.IsDBNull(0) ? new double?() : rdr.GetDouble(0);
                }
            }
        }

        /// <summary>
        /// Returns a point Y coordinate.
        /// </summary>
        public override double? GetYCoordinate(DbGeometry geometryValue)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.CommandText = "SELECT ST_Y(:p1)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.IsDBNull(0) ? new double?() : rdr.GetDouble(0);
                }
            }
        }

        /// <summary>
        /// Returns the index-nth interior ring of the geometry
        /// </summary>
        public override DbGeometry InteriorRingAt(DbGeometry geometryValue, int index)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Integer, index);
                cmd.CommandText = "SELECT ST_InteriorRingN(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        ///Returns the intersection of two geometries.
        /// </summary>
        public override DbGeometry Intersection(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Intersection(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns the intersection of two geographies.
        /// </summary>
        public override DbGeography Intersection(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns TRUE if the Geometries/Geography "spatially intersect in 2D" - (share any portion of space) and FALSE if they don't (they are Disjoint).
        /// </summary>
        public override bool Intersects(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry);
                cmd.CommandText = "SELECT ST_Intersects(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Returns TRUE if the Geometries/Geography "spatially intersect in 2D" - (share any portion of space) and FALSE if they don't (they are Disjoint).
        ///  For geography -- tolerance is 0.00001 meters (so any points that close are considered to intersect)
        /// </summary>
        public override bool Intersects(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns TRUE if the Geometries share space, are of the same dimension, but are not completely contained by each other.
        /// </summary>
        public override bool Overlaps(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Overlaps(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <inheritdoc />
        public override DbGeometry PointAt(DbGeometry geometryValue, int index)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public override DbGeography PointAt(DbGeography geographyValue, int index)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns true if this Geometry is spatially related to anotherGeometry,
        /// by testing for intersections between the Interior, Boundary and Exterior of the two geometries
        /// </summary>
        public override bool Relate(DbGeometry geometryValue, DbGeometry otherGeometry, string matrix)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.Parameters.AddWithValue("p3", NpgsqlDbType.Text, matrix);
                cmd.CommandText = "SELECT ST_Relate(:p1,:p2,:p3)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        ///  Returns true if the given geometries represent the same geometry. Directionality is ignored.
        /// </summary>
        public override bool SpatialEquals(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Equals(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        ///  Returns true if the given geometries represent the same geometry. Directionality is ignored.
        /// </summary>
        public override bool SpatialEquals(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns a geometry that represents the portions of A and B that do not intersect.
        /// It is called a symmetric difference because ST_SymDifference(A,B) = ST_SymDifference(B,A).
        /// </summary>
        public override DbGeometry SymmetricDifference(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_SymDifference(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        ///  Returns a geometry that represents the portions of A and B that do not intersect.
        /// It is called a symmetric difference because ST_SymDifference(A,B) = ST_SymDifference(B,A).
        /// </summary>
        public override DbGeography SymmetricDifference(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns TRUE if the geometries have at least one point in common, but their interiors do not intersect.
        /// </summary>
        public override bool Touches(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Touches(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        /// <summary>
        /// Returns a geometry that represents the point set union of the Geometries.
        /// </summary>
        public override DbGeometry Union(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Union(:p1,:p2)";
                return CreateGeometry(this, cmd.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns a geometry that represents the point set union of the Geometries.
        /// </summary>
        public override DbGeography Union(DbGeography geographyValue, DbGeography otherGeography)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns true if the geometry A is completely inside geometry B
        /// </summary>
        public override bool Within(DbGeometry geometryValue, DbGeometry otherGeometry)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.Parameters.AddWithValue("p1", NpgsqlDbType.Geometry, geometryValue.ProviderValue);
                cmd.Parameters.AddWithValue("p2", NpgsqlDbType.Geometry, otherGeometry.ProviderValue);
                cmd.CommandText = "SELECT ST_Within(:p1,:p2)";
                using (var rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    return rdr.GetBoolean(0);
                }
            }
        }

        NpgsqlConnection _connection;

        /// <summary>
        /// Set the provider connection
        /// </summary>
        /// <param name="c"></param>
        public void SetConnection(NpgsqlConnection c)
        {
            _connection = c;
        }
    }
}
