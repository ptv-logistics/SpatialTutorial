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
    /// Summary description for DynamicTilesHandler
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
                string sx1 = Convert.ToString(queryWindow.Left, CultureInfo.InvariantCulture);
                string sy1 = Convert.ToString(queryWindow.Top, CultureInfo.InvariantCulture);
                string sx2 = Convert.ToString(queryWindow.Right, CultureInfo.InvariantCulture);
                string sy2 = Convert.ToString(queryWindow.Bottom, CultureInfo.InvariantCulture);

                var strSql = string.Format(
                    @"SELECT WorldGeom.Id, AsBinary(Geometry), Pop/Area as PopDens FROM WorldGeom " + 
                    @"JOIN WorldData on WorldData.Id = WorldGeom.Id " + 
                    @"WHERE MBRIntersects(Geometry, BuildMbr({0}, {1}, {2}, {3}));",
                    sx1, sy2, sx2, sy1);

                var choroploeth = new Classification<double, System.Drawing.Color>();
                choroploeth.MinKey = 0;
                choroploeth.DefaultValue = System.Drawing.Color.White;
                choroploeth.Values = new SortedList<double, Color> { 
                { 190, palette[0] }, { 259, palette[1] }, { 509, palette[2] }, { 1900, palette[3] }, { 2590, palette[4] }, { 5090, palette[5] }, { 19000000, palette[6] } };

                using (SQLiteCommand command = new SQLiteCommand(strSql, Global.cn))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        byte[] wkb = reader[1] as byte[];
                        double popDens = -1;
                        if(!reader.IsDBNull(2))
                            popDens = reader.GetDouble(2);

                        // create GDI path from wkb
                        var path = WkbToGdi.Parse(wkb, p => TransformTools.WgsToTile(x, y, z, p));

                        // fill polygon
                        var color = choroploeth.GetValue(popDens);
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
        
        // the palette for the choropleth
        System.Drawing.Color[] palette = new System.Drawing.Color[]
            {
                System.Drawing.Color.Green,
                System.Drawing.Color.LightGreen,
                System.Drawing.Color.Yellow,                               
                System.Drawing.Color.Orange,                                
                System.Drawing.Color.Red,                                
                System.Drawing.Color.DarkRed,                                
                System.Drawing.Color.Purple,
        };
        
        public bool IsReusable
        {
            get { return true; }
        }
    }

    public class Classification<K, V> where K : IComparable
    {
        public V DefaultValue { get; set; }

        public K MinKey { get; set; }

        public SortedList<K, V> Values { get; set; }

        public V GetValue(K key)
        {
            if (key == null)
                return DefaultValue;

            if (key.CompareTo(MinKey) < 0)
                return DefaultValue;

            // todo: maybe implement some O(log n) method
            foreach (K k in Values.Keys)
            {
                if (k.CompareTo(key) > 0)
                    return Values[k];                 
            }

            return DefaultValue;
        }
    }
}