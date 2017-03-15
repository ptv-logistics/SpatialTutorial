using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SpatialTutorial
{
    public enum WkbByteOrder : byte
    {
        Xdr = 0,
        Ndr = 1
    }

    public enum WKBGeometryType : uint
    {
        Point = 1,
        LineString = 2,
        Polygon = 3,
        MultiPoint = 4,
        MultiLineString = 5,
        MultiPolygon = 6,
        GeometryCollection = 7
    }

    /// <summary> Converts Well-known Binary representations to a GraphicsPath instance. </summary>
    public class WkbToGdi
    {
        #region public methods
        public static GraphicsPath Parse(byte[] bytes, Func<System.Windows.Point, System.Drawing.Point> geoToPixel)
        {
            // Create a memory stream using the suppiled byte array.
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                // Create a new binary reader using the newly created memorystream.
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    // Call the main create function.
                    return Parse(reader, geoToPixel);
                }
            }
        }

        public static GraphicsPath Parse(BinaryReader reader, Func<System.Windows.Point, System.Drawing.Point> geoToPixel)
        {
            // Get the first byte in the array.  This specifies if the WKB is in
            // XDR (big-endian) format of NDR (little-endian) format.
            byte byteOrder = reader.ReadByte();

            if (!Enum.IsDefined(typeof(WkbByteOrder), byteOrder))
            {
                throw new ArgumentException("Byte order not recognized");
            }

            // Get the type of this geometry.
            uint type = (uint)ReadUInt32(reader, (WkbByteOrder)byteOrder);

            if (!Enum.IsDefined(typeof(WKBGeometryType), type))
                throw new ArgumentException("Geometry type not recognized");

            switch ((WKBGeometryType)type)
            {
                case WKBGeometryType.Polygon:
                    return CreateWKBPolygon(reader, (WkbByteOrder)byteOrder, geoToPixel);

                case WKBGeometryType.MultiPolygon:
                    return CreateWKBMultiPolygon(reader, (WkbByteOrder)byteOrder, geoToPixel);

                default:
                    throw new NotSupportedException("Geometry type '" + type.ToString() + "' not supported");
            }
        }
        #endregion

        #region private methods
        private static Point CreateWKBPoint(BinaryReader reader, WkbByteOrder byteOrder)
        {
            // Create and return the point.
            return new Point((int)ReadDouble(reader, byteOrder), (int)ReadDouble(reader, byteOrder));
        }

        private static List<Point> ReadCoordinates(BinaryReader reader, WkbByteOrder byteOrder, Func<System.Windows.Point, System.Drawing.Point> geoToPixel)
        {
            // Get the number of points in this linestring.
            int numPoints = (int)ReadUInt32(reader, byteOrder);

            // Create a new array of coordinates.
            var coords = new List<Point>();

            Point p0 = new Point(0, 0 );
            // Loop on the number of points in the ring.
            for (int i = 0; i < numPoints; i++)
            {
                double x = ReadDouble(reader, byteOrder);
                double y = ReadDouble(reader, byteOrder);

                var dx = geoToPixel(new System.Windows.Point(x, y));

                if(i == 0)
                {
                    coords.Add(new Point(dx.X, dx.Y));

                    p0 = dx;
                }
                else if (i == numPoints-1)
                {
                    if (Math.Abs(coords[0].X - dx.X) >= 1 || Math.Abs(coords[0].Y - dx.Y) >= 1)
                        coords.Add(new Point(dx.X, dx.Y));
                }
                else
                {
                    if (Math.Abs(p0.X - dx.X) >= 1 || Math.Abs(p0.Y - dx.Y) >= 1)
                    {
                        // Add the coordinate.
                        coords.Add(new Point(dx.X, dx.Y));

                        p0 = dx;
                    }
                }
            }

            return coords;
        }

        private static List<Point> CreateWKBLinearRing(BinaryReader reader, WkbByteOrder byteOrder, Func<System.Windows.Point, System.Drawing.Point> geoToPixel)
        {
            return ReadCoordinates(reader, byteOrder, geoToPixel);
        }

        private static GraphicsPath CreateWKBPolygon(BinaryReader reader, WkbByteOrder byteOrder, Func<System.Windows.Point, System.Drawing.Point> geoToPixel)
        {
            // Get the Number of rings in this Polygon.
            int numRings = (int)ReadUInt32(reader, byteOrder);

            Debug.Assert(numRings >= 1, "Number of rings in polygon must be 1 or more.");

            GraphicsPath gp = new GraphicsPath();

            var arr = CreateWKBLinearRing(reader, byteOrder, geoToPixel);

            if (arr.Count > 2)
                gp.AddPolygon(arr.ToArray());

            // Create a new array of linearrings for the interior rings.
            for (int i = 0; i < (numRings - 1); i++)
            {
                var rarr = CreateWKBLinearRing(reader, byteOrder, geoToPixel);

                if (arr.Count > 2 && rarr.Count > 2)
                {
                    gp.AddPolygon(rarr.ToArray());
                }
            }

            // Create and return the Poylgon.
            if (arr.Count > 2)
                return gp;
            else
            {
                int dummy = 42;
                return null;
            }
        }

        private static GraphicsPath CreateWKBMultiPolygon(BinaryReader reader, WkbByteOrder byteOrder, Func<System.Windows.Point, System.Drawing.Point> geoToPixel)
        {
            GraphicsPath gp = new GraphicsPath();

            // Get the number of Polygons.
            int numPolygons = (int)ReadUInt32(reader, byteOrder);

            // Loop on the number of polygons.
            for (int i = 0; i < numPolygons; i++)
            {
                // read polygon header
                reader.ReadByte();
                ReadUInt32(reader, byteOrder);
                var p = CreateWKBPolygon(reader, byteOrder, geoToPixel);

                // TODO: Validate type

                // Create the next polygon and add it to the array.
                if(p != null)
                    gp.AddPath(p, false);
            }

            //Create and return the MultiPolygon.
            if (gp.PointCount > 0)
                return gp;
            else
                return null;
        }

        private static uint ReadUInt32(BinaryReader reader, WkbByteOrder byteOrder)
        {
            if (byteOrder == WkbByteOrder.Xdr)
            {
                byte[] bytes = BitConverter.GetBytes(reader.ReadUInt32());
                Array.Reverse(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }
            else
                return reader.ReadUInt32();
        }

        private static double ReadDouble(BinaryReader reader, WkbByteOrder byteOrder)
        {
            if (byteOrder == WkbByteOrder.Xdr)
            {
                byte[] bytes = BitConverter.GetBytes(reader.ReadDouble());
                Array.Reverse(bytes);
                return BitConverter.ToDouble(bytes, 0);
            }
            else
                return reader.ReadDouble();
        }
        #endregion
    }
}