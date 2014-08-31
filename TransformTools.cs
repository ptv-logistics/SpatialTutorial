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
        /// Convert Wgs84 coordinates (Lon/Lat) to spherical mercator,
        /// aka "Web Mercator", aka "Google Mercator". You can also use this formula
        /// for "PTV Mercator" by setting the radius to 6371000
        /// </summary>
        public static Point WgsToSphereMercator(Point point, double radius = 6378137)
        {
            double x = radius * point.X * Math.PI / 180.0;
            double y = radius * Math.Log(Math.Tan(Math.PI / 4.0 + point.Y * Math.PI / 360.0));

            return new Point(x, y);
        }

        /// <summary>
        /// The reverse of the function above
        /// </summary>
        public static Point SphereMercatorToWgs(Point point, double radius = 6378137)
        {
            double x = (180 / Math.PI) * (point.X / radius);
            double y = (360 / Math.PI) * (Math.Atan(Math.Exp(point.Y / radius)) - (Math.PI / 4));

            return new Point(x, y);
        }

        /// <summary>
        /// Calculate the Mercator bounds for a tile key
        /// </summary>
        public static Rect TileToSphereMercator(uint x, uint y, uint z, double radius = 6378137)
        {
            double earthHalfCircum = radius * Math.PI;
            double earthCircum = earthHalfCircum * 2.0;

            double arc = earthCircum / Math.Pow(2, z);
            double x1 = earthHalfCircum - x * arc;
            double y1 = earthHalfCircum - y * arc;
            double x2 = earthHalfCircum - (x + 1) * arc;
            double y2 = earthHalfCircum - (y + 1) * arc;

            return new Rect(new Point(-x1, y2), new Point(-x2, y1));
        }

        /// <summary>
        /// Calculate WGS (Lon/Lat) bounds for a tile key
        /// </summary>
        public static Rect TileToWgs(uint x, uint y, uint z, int bleedingPixels = 0)
        {
            // when using tiles with wgs, the actual earth radius doesn't matter, can just use radius 1 
            var rect = TileToSphereMercator(x, y, z, 1);

            if(bleedingPixels != 0)
            { 
                double bleedingFactor = bleedingPixels / 256.0 * 2;

                rect.Inflate(rect.Width * bleedingFactor, rect.Height * bleedingFactor);
            }

            return new Rect(SphereMercatorToWgs(rect.TopLeft, 1), SphereMercatorToWgs(rect.BottomRight, 1));
        }

        /// <summary>
        /// Convert a point relative to a mercator viewport to a point relative to an image
        /// </summary>
        public static Point MercatorToImage(Rect mercatorRect, Size imageSize, Point mercatorPoint)
        {
            return new Point(
              (mercatorPoint.X - mercatorRect.Left) / (mercatorRect.Right - mercatorRect.Left) * imageSize.Width,
              imageSize.Height - (mercatorPoint.Y - mercatorRect.Top) / (mercatorRect.Bottom - mercatorRect.Top) * imageSize.Height);
        }

        /// <summary>
        /// Convert a WGS (Lon,Lat) coordinate to a point relative to a tile image
        /// </summary>
        public static Point WgsToTile(uint x, uint y, uint z, Point wgsPoint, double clipWgsAtDegrees = 85.05)
        {
            if (clipWgsAtDegrees < 90)
                wgsPoint = ClipWgsPoint(wgsPoint, clipWgsAtDegrees);

            return MercatorToImage(TileToSphereMercator(x, y, z, 1), new Size(256, 256), WgsToSphereMercator(wgsPoint, 1));
        }

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
