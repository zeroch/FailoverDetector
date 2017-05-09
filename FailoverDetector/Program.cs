using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace FailoverDetector
{
    class Program
    {
        static void Main(string[] args)
        {

            string connStr = "Server=ze-vm001\\SQL18TEST01; Initial Catalog=FailoverDetector; Trusted_Connection=True;";
            TestingSQLCommand testCommand = new TestingSQLCommand(connStr);
            testCommand.TestSQLConnection();
            testCommand.TestSQLPrepare("NodeA");
            Console.ReadLine(); 
        }
    }

    public class TestingSQLCommand
    {
        private String connString;

        public TestingSQLCommand(string connStr)
        {
            connString = connStr;
        }
        public String getConnString()
        {
            return connString;
        }

        public void TestSQLConnection()
        {
            string queryString = "select @@servername";
            try
            {
                using (SqlConnection conn = new SqlConnection(this.connString))
                {
                    SqlCommand command = new SqlCommand(queryString, conn);
                    command.Connection.Open();
                    var ret = command.ExecuteScalar();
                    Console.WriteLine(ret.ToString());
                }
            }catch(SqlException e)
            {
                Console.WriteLine(e.Message);
            }
            
        }

        public void TestSQLPrepare(string instanceName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(this.connString))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand(null, conn);

                    command.CommandText =
                        "CREATE TABLE " + instanceName + " (" +
                        " [server_name] [NVARCHAR](128) NULL," +
                        " [event_name][NVARCHAR](60) NOT NULL," +
                        " [timeStamp][datetimeoffset](7)," +
                        "[current_state][NVARCHAR](60) NULL," +
                        "[message][NVARCHAR](250) NULL" +
                        ")";


                    //SqlParameter dbParam = new SqlParameter("@NodeName", System.Data.SqlDbType.Text, 20);
                    //dbParam.Value = instanceName;
                    //command.Parameters.Add(dbParam);
                    command.Prepare();
                    var ret = command.ExecuteScalar();
                    if (ret != null)
                        Console.WriteLine(ret.ToString());
                    else
                        Console.WriteLine("No return value from sql execution.");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

    }
}
