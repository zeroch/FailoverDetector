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

            // process AlwaysOn Health 
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
            }

            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            // parse ErrorLog
            ErrorLogParser errorLogParser = new ErrorLogParser();
            ClusterLogParser clusterLogParser = new ClusterLogParser();

            foreach (var node in pFileProcess.NodeList)
            {
                string nodeName = node.Key;
                FileProcessor.NodeFileInfo cNode = node.Value;
                Console.WriteLine("Node name: {0}", nodeName);
                foreach (var logPath in cNode.ErrorLogFileList)
                {
                    errorLogParser.ParseLog(logPath);
                }
                Console.WriteLine("Parsing Cluster Log:");
                clusterLogParser.ParseLog(cNode.ClusterLogPath);
            }


            // determine Failover 
            pReportMgr.AnalyzeReports();

            pReportMgr.ShowFailoverReports();

            Console.ReadLine();
        }

    }


}
