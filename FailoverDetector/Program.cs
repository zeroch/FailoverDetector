﻿using System;
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
                    errorLogParser.ParseLog(logPath, nodeName);
                }
                Console.WriteLine("Parsing Cluster Log:");
                clusterLogParser.ParseLog(cNode.ClusterLogPath, nodeName);
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
