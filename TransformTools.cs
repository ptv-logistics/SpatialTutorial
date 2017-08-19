using System;
using System.Windows;

namespace SpatialTutorial
{
    /// <summary>
    /// A tools class that does the arithmetics for tiled maps
    /// </summary>
    public static class TransformTools
    {
        /// <summary>
        /// Convert a WGS84 coordinate (Lon/Lat) to generic spherical mercator.
        /// When using tiles with wgs, the actual earth radius doesn't matter, we can just use radius 1.
        /// To use this formula with "Google Mercator", you have to multiply the output coordinates by 6378137.
        /// For "PTV Mercator" use 6371000
        /// </summary>
        public static Point WgsToSphereMercator(Point point)
        {
            double x = point.X * Math.PI / 180.0;
            double y = Math.Log(Math.Tan(Math.PI / 4.0 + point.Y * Math.PI / 360.0));

            return new Point(x, y);
        }

        /// <summary>
        /// The reverse of the function above
        /// To use this formula with "Google Mercator", you have to divide the input coordinates by 6378137.
        /// For "PTV Mercator" use 6371000
        /// </summary>
        public static Point SphereMercatorToWgs(Point point)
        {
            double x = (180 / Math.PI) * point.X;
            double y = (360 / Math.PI) * (Math.Atan(Math.Exp(point.Y)) - (Math.PI / 4));

            return new Point(x, y);
        }

        /// <summary>
        /// Calculate the Mercator bounds for a tile key
        /// </summary>
        public static Rect TileToSphereMercator(uint x, uint y, uint z)
        {
            // the width of a tile (when the earth has radius 1)
            double arc = Math.PI * 2.0 / Math.Pow(2, z);

            double x1 = -Math.PI + x * arc;
            double x2 = x1 + arc;

            double y1 = Math.PI - y * arc;
            double y2 = y1 - arc;

            return new Rect(new Point(x1, y2), new Point(x2, y1));
        }

        /// <summary>
        /// Calculate WGS (Lon/Lat) bounds for a tile key
        /// </summary>
        public static Rect TileToWgs(uint x, uint y, uint z, int bleedingPixels = 0)
        {
            var rect = TileToSphereMercator(x, y, z);

            if(bleedingPixels != 0)
            { 
                double bleedingFactor = bleedingPixels / 256.0 * 2;

                rect.Inflate(rect.Width * bleedingFactor, rect.Height * bleedingFactor);
            }

            return new Rect(SphereMercatorToWgs(rect.TopLeft), SphereMercatorToWgs(rect.BottomRight));
        }

        /// <summary>
        /// Convert a point relative to a mercator viewport to a point relative to an image
        /// </summary>
        public static System.Drawing.Point MercatorToImage(Rect mercatorRect, Size imageSize, Point mercatorPoint)
        {
            return new System.Drawing.Point(
              (int)((mercatorPoint.X - mercatorRect.Left) / (mercatorRect.Right - mercatorRect.Left) * imageSize.Width),
              (int)(imageSize.Height - (mercatorPoint.Y - mercatorRect.Top) / (mercatorRect.Bottom - mercatorRect.Top) * imageSize.Height));
        }

        /// <summary>
        /// Convert a WGS (Lon,Lat) coordinate to a point relative to a tile image
        /// </summary>
        public static System.Drawing.Point WgsToTile(uint x, uint y, uint z, Point wgsPoint, double clipWgsAtDegrees = 85.05)
        {
            if (clipWgsAtDegrees < 90)
                wgsPoint = ClipWgsPoint(wgsPoint, clipWgsAtDegrees);

            return MercatorToImage(TileToSphereMercator(x, y, z), new Size(256, 256), WgsToSphereMercator(wgsPoint));
        }

        /// <summary>
        /// Clip the latitude value to avoid overflow at the poles
        /// </summary>
        public static Point ClipWgsPoint(Point p, double degrees = 85.05)
        {
            if (p.Y > degrees)
                p.Y = degrees;
            if (p.Y < -degrees)
                p.Y = -degrees;

            return p;
        }
    }
}
