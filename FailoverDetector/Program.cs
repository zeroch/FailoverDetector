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
            testDemo();
            Console.ReadLine();
        }
        static void testDemo()
        {
            TimeSpan diff = new TimeSpan(-7, 0, 0);
            DateTimeOffset start = new DateTimeOffset(2017, 07, 08, 22, 26, 00, diff);
            DateTimeOffset end = new DateTimeOffset(2017, 07, 08, 22, 29, 00, diff);
            string url = "C:\\Users\\zeche\\Documents\\WorkItems\\POC\\SYS001_0.xel";
            using (QueryableXEventData evts = new QueryableXEventData(url))
            {
                foreach (PublishedEvent evt in evts)
                {

                    if (evt.Name.ToString() == "sp_server_diagnostics_component_result")
                    {
                        String t_component = evt.Fields["component"].Value.ToString();
                        String t_data = evt.Fields["data"].Value.ToString();
                        SystemHealthParser parser = new SystemHealthParser();
                        switch (t_component)
                        {
                            case "QUERY_PROCESSING":
                                if (!parser.ParseQPComponent(t_data))
                                {
                                    Console.WriteLine("Event: {0}, time:{1} ", evt.Name, evt.Timestamp);
                                }
                                break;
                            case "SYSTEM":
                                parser.ParseSystemComponent(t_data);
                                break;
                            case "RESOURCE":
                                parser.ParseResource(t_data);
                                break;
                            case "IO_SUBSYSTEM":
                                parser.ParseIOSubsytem(t_data);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

    }


}
