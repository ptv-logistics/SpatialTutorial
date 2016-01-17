using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Globalization;
using System.Configuration;
using System.Data.SQLite;

namespace SpatialTutorial
{
    /// <summary>
    /// Summary description for SpatialPickHandler
    /// </summary>
    public class SpatialPickingHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            try
            {
                //Parse request parameters
                double lat, lng;
                if (!double.TryParse(context.Request.Params["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                    throw (new ArgumentException("Invalid parameter"));
                if (!double.TryParse(context.Request.Params["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out lng))
                    throw (new ArgumentException("Invalid parameter"));

                // convert line string to polygon
                var text = "";// isoResult.wrappedIsochrones[0].polys.wkt;
                text = text.Replace("LINESTRING (", "POLYGON ((");
                text = text.Replace(")", "))");


                var strSql = string.Format(CultureInfo.InvariantCulture,
                    @"SELECT WorldGeom.Id, AsText(Geometry), Name, Region, Area, Pop FROM WorldGeom " +
                    @"JOIN WorldData on WorldData.Id = WorldGeom.Id " +
                    @"WHERE Intersects(Geometry, MakePoint({0}, {1}));",
                    lng, lat);

                using (SQLiteCommand command = new SQLiteCommand(strSql, Global.cn))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string type;

                        int id = reader.GetInt32(0);
                        string str = reader.GetString(1);
                        string name = reader.GetString(2);
                        string region = reader.GetString(3);
                        double area = reader.GetDouble(4);
                        double pop = reader.GetDouble(5);

                        // convert wkt to GeoJson
                        if (str.StartsWith("POLYGON"))
                        {
                            type = "Polygon";
                            str = str
                                .Replace("POLYGON", "")
                                .Trim();
                        }
                        else
                        {
                            type = "MultiPolygon";
                            str = str
                                .Replace("MULTIPOLYGON", "")
                                .Trim();
                        }

                        str = str
                            .Replace(", ", "],[")
                            .Replace(" ", ",")
                            .Replace("(", "[")
                            .Replace(")", "]")
                            .Replace(",", ", ");

                        // buiold response
                        context.Response.ContentType = "text/json";
                        context.Response.Write(string.Format(CultureInfo.InvariantCulture,
                            @"{{""geometry"": {{""type"": ""{0}"",""coordinates"": [{1}]}},""type"": ""Feature""," + 
                            @"""properties"": {{""name"": ""{2}"", ""region"": ""{3}"",""area"": ""{4}"",""pop"": ""{5}""}}}}",
                            type, str, name, region, area, pop));

//{
//  ""type"": """ + type + @""",
//  ""description"": """ + string.Format(CultureInfo.InvariantCulture, "{0:0,0}", name) + " households<br>"
//                    + string.Format(CultureInfo.InvariantCulture, "{0:n}", 0) + " avg. power" + @""",
//  ""coordinates"": [" + str + @"]
//}");
                        return;
                    }
                }

                // no result - return empty json
                context.Response.ContentType = "text/json";
                context.Response.Write("{}");
            }
            catch (Exception ex)
            {
                // no result - return empty json
                context.Response.ContentType = "text/json";
                context.Response.Write(@"{  ""error"": """ + ex.Message + @"""}");
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}