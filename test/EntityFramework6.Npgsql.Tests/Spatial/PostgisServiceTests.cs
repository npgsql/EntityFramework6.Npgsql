using System;
using Npgsql;
using Npgsql.Spatial;
using NpgsqlTypes;
using NUnit.Framework;
// ReSharper disable RedundantExplicitArrayCreation

namespace EntityFramework6.Npgsql.Tests.Spatial
{
    public class PostgisServiceTests : TestBase
    {
        [Test]
        public void AsBinaryTest()
        {
            var p = new PostgisPoint(1D, 1D);
            var svcs = CreatePostgisServices();
            svcs.AsBinary(svcs.GeometryFromProviderValue(p));
        }

        [Test]
        public void AsBinaryTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void AsGmlTest()
        {
            var p = new PostgisPoint(1D, 1D);
            var svcs = CreatePostgisServices();
            svcs.AsGml(svcs.GeometryFromProviderValue(p));
        }

        [Test]
        public void AsGmlTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void AsTextTest()
        {
            var p = new PostgisPoint(1D, 1D);
            var svcs = CreatePostgisServices();
            svcs.AsText(svcs.GeometryFromProviderValue(p));
        }

        [Test]
        public void AsTextTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void BufferTest()
        {
            var p = new PostgisPoint(1D, 1D);
            var svcs = CreatePostgisServices();
            svcs.Buffer(svcs.GeometryFromProviderValue(p), 19D);
        }

        [Test]
        public void BufferTest1()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void ContainsTest()
        {
            var p = new PostgisPoint(1D, 1D);
            var svcs = CreatePostgisServices();
            var pol = new PostgisPolygon(new Coordinate2D[1][]
            {new Coordinate2D[5]
                {
                    new Coordinate2D(0D,0D),
                    new Coordinate2D(5D,0D),
                    new Coordinate2D(5D,5D),
                    new Coordinate2D(0D,5D),
                    new Coordinate2D(0D,0D)
                }
            });
            Assert.True(svcs.Contains(svcs.GeometryFromProviderValue(pol), svcs.GeometryFromProviderValue(p)));
        }

