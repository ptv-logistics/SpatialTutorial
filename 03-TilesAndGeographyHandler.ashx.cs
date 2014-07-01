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
            uint x, y, z;

            //Parse request parameters
            if (!uint.TryParse(context.Request.Params["x"], out x))
                throw (new ArgumentException("Invalid parameter"));
            if (!uint.TryParse(context.Request.Params["y"], out y))
                throw (new ArgumentException("Invalid parameter"));
            if (!uint.TryParse(context.Request.Params["z"], out z))
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

                int strokeSize;
                 if (z > 15)
                    strokeSize = 4;
                else if (z > 10)
                    strokeSize = 3;
                else if (z > 5)
                    strokeSize = 2;
                else 
                    strokeSize = 1;   

                int sz = (int)(5 * Math.Pow(Math.Pow(2, z), .2));
                var pen = (strokeSize == 1) ? Pens.Black : new System.Drawing.Pen(System.Drawing.Color.Black, strokeSize);

                var rect = TransformTools.TileToWgs(x, y, z);
                double left = Math.Floor(rect.Left);
                double right = Math.Floor(rect.Right);
                double top = Math.Ceiling(rect.Top);
                double bottom = Math.Ceiling(rect.Bottom);             

                for (double lon = left; lon <= right; lon++)
                {
                    for (double lat = top; lat <= bottom; lat++)                    
                    {
                        var g1 = new System.Windows.Point(lon, lat);
                        var g2 = new System.Windows.Point(lon + 1, lat - 1);
                        var p1 = TransformTools.WgsToTile(x, y, z, g1);
                        var p2 = TransformTools.WgsToTile(x, y, z, g2);

                        graphics.DrawLine(pen, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p2.X, (int)p1.Y));
                        graphics.DrawLine(pen, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p1.X, (int)p2.Y));             
                    }
                }

                if(strokeSize != 1)
                    pen.Dispose();

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