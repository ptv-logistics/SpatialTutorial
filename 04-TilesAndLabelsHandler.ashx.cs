using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Web;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for DynamicTilesHandler
    /// </summary>
    public class TilesAndLabelsHandler : HttpTaskAsyncHandler
    {
        // http://msdn.microsoft.com/en-us/library/bb259689.aspx
        public override async Task ProcessRequestAsync(HttpContext context)
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
                int symbolSize = 16; // the size of our symbol in pixel

                // calculate geo rect, taking the bleeding into account
                // = the maximum number of pixels a symbol can "bleed" into a neighbouring tile
                // see tile tile http://80.146.239.139/SpatialTutorial/04-TilesAndLabelsHandler.ashx?x=69&y=44&z=7   
                var rect = TransformTools.TileToWgs(x, y, z, symbolSize / 2);
                int left = (int)Math.Floor(rect.Left);
                int right = (int)Math.Floor(rect.Right);
                int top = (int)Math.Floor(rect.Top);
                int bottom = (int)Math.Floor(rect.Bottom);

                // draw text
                var font = new Font("Arial", symbolSize - 4);
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                for (double lon = left; lon <= right; lon++)
                {
                    for (double lat = top; lat <= bottom; lat++)
                    {
                        var g1 = new System.Windows.Point(lon, lat);
                        var g2 = new System.Windows.Point(lon + 1, lat + 1);
                        var p1 = TransformTools.WgsToTile(x, y, z, g1);
                        var p2 = TransformTools.WgsToTile(x, y, z, g2);

                        graphics.DrawLine(Pens.Black, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p2.X, (int)p1.Y));
                        graphics.DrawLine(Pens.Black, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p1.X, (int)p2.Y));

                        if (z < 6) // no symbols for levels < 6
                            continue;

                        // draw symbol for latitude
                        graphics.FillEllipse(Brushes.LightGray, (int)(p1.X + p2.X) / 2 - symbolSize, (int)p1.Y - symbolSize, symbolSize * 2, symbolSize * 2);
                        graphics.DrawEllipse(Pens.Black, (int)(p1.X + p2.X) / 2 - symbolSize, (int)p1.Y - symbolSize, symbolSize * 2, symbolSize * 2);
                        graphics.DrawString(string.Format("{0}°", lat), font, Brushes.Black, (int)(p1.X + p2.X) / 2, (int)p1.Y, format);

                        // draw symbol for longitude
                        graphics.FillEllipse(Brushes.LightGray, (int)(p1.X) - symbolSize, (int)(p1.Y + p2.Y) / 2 - symbolSize, symbolSize * 2, symbolSize * 2);
                        graphics.DrawEllipse(Pens.Black, (int)(p1.X) - symbolSize, (int)(p1.Y + p2.Y) / 2 - symbolSize, symbolSize * 2, symbolSize * 2);
                        graphics.DrawString(string.Format("{0}°", lon), font, Brushes.Black, (int)(p1.X), (int)(p1.Y + p2.Y) / 2, format);
                    }
                }

                font.Dispose();

                //Stream the image to the client
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    // Saving a PNG image requires a seekable stream, first save to memory stream 
                    // http://forums.asp.net/p/975883/3646110.aspx#1291641
                    bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    var buffer = memoryStream.ToArray();

                    context.Response.ContentType = "image/png";
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }
    }
}  