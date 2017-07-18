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
            //AlwaysOnData Node003 = new AlwaysOnData();
            //Node003.loadData("C:\\Users\\zeche\\Documents\\WorkItems\\POC\\VM003_0.xel", "ZE-VM003");
            //AlwaysOnData Node001 = new AlwaysOnData();
            //Node001.loadData("C:\\Users\\zeche\\Documents\\WorkItems\\POC\\VM001_0.xel", "ZE-VM001");

            //Node001.MergeInstance(Node003);
            //Node001.AnalyzeReports();
            //Node001.ShowFailoverReports();
            //Console.ReadLine();
            testDemo();
        }

        static void testDemo()
        {
            TimeSpan diff = new TimeSpan(8, 0, 0);
            DateTimeOffset start = new DateTimeOffset(2017, 05, 13, 20, 12, 00,diff);
            DateTimeOffset end = new DateTimeOffset(2017, 05, 13, 20, 18, 00,diff);
            using (QueryableXEventData evts = new QueryableXEventData("C:\\Users\\zeche\\Documents\\WorkItems\\POC\\SYS_000_0.xel"))
            {
                foreach(PublishedEvent evt in evts)
                {
                    if(evt.Timestamp > start && evt.Timestamp < end)
                    {
                        Console.WriteLine("Event: {0}, time:{1} ", evt.Name, evt.Timestamp);
                        if(evt.Name.ToString() == "sp_server_diagnostics_component_result")
                        {
                            Console.WriteLine("component : {0} \n, Data: {1}", evt.Fields["component"].Value.ToString(), evt.Fields["data"].Value.ToString());
                        }
                    }
                }
            }
            Console.ReadLine();
        }
    }


}
