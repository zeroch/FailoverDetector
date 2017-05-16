using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.XEvent.Linq;
using System.Collections;


namespace FailoverDetector
{
    public class XEventContainer
    {
        enum XEventType
        {
            AlwaysOnHealth,
            SystemHealth,
        };

        private string instanceName;
        private const string connectionString = "Initial Catalog=FailoverDetector; Trusted_Connection=True;";
        private SqlConnection m_conn;
        private SqlCommand insert_command;
        public QueryableXEventData raw_data;

        public bool openXelFile(string fileName)
        {
            if (fileName.Length == 0)
            {
                return false;
            }
            else
            { try
                {
                    raw_data = new QueryableXEventData(fileName);
                    return true;
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("open XelFiel failed");
                    return false;
                }
            }
        }

        public void getStateChange()
        {

            raw_data.Where(e => e.Name == "error_reported");
            //IEnumerable<string> allEventName = raw_data.Select(evt => evt.Name);
            //foreach( string s in allEventName)
            //{
            //    Console.WriteLine(s);
            //}

        }

        public void filterNoise()
        {
            
        }

        

    }
   
}
