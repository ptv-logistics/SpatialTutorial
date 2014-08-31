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
            String slPath = System.Web.Hosting.HostingEnvironment.MapPath("~/SpatialLite");
            String path = Environment.GetEnvironmentVariable("path");
            if (path == null) path = "";
            if (!path.ToLowerInvariant().Contains(slPath.ToLowerInvariant()))
                Environment.SetEnvironmentVariable("path", slPath + ";" + path);

            cn = new SQLiteConnection(@"Data Source=|DATADIRECTORY|\db.sqlite;Version=3;"); 
            cn.Open();
            SQLiteCommand cm = new SQLiteCommand(String.Format("SELECT load_extension('{0}');", "libspatialite-4.dll"), cn);
            cm.ExecuteNonQuery();
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