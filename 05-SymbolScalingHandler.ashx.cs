using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Web;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for DynamicTilesHandler
    /// </summary>
    public class SymbolScalingHandler : IHttpHandler
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
                // widen stroke width based on zoom
                int strokeSize = (z > 15) ? 4 : (z > 10) ? 3 : (z > 5) ? 2 : 1;

                // calculate map scale
                var mapSize = 256 * Math.Pow(2, z); // size of the map in pixel
                double earthCircumfence = 2.0 * Math.PI * 6378137.0; // circumfence of earth
                var scale = mapSize / earthCircumfence; // pixel per mercator unit

                // Calculate the symbol sizes with 3 different scaling modes
                // 1 - constant scaling - radius is always 16 pixels
                int sz1 = (int)(16 * Math.Pow(scale, 0.0));

                // 2 - linear scaling - the symbol has a size of 10000 merctor units (=meter at equator, equals sz = 10000 * scale)
                int sz2 = (int)(10000 * Math.Pow(scale, 1.0));

                // 2 - logarithmic scaling - the size is adapted with a base size (64) and a scaling factor (0.25)
                int sz3 = (int)(64 * Math.Pow(scale, 0.25));


                // the maximum number of pixels a symbol can "bleed" into a neighbouring tile
                var bleedingPixels = (Math.Max(Math.Max(sz1, sz2), sz3) + strokeSize) / 2;

                var rect = TransformTools.TileToWgs(x, y, z, bleedingPixels);
                int left = (int)Math.Floor(rect.Left);
                int right = (int)Math.Floor(rect.Right);
                int top = (int)Math.Floor(rect.Top);
                int bottom = (int)Math.Floor(rect.Bottom);
               
                var pen = new System.Drawing.Pen(System.Drawing.Color.Black, strokeSize);
                var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                for (int lon = left; lon <= right; lon++)
                {
                    for (int lat = top; lat <= bottom; lat++)
                    {
                        var g1 = new System.Windows.Point(lon, lat);
                        var g2 = new System.Windows.Point(lon + 1, lat + 1);
                        var p1 = TransformTools.WgsToTile(x, y, z, g1);
                        var p2 = TransformTools.WgsToTile(x, y, z, g2);

                        graphics.DrawLine(pen, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p2.X, (int)p1.Y));
                        graphics.DrawLine(pen, new System.Drawing.Point((int)p1.X, (int)p1.Y), new System.Drawing.Point((int)p1.X, (int)p2.Y));

                        if (z < 6) // no symbols for levels < 6
                            continue;

                        int sz;
                        Brush brush;
                        switch ((lat + lon) % 3) // switch between the 3 size modes
                        {
                            case 0: sz = sz1; brush = Brushes.LightGreen; break;
                            case 1: sz = sz2; brush = Brushes.LightYellow; break;
                            default: sz = sz3; brush = Brushes.LightBlue; break;
                        }

                        graphics.FillEllipse(brush, (int)(p1.X + p2.X) / 2 - sz, (int)p1.Y - sz, sz * 2, sz * 2);
                        graphics.FillEllipse(brush, (int)(p1.X) - sz, (int)(p1.Y + p2.Y) / 2 - sz, sz * 2, sz * 2);

                        graphics.DrawEllipse(pen, (int)(p1.X + p2.X) / 2 - sz, (int)p1.Y - sz, sz * 2, sz * 2);
                        graphics.DrawEllipse(pen, (int)(p1.X) - sz, (int)(p1.Y + p2.Y) / 2 - sz, sz * 2, sz * 2);

                        if (sz > 4)
                        {
                            var font = new Font("Arial", sz - 4);

                            graphics.DrawString(string.Format("{0}°", lat), font, Brushes.Black, (int)(p1.X + p2.X) / 2, (int)p1.Y, format);
                            graphics.DrawString(string.Format("{0}°", lon), font, Brushes.Black, (int)(p1.X), (int)(p1.Y + p2.Y) / 2, format);

                            font.Dispose();
                        }
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