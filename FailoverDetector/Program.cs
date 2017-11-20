using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FailoverDetector.Utils;

namespace FailoverDetector
{
    class Program
    {
        static void Main(string[] args)
        {
            string demoPath = @"C:\Temp\FailoverDetector\Data";

            FileProcessor pFileProcess = new FileProcessor(demoPath);
            pFileProcess.RootDirectory(demoPath);

            List<AlwaysOnData> nodeList = new List<AlwaysOnData>();
            foreach( var node in pFileProcess.NodeList)
            {

                
                string nodeName = node.Key;
                FileProcessor.NodeFileInfo cNode = node.Value;
                Console.WriteLine("Node name: {0}",  nodeName);
                var instance = new AlwaysOnData();
                foreach (var xelPath in cNode.AlwaysOnFileList)
                {
                    instance.LoadData(xelPath, nodeName);
                }



                // actually we use a singlton Report Manager to handle reports
                // all reports are merged while new insert.
                // no more merge is needed here. 
                //if(i != 0 )
                //{
                //    nodeList.First().MergeInstance(instance);
                //}
                nodeList.Add(instance);
            }

            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            pReportMgr.AnalyzeReports();

            // parse ErrorLog
            ErrorLogParser errorLogParser = new ErrorLogParser();

            //errorLogParser.ParseLog();


            pReportMgr.ShowFailoverReports();

            Console.ReadLine();
        }

    }


}
