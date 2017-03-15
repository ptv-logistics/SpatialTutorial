using SharpMap.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace ShapeImport
{
    class Program
    {
        static void Main(string[] args)
        {
            string appPath = typeof(Program).Assembly.Location.Substring(0, typeof(Program).Assembly.Location.LastIndexOf("\\"));

            // delete old db
            if (System.IO.File.Exists(string.Format(@"{0}\Data\db.sqlite", appPath)))
                System.IO.File.Delete(string.Format(@"{0}\Data\db.sqlite", appPath));

            var mod_spatialite_folderPath = (IntPtr.Size == 4) ?
                 "mod_spatialite-4.4.0-RC0-win-x86" : "mod_spatialite-4.4.0-RC0-win-amd64";

            //using relative path, cannot use absolute path, dll load will fail
            var slPath = appPath.Substring(0, appPath.LastIndexOf('\\'));
            slPath = slPath.Substring(0, slPath.LastIndexOf('\\'));
            slPath = slPath.Substring(0, slPath.LastIndexOf('\\'));

            string path =
                slPath + @"\SpatialLite\" + mod_spatialite_folderPath + ";" +
                Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            var cn = new SQLiteConnection(string.Format(@"Data Source={0}\Data\db.sqlite;Version=3;", appPath));
            cn.Open();

            cn.LoadExtension("mod_spatialite");

            var cm = new SQLiteCommand("SELECT InitSpatialMetadata();", cn);
            cm.ExecuteNonQuery();

            // create geometry table
            cm = new SQLiteCommand(
@"CREATE TABLE WorldGeom (" +
@"ID INTEGER PRIMARY KEY AUTOINCREMENT)", cn);
            cm.ExecuteNonQuery();

            cm = new SQLiteCommand("SELECT AddGeometryColumn('WorldGeom', 'Geometry', 4326, 'GEOMETRY', 2)", cn);
            cm.ExecuteNonQuery();

            // create feature data table
            cm = new SQLiteCommand(
@"CREATE TABLE WorldData (" +
@"ID INTEGER PRIMARY KEY AUTOINCREMENT, " +
@"ISO2 VARCHAR(2) NOT NULL, " +
@"ISO3 VARCHAR(3) NOT NULL, " +
@"Name TEXT NOT NULL, " +
@"Region TEXT NOT NULL, " +
@"Area DOUBLE NOT NULL, " +
@"Pop DOUBLE NOT NULL);", cn);
            cm.ExecuteNonQuery();

            cm = new SQLiteCommand("SELECT CreateMbrCache('WorldGeom', 'Geometry');", cn);
            cm.ExecuteNonQuery();

            // copy shape data to sqlite
            var shapeFile = appPath + @"\Data\world_countries_boundary_file_world_2002.shp";
            var shp = new SharpMap.Data.Providers.ShapeFile(shapeFile);
            shp.Open();

            FeatureDataSet ds = new FeatureDataSet();
            shp.ExecuteIntersectionQuery(new SharpMap.Geometries.BoundingBox(double.MinValue, double.MinValue, double.MaxValue, double.MaxValue), ds);
          
            foreach (FeatureDataRow row in ds.Tables[0].Rows)
            {
                var bytes = SharpMap.Converters.WellKnownBinary.GeometryToWKB.Write(row.Geometry);
                cm = new SQLiteCommand("INSERT INTO WorldGeom (Geometry) VALUES (GeomFromWkb(@wkb, 4326))", cn);
                cm.Parameters.AddWithValue("@wkb", bytes);
                cm.ExecuteNonQuery();

                cm = new SQLiteCommand("INSERT INTO WorldData (ISO2, ISO3, Name, Region, Area, Pop) VALUES (@iso2, @iso3, @name, @region, @area, @pop)", cn);
                cm.Parameters.AddWithValue("@iso2", row["ISO_2_CODE"]);
                cm.Parameters.AddWithValue("@iso3", row["ISO_3_CODE"]);
                cm.Parameters.AddWithValue("@name", row["NAME"]);
                cm.Parameters.AddWithValue("@region", row["REGION"]);
                cm.Parameters.AddWithValue("@area", row["AREA"]);
                cm.Parameters.AddWithValue("@pop", row["POP2005"]);

                cm.ExecuteNonQuery();
            }

            shp.Close();

            // anlyze table
            cm = new SQLiteCommand("ANALYZE WorldGeom;", cn);
            cm.ExecuteNonQuery();

            cn.Close();
        }
    }
}
