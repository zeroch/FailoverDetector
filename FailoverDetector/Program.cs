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

            AlwaysOnData Node003 = new AlwaysOnData();
            Node003.loadData("C:\\Users\\zeche\\Documents\\WorkItems\\POC\\VM003_0.xel", "ZE-VM003");
            AlwaysOnData Node001 = new AlwaysOnData();
            Node001.loadData("C:\\Users\\zeche\\Documents\\WorkItems\\POC\\VM001_0.xel", "ZE-VM001");

            Node001.MergeInstance(Node003);
            Node001.AnalyzeReports();
            Node001.ShowFailoverReports();

            Console.ReadLine();
        }

    }


}
