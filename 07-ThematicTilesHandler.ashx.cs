using System;
using System.Drawing;
using System.Web;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for ThematicTilesHandler
    /// </summary>
    public class ThematicTilesHandler : HttpTaskAsyncHandler
    {
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            //Parse request parameters
            if (!uint.TryParse(context.Request.Params["x"], out uint x))
                throw (new ArgumentException("Invalid parameter"));
            if (!uint.TryParse(context.Request.Params["y"], out uint y))
                throw (new ArgumentException("Invalid parameter"));
            if (!uint.TryParse(context.Request.Params["z"], out uint z))
                throw (new ArgumentException("Invalid parameter"));

            // definition of our choropletz
            var choropleth = new Classification<double, Color>
            {
                MinValue = 0, // lower border for classification
                DefaultAttribute = Color.White, // color if key hits no class
                Values = new SortedList<double, Color> { // the classes
                        { 50, Color.Green },
                        { 100, Color.LightGreen },
                        { 250, Color.Yellow },
                        { 500,Color.Orange },
                        { 1000, Color.Red },
                        { 2500, Color.DarkRed },
                        { double.MaxValue, Color.Purple }
                    }
            };

            // Create a bitmap of size 256x256
            using (var bmp = new Bitmap(256, 256))
            // get graphics from bitmap
            using (var graphics = Graphics.FromImage(bmp))
            {
                // calc rect from tile key
                var qw = TransformTools.TileToWgs(x, y, z);

                // build the sql
                var query = FormattableString.Invariant(
                    $@"
                    Select WorldData.Id, AsBinary(Geometry), Pop/Area as PopDens from 
                        (SELECT * from WorldGeom
                            WHERE ROWID IN 
                                (Select rowid FROM cache_WorldGeom_Geometry WHERE
                                    mbr = FilterMbrIntersects({qw.Left}, {qw.Bottom}, {qw.Right}, {qw.Top}))) as g 
                        JOIN WorldData on WorldData.Id = g.Id
                    ");

                using (var command = new SQLiteCommand(query, Global.cn))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = reader.GetInt32(0);
                        byte[] wkb = reader[1] as byte[];
                        double popDens = reader.IsDBNull(2) ? -1 : reader.GetDouble(2);

                        // create GDI path from wkb
                        var path = WkbToGdi.Parse(wkb, p => TransformTools.WgsToTile(x, y, z, p));

                        // degenerated polygon
                        if (path == null)
                            continue;

                        // fill polygon
                        var color = choropleth.GetValue(popDens);
                        var fill = new SolidBrush(Color.FromArgb(168, color.R, color.G, color.B));
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

    public class Classification<V, A> where V : IComparable
    {
        public A DefaultAttribute { get; set; }

        public V MinValue { get; set; }

        public SortedList<V, A> Values { get; set; }

        public A GetValue(V key)
        {
            if (key == null)
                return DefaultAttribute;

            if (key.CompareTo(MinValue) < 0)
                return DefaultAttribute;

            // todo: maybe implement some O(log n) method
            foreach (V k in Values.Keys)
            {
                if (k.CompareTo(key) > 0)
                    return Values[k];
            }

            return DefaultAttribute;
        }
    }
}