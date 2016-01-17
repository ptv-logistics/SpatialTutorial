using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Web;
using System.Data.SQLite;
using System.Globalization;
using System.Collections.Generic;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for ThematicTilesHandler
    /// </summary>
    public class ThematicTilesHandler : IHttpHandler
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
                var strSql = string.Format(CultureInfo.InvariantCulture,
                    @"SELECT WorldGeom.Id, AsBinary(Geometry), Pop/Area as PopDens FROM WorldGeom " +
                    @"JOIN WorldData on WorldData.Id = WorldGeom.Id " +
                    @"WHERE MBRIntersects(Geometry, BuildMbr({0}, {1}, {2}, {3}));",
                    queryWindow.Left, queryWindow.Top, queryWindow.Right, queryWindow.Bottom);

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

                using (SQLiteCommand command = new SQLiteCommand(strSql, Global.cn))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        byte[] wkb = reader[1] as byte[];
                        double popDens = reader.IsDBNull(2)? -1 : reader.GetDouble(2);
 
                        // create GDI path from wkb
                        var path = WkbToGdi.Parse(wkb, p => TransformTools.WgsToTile(x, y, z, p));

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
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public bool IsReusable
        {
            get { return true; }
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