        [Test]
        public void CreateProviderValueTest()
        {
            var svcs = CreatePostgisServices();
            svcs.CreateProviderValue(
            svcs.CreateWellKnownValue(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D))));
        }

        [Test]
        public void CreateProviderValueTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void CreateWellKnownValueTest()
        {
            var svcs = CreatePostgisServices();
            svcs.CreateWellKnownValue(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
        }

        [Test]
        public void CreateWellKnownValueTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void CrossesTest()
        {
            var svcs = CreatePostgisServices();
            svcs.Crosses(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)),
                            svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
        }

        [Test]
        public void DifferenceTest()
        {
            var svcs = CreatePostgisServices();
            svcs.Difference(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)),
                            svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
        }

        [Test]
        public void DifferenceTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void DisjointTest()
        {   
            var svcs = CreatePostgisServices();
            svcs.Disjoint(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)),
                            svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
        }

        [Test]
        public void DisjointTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void DistanceTest()
        {
            var svcs = CreatePostgisServices();
            svcs.Distance(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)),
                            svcs.GeometryFromProviderValue(new PostgisPoint(1D, 1D)));
        }

        [Test]
        public void DistanceTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void ElementAtTest()
        {
            var svcs = CreatePostgisServices();
            svcs.ElementAt(svcs.GeometryFromProviderValue(
                new PostgisGeometryCollection(new PostgisGeometry[1] { new PostgisPoint(0D, 0D) })),
                1);
        }

        [Test]
        public void ElementAtTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyCollectionFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyCollectionFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromBinaryTest1()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromGmlTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromGmlTest1()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromProviderValueTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyFromTextTest1()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyLineFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyLineFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyMultiLineFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyMultiLineFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyMultiPointFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyMultiPointFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyMultiPolygonFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyMultiPolygonFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyPointFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyPointFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyPolygonFromBinaryTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeographyPolygonFromTextTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeometryCollectionFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            try
            {
                var b = svcs.AsBinary(svcs.GeometryFromProviderValue(new PostgisGeometryCollection
                    (new PostgisGeometry[] { new PostgisPoint(0D, 0D) })));
                svcs.GeometryCollectionFromBinary(b, 1);
            }
            catch (NotImplementedException)
            {
                Assert.Ignore("not implemented");
            }
        }

        [Test]
        public void GeometryCollectionFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(new PostgisGeometryCollection
                (new PostgisGeometry[] { new PostgisPoint(0D, 0D) })));
            svcs.GeometryCollectionFromText(b, 1);
        }

        [Test]
        public void GeometryFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
            svcs.GeometryFromBinary(b, 1);
        }

        [Test]
        public void GeometryFromBinaryTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeometryFromGmlTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
            svcs.GeometryCollectionFromText(b, 1);
        }

        [Test]
        public void GeometryFromGmlTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeometryFromProviderValueTest()
        {
            var svcs = CreatePostgisServices();
            svcs.GeometryFromProviderValue(new PostgisPoint(0d, 0d));
        }

        [Test]
        public void GeometryFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(new PostgisPoint(0D, 0D)));
            svcs.GeometryCollectionFromText(b, 1);
        }

        [Test]
        public void GeometryFromTextTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GeometryLineFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(
                new PostgisLineString(new Coordinate2D[] { new Coordinate2D(0D, 0D),
                                        new Coordinate2D(0d,0d) })));
            svcs.GeometryLineFromBinary(b, 1);
        }

        [Test]
        public void GeometryLineFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(
                new PostgisLineString(new Coordinate2D[] { new Coordinate2D(0D, 0D),
                                        new Coordinate2D(0d,0d) })));
            svcs.GeometryLineFromText(b, 1);
        }

        [Test]
        public void GeometryMultiLineFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(
                new PostgisMultiLineString(
                    new Coordinate2D[][] {
                        new Coordinate2D[] {
                            new Coordinate2D(0D, 0D),
                            new Coordinate2D(0d,0d)
                            }
                        })));
            svcs.GeometryMultiLineFromBinary(b, 1);
        }

        [Test]
        public void GeometryMultiLineFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(
                new PostgisMultiLineString(
                    new Coordinate2D[][] {
                        new Coordinate2D[] {
                            new Coordinate2D(0D, 0D),
                            new Coordinate2D(0d,0d)
                            }
                        })));
            svcs.GeometryMultiLineFromText(b, 1);
        }

        [Test]
        public void GeometryMultiPointFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(
                new PostgisMultiPoint(
                        new Coordinate2D[] {
                            new Coordinate2D(0D, 0D),
                            new Coordinate2D(0d,0d)
                            }
                        )));
            svcs.GeometryMultiPointFromBinary(b, 1);
        }

        [Test]
        public void GeometryMultiPointFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(
                new PostgisMultiPoint(
                        new Coordinate2D[] {
                            new Coordinate2D(0D, 0D),
                            new Coordinate2D(0d,0d)
                            }
                        )));
            svcs.GeometryMultiPointFromText(b, 1);
        }

        [Test]
        public void GeometryMultiPolygonFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(
                new PostgisMultiPolygon(new PostgisPolygon[]
                {new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }
                        )
                }
                        )));
            svcs.GeometryMultiPolygonFromBinary(b, 1);
        }

        [Test]
        public void GeometryMultiPolygonFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(
                new PostgisMultiPolygon(new PostgisPolygon[]
                {new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }
                        )
                }
                        )));
            svcs.GeometryMultiPolygonFromText(b, 1);
        }

        [Test]
        public void GeometryPointFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(new PostgisPoint(1D, 1D)));
            svcs.GeometryPointFromBinary(b, 1);
        }

        [Test]
        public void GeometryPointFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(new PostgisPoint(1D, 1D)));
            svcs.GeometryPointFromText(b, 1);
        }

        [Test]
        public void GeometryPolygonFromBinaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsBinary(svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            })));
            svcs.GeometryPolygonFromBinary(b, 1);
        }

        [Test]
        public void GeometryPolygonFromTextTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.AsText(svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            })));
            svcs.GeometryPolygonFromText(b, 1);
        }

        [Test]
        public void GetAreaTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetArea(b);
        }

        [Test]
        public void GetAreaTestGeom()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetBoundaryTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetBoundary(b);
        }

        [Test]
        public void GetCentroidTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetCentroid(b);
        }

        [Test]
        public void GetConvexHullTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetConvexHull(b);
        }

        [Test]
        public void GetCoordinateSystemIdTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            })
                { SRID = 3742 });
            svcs.GetCoordinateSystemId(b);
        }

        [Test]
        public void GetCoordinateSystemIdTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetDimensionTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetDimension(b);
        }

        [Test]
        public void GetDimensionTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetElementCountTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisGeometryCollection(
                    new PostgisGeometry[] {
                        new PostgisPolygon(
                            new Coordinate2D[][] {
                                new Coordinate2D[] {
                                    new Coordinate2D(0D, 0D),
                                    new Coordinate2D(0d,1d),
                                    new Coordinate2D(1d,1d),
                                    new Coordinate2D(1d,0d),
                                    new Coordinate2D(0d,0d)
                                    }
                            })
                        })
                    );
            svcs.GetElementCount(b);
        }

        [Test]
        public void GetElementCountTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetElevationTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetElevationTest1()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetEndPointTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetEndPoint(b);
        }

        [Test]
        public void GetEndPointTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetEnvelopeTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetEnvelope(b);
        }

        [Test]
        public void GetExteriorRingTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetExteriorRing(b);
        }

        [Test]
        public void GetInteriorRingCountTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetInteriorRingCount(b);
        }

        [Test]
        public void GetIsClosedTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetIsClosed(b);
        }

        [Test]
        public void GetIsClosedTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetIsEmptyTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetIsEmpty(b);
        }

        [Test]
        public void GetIsEmptyTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetIsRingTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisLineString(
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }));
            svcs.GetIsRing(b);
        }

        [Test]
        public void GetIsSimpleTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetIsSimple(b);
        }

        [Test]
        public void GetIsValidTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetIsSimple(b);
        }

        [Test]
        public void GetLatitudeTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetLengthTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetLength(b);
        }

        [Test]
        public void GetLengthTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetLongitudeTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetMeasureTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetMeasureTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetPointCountTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetPointCount(b);
        }

        [Test]
        public void GetPointCountTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetPointOnSurfaceTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetPointOnSurface(b);
        }

        [Test]
        public void GetSpatialTypeNameTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            Assert.IsTrue(svcs.GetSpatialTypeName(b).ToLower() == "polygon");
        }

        [Test]
        public void GetSpatialTypeNameTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetStartPointTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.GetStartPoint(b);
        }

        [Test]
        public void GetStartPointTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void GetXCoordinateTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPoint(0D, 0D));
            svcs.GetXCoordinate(b);
        }

        [Test]
        public void GetYCoordinateTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPoint(0D, 0D));
            svcs.GetYCoordinate(b);
        }

        [Test]
        public void InteriorRingAtTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            ,new Coordinate2D[] {
                                new Coordinate2D(0.5D, 0.5D),
                                new Coordinate2D(0.5d,0.8d),
                                new Coordinate2D(0.8d,0.8d),
                                new Coordinate2D(0.8d,0.5d),
                                new Coordinate2D(0.5d,0.5d)
                                }
                            }));
            svcs.InteriorRingAt(b, 2);
        }

        [Test]
        public void IntersectionTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Intersection(b, b);
        }

        [Test]
        public void IntersectionTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void IntersectsTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Intersection(b, b);
        }

        [Test]
        public void IntersectsTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void OverlapsTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Overlaps(b, b);
        }

        [Test]
        public void PointAtTest()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void PointAtTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void RelateTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Relate(b, b, "0FFFFF212");
        }

        [Test]
        public void SpatialEqualsTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.SpatialEquals(b, b);
        }

        [Test]
        public void SpatialEqualsTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void SymmetricDifferenceTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.SymmetricDifference(b, b);
        }

        [Test]
        public void SymmetricDifferenceTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void TouchesTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Touches(b, b);
        }

        [Test]
        public void UnionTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Union(b, b);
        }

        [Test]
        public void UnionTestGeog()
        {
            Assert.Ignore("not implemented");
        }

        [Test]
        public void WithinTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                new PostgisPolygon(
                        new Coordinate2D[][] {
                            new Coordinate2D[] {
                                new Coordinate2D(0D, 0D),
                                new Coordinate2D(0d,1d),
                                new Coordinate2D(1d,1d),
                                new Coordinate2D(1d,0d),
                                new Coordinate2D(0d,0d)
                                }
                            }));
            svcs.Within(b, b);
        }

        [Test]
        public void InstanceTest()
        {
            var svcs = CreatePostgisServices();
            var b = svcs.GeometryFromProviderValue(
                    new PostgisPolygon(
                            new Coordinate2D[][] {
                                new Coordinate2D[] {
                                    new Coordinate2D(0D, 0D),
                                    new Coordinate2D(0d,1d),
                                    new Coordinate2D(1d,1d),
                                    new Coordinate2D(1d,0d),
                                    new Coordinate2D(0d,0d)
                                    }
                                }));
            var x = b.Area;
        }

        #region Support

        // ReSharper disable once InconsistentNaming
        PostgisServices CreatePostgisServices()
        {
            var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            var svcs = new PostgisServices();
            svcs.SetConnection(conn);
            return svcs;
        }

        [OneTimeSetUp]
        public new void TestFixtureSetup()
        {
            using (var context = new BloggingContext(ConnectionString))
            {
                if (!context.Database.Exists())
                    context.Database.Create();
            }

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS postgis", conn))
                    cmd.ExecuteNonQuery();

                // Need to also have Npgsql reload the types from the database
                conn.ReloadTypes();
                NpgsqlConnection.ClearPool(conn);
            }
        }

        #endregion Support
    }
}
