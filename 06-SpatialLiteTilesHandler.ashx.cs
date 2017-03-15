using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Web;
using System.Data.SQLite;
using System.Globalization;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for DynamicTilesHandler
    /// </summary>
    public class SpatialLiteTilesHandler : IHttpHandler
    { 
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
                // calc rect from tile key
                var queryWindow = TransformTools.TileToWgs(x, y, z);

                // build the sql
                string sx1 = Convert.ToString(queryWindow.Left, CultureInfo.InvariantCulture);
                string sy1 = Convert.ToString(queryWindow.Top, CultureInfo.InvariantCulture);
                string sx2 = Convert.ToString(queryWindow.Right, CultureInfo.InvariantCulture);
                string sy2 = Convert.ToString(queryWindow.Bottom, CultureInfo.InvariantCulture);

                var strSql = string.Format(
                    "SELECT Id, AsBinary(Geometry) FROM WorldGeom WHERE ROWID IN " +
                    "(Select rowid FROM cache_WorldGeom_Geometry WHERE mbr = FilterMbrIntersects({0}, {1}, {2}, {3}));",
                    sx1, sy2, sx2, sy1);

                using (SQLiteCommand command = new SQLiteCommand(strSql, Global.cn))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        byte[] wkb = reader[1] as byte[];

                        // create GDI path from wkb
                        var path = WkbToGdi.Parse(wkb, p => TransformTools.WgsToTile(x, y, z, p));

                        // degenerated polygon
                        if (path == null)
                            continue;

                        // fill polygon
                        var fill = new SolidBrush(Color.FromArgb(168, 0, 0, 255));
                        graphics.FillPath(fill, path);
                        fill.Dispose();

                        // draw outline
                        graphics.DrawPath(Pens.Black, path);
                    }

                    reader.Close();
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