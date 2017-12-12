using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Text;
using System.Runtime.Serialization.Json;

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
            public Dictionary<string, NodeFileInfo> NodeList { get; set; }
            private readonly string dataDirectory;
            private readonly string resultDirectory;
            private readonly string configureFilePath;

            public FileProcessor()
            {
                NodeList = new Dictionary<string, NodeFileInfo>();


                // some global value, we put at here first
                DefaultMode = false;
                AnalyzeOnly = false;
                ShowResult = false;
                FoundConfiguration = false;

                rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                dataDirectory = rootDirectory + @"\Data\Demo";
                resultDirectory = rootDirectory + @"\Result";
                configureFilePath = rootDirectory + @"\Configuration.json";
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

            public void ProcessDataDirectory(string root)
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
            private void ProcessDirectory(string targetDirectory)
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
                List<string> errorlog = new List<string>();
                List<string> alwaysOnList = new List<string>();
                List<string> systemHealthList = new List<string>();
                // TODO: fix it for windows or linux expression
                string nodeName = targetDirectory.Substring(targetDirectory.LastIndexOf("\\") + 1);
                NodeFileInfo pNode = new NodeFileInfo(nodeName);


                string[] fileEntries = Directory.GetFiles(targetDirectory);

                foreach (string fileEntry in fileEntries)
                {
                    if (fileEntry.Contains("ERRORLOG"))
                    {
                        errorlog.Add(fileEntry);
                    }
                    else if (fileEntry.Contains("AlwaysOn_health"))
                    {
                        alwaysOnList.Add(fileEntry);
                    }
                    else if (fileEntry.Contains("system_health"))
                    {
                        systemHealthList.Add(fileEntry);

                    }
                    else if (fileEntry.Contains("cluster.log"))
                    {
                        pNode.SetClusterLog(fileEntry);
                    }
                }


                pNode.SetAlwaysOnFile(alwaysOnList);
                pNode.SetErrorLogFile(errorlog);
                pNode.SetSystemHealthFile(systemHealthList);
                NodeList[nodeName] = pNode;
            }

            // use use internal Path to validate Data folder
            public void ProcessDirectory()
            {
                // go through data directory to scan all applicatable log files
                ProcessDataDirectory(dataDirectory);
                try
                {
                    // if we doesn't find configuration file, we will return failed. at this moment. Which means parse configuration alwasy after ProcessDirectory called and passed. 
                    if (!File.Exists(configureFilePath))
                    {
                        Console.WriteLine("Failed to locate configuration file.");
                    }
                    else
                    {
                        FoundConfiguration = true;
                    }

                    // Create Result folder if it is not existed
                    if (!Directory.Exists(resultDirectory))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(resultDirectory);
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }

            }

            public class NodeFileInfo
            {
                public List<string> AlwaysOnFileList { get; set; }
                public bool FoundAlwaysOnFile { get; set; }
                public List<string> SystemHealthFileList { get; set; }
                public bool FoundSystemHealthFile { get; set; }
                public List<String> ErrorLogFileList { get; set; }
                public bool FoundErrologLogFile { get; set; }
                public string NodeName { get; set; }
                public string ClusterLogPath { get; set; }
                public bool FoundClusterLogFile { get; set; }

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
                           AlwaysOnFileList.Count == obj.AlwaysOnFileList.Count &&
                           AlwaysOnFileList.SequenceEqual(obj.AlwaysOnFileList) &&
                           SystemHealthFileList.Count == obj.SystemHealthFileList.Count &&
                           SystemHealthFileList.SequenceEqual(obj.SystemHealthFileList) &&
                           ErrorLogFileList.Count == obj.ErrorLogFileList.Count &&
                           ErrorLogFileList.SequenceEqual(obj.ErrorLogFileList);
                }

                public void SetAlwaysOnFile(List<string> fileList)
                {
                    AlwaysOnFileList = fileList;
                    FoundAlwaysOnFile = AlwaysOnFileList.Any();
                }

                public void SetSystemHealthFile(List<string> fileList)
                {
                    SystemHealthFileList = fileList;
                    FoundSystemHealthFile = SystemHealthFileList.Any();
                }

                public void SetErrorLogFile(List<string> fileList)
                {
                    ErrorLogFileList = fileList;
                    FoundErrologLogFile = ErrorLogFileList.Any();
                }

                public void SetClusterLog(string path)
                {
                    ClusterLogPath = path;
                    FoundClusterLogFile = true;
                }

            }


            public bool DefaultMode { set; get; } // default mode is copy file and run analyze
            public bool AnalyzeOnly { set; get; } // doesn't run copy file but analyze data directly
            public bool ShowResult { set; get; } // show Result at the end
            public bool FoundConfiguration { set; get; } // Configuration is located
            public Configuration ConfigInfo { set; get; }

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

            // we should located Configuration file before this call
            public bool ParseConfigurationFile()
            {
                if (!FoundConfiguration)
                {
                    Console.WriteLine("Configuration File is not located at {0}", configureFilePath);
                    return false;
                }

                using (StreamReader reader = new StreamReader(configureFilePath))
                {
                    string json = reader.ReadToEnd();
                    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    ConfigInfo = new Configuration();
                    var res = new DataContractJsonSerializer(typeof(Configuration));
                    ConfigInfo = (Configuration) res.ReadObject(ms);

                    ms.Close();
                }

                return true;
            }

            /*  1. Configuration File should processed
     2. Data Directory should processed

     result: provide vebal information about log data mismatch
             between user addressed at configuration file and 
             tool scanned from Data folder. 
  */
            public void ValidateFileCoverage()
            {

                if (!FoundConfiguration)
                {
                    return;
                }


                // We need to validate /data dir if there are data logs existed

                foreach (MetaAgInfo agInfo in ConfigInfo.AgInfo)
                {
                    Console.WriteLine("Validating log provided for AG: {0}", agInfo.Name);
                    foreach (string instance in agInfo.InstanceName)
                    {
                        if (!NodeList.ContainsKey(instance))
                        {
                            Console.WriteLine(
                                "All data about instance: {0} is missing. Please check files that you provided.",
                                instance);
                            continue;
                        }
                        // instance folder is existed. Check each log now. 
                        FileProcessor.NodeFileInfo pInstanceFileInfo = NodeList[instance];
                        Console.WriteLine("For Instance: {0}.", instance);

                        if (pInstanceFileInfo.FoundAlwaysOnFile && pInstanceFileInfo.FoundErrologLogFile &&
                            pInstanceFileInfo.FoundClusterLogFile && pInstanceFileInfo.FoundSystemHealthFile)
                        {
                            Console.WriteLine("All data is ready.");
                        }
                        else
                        {
                            if (!pInstanceFileInfo.FoundAlwaysOnFile)
                            {
                                Console.WriteLine(
                                    "AlwaysOn XEvent Data is not existed. We may not be able to detect failover at all");
                            }
                            if (!pInstanceFileInfo.FoundErrologLogFile)
                            {
                                Console.WriteLine(
                                    "For Instance: {0}. ErrorLog Data is not existed. We may not be able to detect some root cause.");
                            }
                            if (!pInstanceFileInfo.FoundClusterLogFile)
                            {
                                Console.WriteLine(
                                    "For Instance: {0}. Cluster log Data is not existed. We may not be able to detect some root cause.");
                            }
                        }

                    }
                }
            }


            // Copy Data direcotry from remote source path
            // Note: Configuration File should processed
            public void CopySourceDataFromRemote()
             {
                // not default mode then we only run analyze, skip copying data
                if (!DefaultMode)
                {
                    return;
                }
                // check remote directory if valid
                string sourcePath = ConfigInfo.SourcePath;
                if (!Directory.Exists(sourcePath))
                {
                    Console.WriteLine("Data Source Path: {0} at configuration file is invalid, Please check your configuration or Data Source Path.");
                    Console.WriteLine("We won't be able to copy data logs from Data Source Path: {0}. We will use data at current workspace for continue. Y/N");
                    // let's do yes
                    //Console.ReadLine();

                }
                else
                {
                    string targetPath = dataDirectory;
                    if (!Directory.Exists(targetPath))
                    {
                        System.IO.Directory.CreateDirectory(targetPath);
                    }
                    DirectoryCopy(sourcePath, targetPath);

                }
                
            }
            public void DirectoryCopy(string sourceDirPath, string destDirPath)
            {
                DirectoryInfo dir = new DirectoryInfo(sourceDirPath);
                DirectoryInfo[] subDirs = null;
                FileInfo[] files = null;

                if (!dir.Exists)
                {
                    Console.WriteLine("Source directory does not exist or could not be found: {0}", sourceDirPath);
                    return;
                }

                // Start copy current dir
                // create dir at destination
                if (!Directory.Exists(destDirPath))
                {
                    Directory.CreateDirectory(destDirPath);
                }
                // get Files under current dir
                try
                {
                    files = dir.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        string tempPath = Path.Combine(destDirPath, file.Name);
                        // overwrite existed file
                        file.CopyTo(tempPath, true);
                    }
                }
                catch (DirectoryNotFoundException e)
                {

                    Console.WriteLine(e.Message);
                    return;
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                // get sub directory
                try
                {
                    subDirs = dir.GetDirectories();
                }
                catch (DirectoryNotFoundException e)
                {

                    Console.WriteLine(e.Message);
                    return;
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
                // copy Subdirectories to new location
                foreach (DirectoryInfo subdir in subDirs)
                {
                    string tempPath = Path.Combine(destDirPath, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath);
                }

            }


        }




        [DataContract]
        public class Configuration
        {
            [DataMember(Name = "Data Source Path")]
            public string SourcePath { get; set; }
            [DataMember(Name = "AG")]
            public List<MetaAgInfo> AgInfo { get; set; }

            public Configuration() { }
            public override bool Equals(object obj)
            {
                if (!(obj is Configuration other))
                    return false;
                return this.SourcePath == other.SourcePath &&
                       this.AgInfo.SequenceEqual(other.AgInfo);
            }
        }
        
        [DataContract]
        public class MetaAgInfo
        {
            public MetaAgInfo()
            {
            }

            public MetaAgInfo(string agName)
            {
                Name = agName;
            }
            [DataMember(Name = "AG Name")]
            public string Name { get; set; }
            [DataMember(Name = "Instances")]
            public List<string> InstanceName { get; set; }

            public override bool Equals(object obj)
            {

                if (!(obj is MetaAgInfo other))
                    return false;
                return this.Name == other.Name &&
                       this.InstanceName.SequenceEqual(other.InstanceName);

            }

            public override int GetHashCode()
            {
                return 0;
            }
        }


    }


}
