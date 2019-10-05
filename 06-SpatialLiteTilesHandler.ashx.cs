using System;
using System.Drawing;
using System.Web;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for DynamicTilesHandler
    /// </summary>
    public class SpatialLiteTilesHandler : HttpTaskAsyncHandler
    {
        // going async here to improve scalability
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
                // calc rect from tile key
                var qw = TransformTools.TileToWgs(x, y, z);

                // build the sql
                var query = FormattableString.Invariant($@"
                    SELECT Id, AsBinary(Geometry) FROM WorldGeom 
                        WHERE ROWID IN 
                            (Select rowid FROM cache_WorldGeom_Geometry 
                                WHERE mbr = FilterMbrIntersects({qw.Left}, {qw.Bottom}, {qw.Right}, {qw.Top}))
                    ");

                using (var command = new SQLiteCommand(query, Global.cn))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
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
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }
    }
}