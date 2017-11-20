using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace FailoverDetector
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Directory.GetCurrentDirectory();
            path += @"\Data";
            string[] nodes = Directory.GetDirectories(path);
            List<AlwaysOnData> nodeList = new List<AlwaysOnData>();
            int i = 0;
            foreach( string node in nodes)
            {
                string xelPath = node + @"\AlwaysOn_health*.xel";
                string nodeName = node.Remove(0, path.Length + 1);
                Console.WriteLine("xel file path is : {0}\n Node name: {1}", xelPath, nodeName);
                var instance = new AlwaysOnData();
                instance.LoadData(xelPath, nodeName);


                // actually we use a singlton Report Manager to handle reports
                // all reports are merged while new insert.
                // no more merge is needed here. 
                //if(i != 0 )
                //{
                //    nodeList.First().MergeInstance(instance);
                //}
                nodeList.Add(instance);
                i++;

            }

            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            pReportMgr.AnalyzeReports();

            pReportMgr.ShowFailoverReports();

            Console.ReadLine();
        }

    }


}
