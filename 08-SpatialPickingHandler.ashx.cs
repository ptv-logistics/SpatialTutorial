using System;
using System.Data.SQLite;
using System.Globalization;
using System.Web;

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

                // Select elements containing the point, pre-filter with mbr-cache to optimize performance
                var strSql = FormattableString.Invariant(
                    $@"
                    SELECT WorldData.Id, AsGeoJSON(Geometry), Name, Region, Area, Pop from 
                        (SELECT * from WorldGeom WHERE 
                            ROWID IN 
                                (Select rowid FROM cache_WorldGeom_Geometry 
                                    WHERE mbr = FilterMbrIntersects({lng}, {lat}, {lng}, {lat}))
                            AND Intersects(Geometry, MakePoint({lng}, {lat}))) as g                 
                        JOIN WorldData on WorldData.Id = g.Id 
                    ");

                using (SQLiteCommand command = new SQLiteCommand(strSql, Global.cn))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string str = reader.GetString(1);
                        string name = reader.GetString(2);
                        string region = reader.GetString(3);
                        double area = reader.GetDouble(4);
                        double pop = reader.GetDouble(5);

                        // build response
                        context.Response.ContentType = "text/json";
                        context.Response.Write(string.Format(CultureInfo.InvariantCulture,
                            @"{{""geometry"": {0},""type"": ""Feature""," +
                            @"""properties"": {{""name"": ""{1}"", ""region"": ""{2}"", ""area"": ""{3}"", ""pop"": ""{4}""}}}}",
                            str, name, region, area, pop));

                        return;
                    }
                }

                // no result - return empty json
                context.Response.ContentType = "text/json";
                context.Response.Write("{}");
            }
            catch (Exception ex)
            {
                // exception - return error
                context.Response.ContentType = "text/json";
                context.Response.Write(@"{  ""error"": """ + ex + @"""}");
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }
    }
}