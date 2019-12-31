using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Data.SQLite;
using System.Web.Hosting;

namespace SpatialTutorial
{
    public class Global : System.Web.HttpApplication
    {
        public static SQLiteConnection cn;

        protected void Application_Start(object sender, EventArgs e)
        {
            // open the database
            cn = new SQLiteConnection($@"Data Source={HostingEnvironment.MapPath("~/App_Data")}\db.sqlite;Version=3;");
            cn.Open();

            // set PATH for SpatialLite depending on processor
            var mod_spatialite_folderPath = (IntPtr.Size == 4) ?
                 "mod_spatialite-4.4.0-RC0-win-x86" : "mod_spatialite-4.4.0-RC0-win-amd64";

            string path =
                HostingEnvironment.MapPath("~/SpatialLite/") + mod_spatialite_folderPath + ";" +
                Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            // load spatiallite extension
            cn.LoadExtension("mod_spatialite");
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}