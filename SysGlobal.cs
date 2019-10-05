using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PerpetualIntegrator
{
    public class SysGlobal
    {
        public static String ConnectionStringConfig()
        {
            String ConnectionString = "";
            String settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SysCurrent.json");

            String json;
            using (StreamReader trmRead = new StreamReader(settingsPath))
            {
                json = trmRead.ReadToEnd();
            }

            JavaScriptSerializer js = new JavaScriptSerializer();
            Models.SysCurrent s = js.Deserialize<Models.SysCurrent>(json);

            ConnectionString = s.POSConnectionString;

            return ConnectionString;

        }
    }
}
