using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.XEvent.Linq;

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
            if (fileName == null)
            {
                return false;
            }
            else
            {try
                {
                    raw_data = new QueryableXEventData(fileName);
                    if( raw_data == null)
                    {
                        return false;
                    }
                    return true;
                }catch(Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("open XelFiel failed");
                    return false;
                }
            }
        }

        public void createTable()
        {
        }

        public void insertDataToTable()
        {

        }
        public void filterNoise()
        {

        }

        

    }
   
}
