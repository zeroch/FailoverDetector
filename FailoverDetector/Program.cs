using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FailoverDetector.Utils;

namespace FailoverDetector
{
    class Program
    {
        static void Main(string[] args)
        {

            FileProcessor pFileProcess = new FileProcessor();

            if (!pFileProcess.ProcessParameter(args))
            {
                Console.WriteLine("Exit with Error.");
                return;
            }

            // Handle Necessaries Files
            pFileProcess.ProcessDirectory();
            if (!pFileProcess.FoundConfiguration)
            {
                return;
            }
            pFileProcess.ParseConfigurationFile();
            pFileProcess.CopySourceDataFromRemote();
            pFileProcess.ProcessDirectory();
            pFileProcess.ValidateFileCoverage();

            // process AlwaysOn Health 
            foreach ( var node in pFileProcess.NodeList)
            {
                string nodeName = node.Key;
                FileProcessor.NodeFileInfo cNode = node.Value;
                Console.WriteLine("Node name: {0}",  nodeName);
                var instance = new AlwaysOnXeventParser();
                foreach (var xelPath in cNode.AlwaysOnFileList)
                {
                    instance.LoadXevent(xelPath, nodeName);
                }
            }

            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            // parse ErrorLog
            ErrorLogParser errorLogParser = new ErrorLogParser();
            ClusterLogParser clusterLogParser = new ClusterLogParser();
            SystemHealthParser systemHealthParser = new SystemHealthParser();

            foreach (var node in pFileProcess.NodeList)
            {
                string nodeName = node.Key;
                FileProcessor.NodeFileInfo cNode = node.Value;
                Console.WriteLine("Node name: {0}", nodeName);
                foreach (var logPath in cNode.ErrorLogFileList)
                {
                    errorLogParser.ParseLog(logPath, nodeName);
                }
                Console.WriteLine("Parsing Cluster Log:");
                clusterLogParser.ParseLog(cNode.ClusterLogPath, nodeName);

                // parse System Health XEvent
                Console.WriteLine("Parsing System Health XEvents:");
                foreach (var systemXEventFile in cNode.SystemHealthFileList)
                {
                    systemHealthParser.LoadXevents(systemXEventFile, nodeName);
                }

            }


            // determine Failover 
            pReportMgr.AnalyzeReports();

            if (pFileProcess.ShowResult)
            {
                pReportMgr.ShowFailoverReports();
            }

            Console.ReadLine();
            pReportMgr.SaveReportsToJson();
        }

    }


}
