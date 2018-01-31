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
            SystemLogParser systemLogParser = new SystemLogParser();

            Console.WriteLine("{0}Disclaimer: All DateTime information represented in UTC.{0}", Environment.NewLine);

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

                // after ErrorLog, we should know Timezone info from errorlogParser
                // system log knows nothing about time zone so we have to borrow it from errorlog
                systemLogParser.SetUTCCorrection(errorLogParser.FGetUTCTimeZone());
                if (File.Exists(cNode.SystemLogPath))
                {
                    systemLogParser.ParseLog(cNode.SystemLogPath, nodeName);
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

                // reset logParser Timezone incase two nodes locate at different timezone


            }

            pReportMgr.MergeReports();
            // determine Failover 
            pReportMgr.AnalyzeReports();

            if (pFileProcess.ShowResult)
            {
                pReportMgr.ShowFailoverReports();
            }

            Console.WriteLine("{0}Saving results to JSON File.{0}", Environment.NewLine);
            pReportMgr.SaveReportsToJson();
            Console.WriteLine("Complete analysis. Please type enter to return.");
            Console.ReadLine();
        }

    }


}
