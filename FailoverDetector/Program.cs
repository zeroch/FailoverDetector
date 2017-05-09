using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.XEvent.Linq;

namespace FailoverDetector
{
    class Program
    {
        static void Main(string[] args)
        {

            string connStr = "Server=ze-vm001\\SQL18TEST01; Initial Catalog=FailoverDetector; Trusted_Connection=True;";
            TestingSQLCommand testCommand = new TestingSQLCommand(connStr);
            testCommand.TestSQLConnection();
            //testCommand.TestSQLPrepare("NodeA");
            //testCommand.openXelFile("C:\\AlwaysOn_health_0_131180526930290000.xel");
            testCommand.TestInsertRow();
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

        public void TestInsertRow()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(this.connString))
                {
                    conn.Open();
                    SqlCommand command = new SqlCommand(null, conn);

                    command.CommandText =
                        "Insert into NodeA( server_name, event_name, timeStamp, current_state, message)" +
                        "VALUES(@name, @evt_name,@time, @state, @msg)";

                    SqlParameter srvParam = new SqlParameter("@name", System.Data.SqlDbType.Text, 30);
                    SqlParameter evtParam = new SqlParameter("@evt_name", System.Data.SqlDbType.Text, 120);
                    SqlParameter timeParam = new SqlParameter("@time", System.Data.SqlDbType.DateTimeOffset, 30);
                    SqlParameter stateParam = new SqlParameter("@state", System.Data.SqlDbType.Text, 50);
                    SqlParameter msgParam = new SqlParameter("@msg", System.Data.SqlDbType.Text, 250);

                    srvParam.Value = "NodeA";
                    command.Parameters.Add(srvParam);

                    evtParam.Value = "I am a event";
                    command.Parameters.Add(evtParam);

                    timeParam.Value = DateTimeOffset.Now;
                    command.Parameters.Add(timeParam);

                    stateParam.Value = "Bad";
                    command.Parameters.Add(stateParam);

                    msgParam.Value = "this is a terrible demo";
                    command.Parameters.Add(msgParam);

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

        public bool openXelFile(string fIn)
        {
            if (fIn == null)
            {
                return false;
            }else
            {
                    using (QueryableXEventData events = new QueryableXEventData(fIn))
                    {
                        foreach (PublishedEvent evt in events)
                        {
                            Console.WriteLine("{0}: {1}", evt.Name, evt.Timestamp);
                            foreach (PublishedEventField fld  in evt.Fields )
                            {
                                Console.WriteLine("\tField: {0} = {1}", fld.Name, fld.Value);
                            }
                        }
                    }
                return true;
            }
        }
    }
}
