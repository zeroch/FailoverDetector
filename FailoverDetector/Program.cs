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
            Console.WriteLine();

            // Handle Necessaries Files
            pFileProcess.ProcessDirectory();
            if (!pFileProcess.FoundConfiguration)
            {
                return;
            }
            pFileProcess.ParseConfigurationFile();
            pFileProcess.CopySourceDataFromRemote();
            if (!pFileProcess.ProcessDataDirectory())
            {
                return;
            }
            pFileProcess.ValidateFileCoverage();

            // process AlwaysOn Health 
            foreach ( var node in pFileProcess.NodeList)
            {
                string nodeName = node.Key;
                FileProcessor.NodeFileInfo cNode = node.Value;

                var instance = new AlwaysOnXeventParser();
                foreach (var xelPath in cNode.AlwaysOnFileList)
                {
                    if (File.Exists(xelPath))
                    {
                        instance.LoadXevent(xelPath, nodeName);
                    }
                }
            }

            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            // parse ErrorLog
            ErrorLogParser errorLogParser = new ErrorLogParser();
            ClusterLogParser clusterLogParser = new ClusterLogParser();
            SystemHealthParser systemHealthParser = new SystemHealthParser();


            Console.WriteLine("{0}Start to analyze failover root cause.{0}", Environment.NewLine);
            foreach (var node in pFileProcess.NodeList)
            {
                // Direcotry
                string nodeName = node.Key;
                FileProcessor.NodeFileInfo cNode = node.Value;
                foreach (var logPath in cNode.ErrorLogFileList)
                {
                    if (File.Exists(logPath))
                    {
                        errorLogParser.ParseLog(logPath, nodeName);
                    }
                }
                if (File.Exists(cNode.ClusterLogPath))
                {
                    clusterLogParser.ParseLog(cNode.ClusterLogPath, nodeName);
                }

                // parse System Health XEvent
                foreach (var systemXEventFile in cNode.SystemHealthFileList)
                {
                    if (File.Exists(systemXEventFile))
                    {
                        systemHealthParser.LoadXevents(systemXEventFile, nodeName);
                    }
                }

            }


            // determine Failover 
            pReportMgr.AnalyzeReports();

            if (pFileProcess.ShowResult)
            {
                pReportMgr.ShowFailoverReports();
            }

            Console.WriteLine("{0}Start saving report results.{0}", Environment.NewLine);
            pReportMgr.SaveReportsToJson();

            Console.ReadLine();
        }

    }


}
