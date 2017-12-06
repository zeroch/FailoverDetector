using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FailoverDetector
{
    namespace Utils
    {

        public abstract class MessageExpression
        {
            protected Regex _Regex;

            protected readonly Regex RxStringInQuote = new Regex(@"\'\w+\'");
            protected readonly Regex RxFirstSentence = new Regex(@"^([^.]*)\.");

            protected MessageExpression(string pRegexPattern)
            {
                _Regex = new Regex(pRegexPattern);
            }
            protected MessageExpression() { }

            public bool IsMatch(string msg)
            {
                return _Regex.IsMatch(msg);
            }
            public abstract void HandleOnceMatch(string msg, PartialReport pReport);

        }

        // match stop sqlservice and handle method
        public class StopSqlServiceExpression : MessageExpression
        {

            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("17148");
            }

            public StopSqlServiceExpression()
            {
                _Regex = new Regex(
                    @"SQL Server is terminating in response to a 'stop' request from Service Control Manager");

            }
        }

        // match shutdown server and handle method
        public class ShutdownServerExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("17147");
            }

            public ShutdownServerExpression()
            {
                _Regex = new Regex(@"SQL Server is terminating because of a system shutdown");
            }
        }

        // match State transition and handle
        public class StateTransitionExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report

                // capture 'ag_name', 'prev_state'  and 'current_state'
                if(RxStringInQuote.IsMatch(msg))
                {
                    // in this case, matches must equels to 3
                    MatchCollection mc = RxStringInQuote.Matches((msg));
                    if (mc.Count != 3)
                        return;
                    pReport.AgName = mc[0].Value;
                    // TODO 
                    // this is AG status ?
                    //pReport.AddRoleTransition( mc[1].Value);
                    //pReport.AddRoleTransition(mc[2].Value);
                }
                
            }

            public StateTransitionExpression()
            {
                _Regex = new Regex(@"The state of the local availability replica in availability group");
            }
        }

        // match Lease Expired and handle
        public class LeaseExpiredExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("19407");
            }

            public LeaseExpiredExpression()
            {
                _Regex = new Regex(
                    @"(The lease between availability group)(.*)(and the Windows Server Failover Cluster has expired)");
            }
        }

        // Match Lease Timeout
        public class LeaseTimeoutExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("19421");
            }

            public LeaseTimeoutExpression()
            {
                _Regex = new Regex(
                    @"(Windows Server Failover Cluster did not receive a process event signal from SQL Server hosting availability group)(.*)(within the lease timeout period.)");
            }
        }
        // Match Lease Renew Failed.
        public class LeaseRenewFailedExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("19422");
            }

            public LeaseRenewFailedExpression()
            {
                _Regex = new Regex(
                    @"(The renewal of the lease between availability group)(.*)(and the Windows Server Failover Cluster failed)");
            }
        }
        // Match LeaseFailedToSleep
        public class LeaseFailedToSleepExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("19423");
            }

            public LeaseFailedToSleepExpression()
            {
                _Regex = new Regex(
                    @"(The lease of availability group)(.*)(lease is no longer valid to start the lease renewal process)");
            }
        }
        public class GenerateDumpExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("Dump");
            }

            public GenerateDumpExpression()
            {
                _Regex = new Regex(@"(BEGIN STACK DUMP)");
            }
        }

        //  cluster log 1006
        public class ClusterHaltExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("1006");
            }

            public ClusterHaltExpression()
            {
                _Regex = new Regex(@"Cluster service was halted due to incomplete connectivity with other cluster nodes");
            }
        }
        // cluster log 1069
        public class ResourceFailedExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("1069");
            }

            public ResourceFailedExpression()
            {
                _Regex = new Regex(@"Cluster resource(.*)in clustered service or application(.*)failed");
            }
        }
        // Cluster log Node Offline, 1135
        public class NodeOfflineExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("1135");
            }

            public NodeOfflineExpression()
            {
                _Regex = new Regex(@"(Cluster node)(.*)(was removed from the active failover cluster membership)");
            }
        }

        // cluster log 1177
        public class LostQuorumExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("1177");
            }

            public LostQuorumExpression()
            {
                _Regex = new Regex(@"The Cluster service is shutting down because quorum was lost");
            }
        }

        // cluster log 1205
        public class ClusterOfflineExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("1205");
            }

            public ClusterOfflineExpression()
            {
                _Regex = new Regex(@"The Cluster service failed to bring clustered role(.*)completely online or offline");
            }
        }
        public class FailoverExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("Failover");
            }

            public FailoverExpression()
            {
                _Regex = new Regex(@"The Cluster service is attempting to fail over the clustered role(.*)from node(.*)to node (.*)");
            }
        }


        // RHS terminated
        public class RhsTerminatedExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg, PartialReport pReport)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                pReport.MessageSet.Add("1146");
            }

            public RhsTerminatedExpression()
            {
                _Regex = new Regex(@"The cluster Resource Hosting Subsystem \(RHS\) process was terminated and will be restarted");
            }
        }

        public class FileProcessor
        {
            private string rootDirectory;
            public Dictionary<string,NodeFileInfo> NodeList { get; set; }


            public FileProcessor()
            {
                rootDirectory = String.Empty;
                NodeList = new Dictionary<string, NodeFileInfo>();
            }
            public FileProcessor(string dirRootDirectory)
            {
                rootDirectory = dirRootDirectory;
                NodeList = new Dictionary<string, NodeFileInfo>();
            }

            public bool Equals(FileProcessor other)
            {
                if (NodeList.Count != other.NodeList.Count)
                    return false;
                if (!(NodeList.Keys.SequenceEqual(other.NodeList.Keys)))
                    return false;

                foreach (var key in NodeList.Keys)
                {
                    if (!NodeList[key].Equals(other.NodeList[key]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void RootDirectory(string root)
            {
                if (File.Exists(root))
                {
                    // This path is a file
                    Console.WriteLine("{0} is not a File not a valid directory", root);
                }
                else if (Directory.Exists(root))
                {
                    // This path is a directory
                    ProcessDirectory(root);
                }
                else
                {
                    Console.WriteLine("{0} is not a valid file or directory.", root);
                }
            }

            // Process all files in the directory passed in, recurse on any directories 
            // that are found, and process the files they contain.
            public void ProcessDirectory(string targetDirectory)
            {


                // Recurse into subdirectories of this directory.
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
                foreach (string subdirectory in subdirectoryEntries)
                {
                    ProcessNodeDirectory(subdirectory);
                }

                // Process the list of files found in the directory.
                // it should be ClusterLog, 
                string[] fileEntries = Directory.GetFiles(targetDirectory);
                foreach (string nodeName in NodeList.Keys)
                {
                    foreach (string fileEntry in fileEntries)
                    {
                        string clusterFileName = nodeName + "_cluster.log";
                        if (fileEntry.Contains(clusterFileName))
                        {
                            NodeList[nodeName].ClusterLogPath = fileEntry;
                        }
                    }
                }

            }

            public void ProcessNodeDirectory(string targetDirectory)
            {
                List<string> Errorlog = new List<string>();
                List<string> AlwaysOnList = new List<string>();
                List<string> SystemHealthList = new List<string>();

                string[] fileEntries = Directory.GetFiles(targetDirectory);

                foreach (string fileEntry in fileEntries)
                {
                    if (fileEntry.Contains("ERRORLOG"))
                    {
                        Errorlog.Add(fileEntry);
                    }else if (fileEntry.Contains("AlwaysOn_health"))
                    {
                        AlwaysOnList.Add(fileEntry);
                    }else if (fileEntry.Contains("system_health"))
                    {
                        SystemHealthList.Add(fileEntry);

                    }
                }

                // TODO: fix it for windows or linux expression
                string nodeName = targetDirectory.Substring(targetDirectory.LastIndexOf("\\") + 1);
                NodeFileInfo pNode = new NodeFileInfo(nodeName);
                pNode.SetAlwaysOnFile(AlwaysOnList);
                pNode.SetErrorLogFile(Errorlog);
                pNode.SetSystemHealthFile(SystemHealthList);
                NodeList[nodeName] = pNode;
            }

            public class NodeFileInfo
            {
                public List<string> AlwaysOnFileList {  get; set; }
                public List<string> SystemHealthFileList { get; set; }
                public List<String> ErrorLogFileList { get; set; }
                public string NodeName { get; set; }
                public string ClusterLogPath { get; set; }

                public NodeFileInfo(string name)
                {
                    NodeName = name;
                    ClusterLogPath = String.Empty;
                    AlwaysOnFileList = new List<string>();
                    SystemHealthFileList = new List<string>();
                    ErrorLogFileList = new List<string>();
                }

                public bool Equals(NodeFileInfo obj)
                {
                    return NodeName.Equals(obj.NodeName) &&
                        ClusterLogPath.Equals(obj.ClusterLogPath) &&
                           AlwaysOnFileList.Count == obj.AlwaysOnFileList.Count && AlwaysOnFileList.SequenceEqual(obj.AlwaysOnFileList) &&
                           SystemHealthFileList.Count == obj.SystemHealthFileList.Count && SystemHealthFileList.SequenceEqual(obj.SystemHealthFileList) &&
                           ErrorLogFileList.Count == obj.ErrorLogFileList.Count && ErrorLogFileList.SequenceEqual(obj.ErrorLogFileList);
                }

                public void SetAlwaysOnFile(List<string> fileList)
                {
                    AlwaysOnFileList = fileList;
                }

                public void SetSystemHealthFile(List<string> fileList)
                {
                    SystemHealthFileList = fileList;
                }

                public void SetErrorLogFile(List<string> fileList)
                {
                    ErrorLogFileList = fileList;
                }

                public void SetClusterLog(string path)
                {
                    ClusterLogPath = path;
                }

            }


            public bool DefaultMode { set; get; }   // default mode is copy file and run analyze
            public bool AnalyzeOnly { set; get; }   // doesn't run copy file but analyze data directly
            public bool ShowResult { set; get; }   // show Result at the end
            public bool ProcessParameter(string[] args)
            {
                int argsNumber = args.Length;

                if (argsNumber == 0)
                {
                    DefaultMode = true;
                }
                else
                {
                    foreach (string s in args)
                    {
                        switch (s)
                        {
                            case @"--Analyze":
                                AnalyzeOnly = true;
                                break;
                            case "--Show":
                                ShowResult = true;
                                break;
                            default:
                                Console.WriteLine("{0} is an invalide parameter.", s);
                                Console.WriteLine("Please check valid Parameter input:");
                                Console.WriteLine("--Analyze");
                                Console.WriteLine("--Show");
                                return false;
                        }
                    }
                }
                return true;
            }
        }




    }


}
