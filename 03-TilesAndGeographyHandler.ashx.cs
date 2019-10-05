using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Web;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for DynamicTilesHandler
    /// </summary>
    public class TilesAndGeographyHandler : IHttpHandler
    {
        // http://msdn.microsoft.com/en-us/library/bb259689.aspx
        public void ProcessRequest(HttpContext context)
        {
            //Parse request parameters
            if (!uint.TryParse(context.Request.Params["x"], out uint x))
                throw (new ArgumentException("Invalid parameter"));
            if (!uint.TryParse(context.Request.Params["y"], out uint y))
                throw (new ArgumentException("Invalid parameter"));
            if (!uint.TryParse(context.Request.Params["z"], out uint z))
                throw (new ArgumentException("Invalid parameter"));

            // Create a bitmap of size 256x256
            using (var bmp = new Bitmap(256, 256))
            // get graphics from bitmap
            using (var graphics = Graphics.FromImage(bmp))
            {
                // draw background
                //var brush = new LinearGradientBrush(new Point(0, 0), new Point(256, 256),
                //    Color.LightBlue, Color.Transparent);
                //graphics.FillRectangle(brush, new Rectangle(0, 0, 256, 256));
                //brush.Dispose();

                var rect = TransformTools.TileToWgs(x, y, z);
                int left = (int)Math.Floor(rect.Left);
                int right = (int)Math.Floor(rect.Right);
                int top = (int)Math.Floor(rect.Top);
                int bottom = (int)Math.Floor(rect.Bottom);

                for (int lon = left; lon <= right; lon++)
                {
                    for (int lat = top; lat <= bottom; lat++)
                    {
                        var g1 = new System.Windows.Point(lon, lat);
                        var g2 = new System.Windows.Point(lon + 1, lat + 1);
                        var p1 = TransformTools.WgsToTile(x, y, z, g1);
                        var p2 = TransformTools.WgsToTile(x, y, z, g2);

                        graphics.DrawLine(Pens.Black, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p2.X, (int)p1.Y));
                        graphics.DrawLine(Pens.Black, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p1.X, (int)p2.Y));             
                    }
                }

                //Stream the image to the client
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    // Saving a PNG image requires a seekable stream, first save to memory stream 
                    // http://forums.asp.net/p/975883/3646110.aspx#1291641
                    bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    var buffer = memoryStream.ToArray();

                    context.Response.ContentType = "image/png";
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}