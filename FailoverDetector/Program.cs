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
            TestingSQLCommand testCommand = new TestingSQLCommand();
            testCommand.TestSQLConnection();
        }
    }

    class TestingSQLCommand
    {
        public void TestSQLConnection()
        {
            string connString = "Server=ze-vm001\\SQL18TEST01; Initial Catalog=FailoverDetector; Trusted_Connection=True;";
            string queryString = "select @@servername";
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
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
    }
}
