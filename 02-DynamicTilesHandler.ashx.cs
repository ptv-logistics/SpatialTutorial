using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Web;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for DynamicTilesHandler
    /// </summary>
    public class DynamicTilesHandler : HttpTaskAsyncHandler
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
                // draw background
                var brush = new LinearGradientBrush(new Point(0, 0), new Point(256, 256),
                    Color.LightBlue, Color.Transparent);
                graphics.FillRectangle(brush, new Rectangle(0, 0, 256, 256));
                brush.Dispose();

                graphics.DrawRectangle(Pens.Black, new Rectangle(0, 0, 255, 255));

                // draw text
                var font = new Font("Arial", 16);
                graphics.DrawString(string.Format("{0}/{1}/{2}", z, x, y),
                    font, Brushes.Black, 0, 0, StringFormat.GenericDefault);
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