using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Data.SQLite;

namespace SpatialTutorial
{
    public class Global : System.Web.HttpApplication
    {
        public static SQLiteConnection cn;

        protected void Application_Start(object sender, EventArgs e)
        {
            var mod_spatialite_folderPath = (IntPtr.Size == 4) ?
                 "mod_spatialite-4.4.0-RC0-win-x86" : "mod_spatialite-4.4.0-RC0-win-amd64";

            //using relative path, cannot use absolute path, dll load will fail
            string path =
                System.Web.Hosting.HostingEnvironment.MapPath("~/SpatialLite/") + mod_spatialite_folderPath + ";" +
                Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine);

            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);

            cn = new SQLiteConnection(@"Data Source=|DATADIRECTORY|\db.sqlite;Version=3;");
            cn.Open();

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