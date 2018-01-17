using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace FailoverDetector
{
    public static class Constants
    {
        public const int DefaultInterval = 3;

        public enum SourceType
        {
            AlwaysOnXevent = 0,
            SystemHealthXevent,
            ErrorLog,
            SystemLog,
            ClusterLog,
        };


        public static readonly Dictionary<string, string> RootCauseMapping
          = new Dictionary<string, string>
        {
           { "Force Failover", "Admin or other process gracefully shuts down SQL Serverhosting the AG primary. In this case, Windows notifies WSFC to initiate a failover and picks one secondary as new primary, if automatic failover is enabled"  },
           { "Shut Down Server", "The Windows Server which hosts the SQL Server that  has the primary replica is shut down or restarted. In this case, Windows notifies WSFC to initiate a failover and picks one secondary as new primary, if automatic failover is enabled" },
           { "Stop Service", "An administrator performs a manual failover through Management Studio or through a direct Transact-SQL statement “ALTER AVAILABILITY GROUP <AG name> FAILOVER” or “ALTER AVAILABILITY GROUP <AG name> FAILOVER WITH DATA_LOSS” on the secondary replica that will become the primary replica." },
           { "SQL Out Of Memeory", ": If no memory has been freed in 2 minutes, sp_server_diagnostics running in SQL Server determines that SQL Server system component is in  error state and notifies failover WSFC to initiate a failover, if automatic failover is enabled. " },
           { "Unresolved Deadlock", "If sp_server_diagnostics detects unresolved deadlock from query processing component, it determines that SQL Server system component is in error state and notifies failover WSFC to initiate a failover, if automatic failover is enabled. " },
           { "Exceed Dump Threshold", "There are more than 100 SQL Server dumps on the primary replica since the last SQL Server restart, and there is at least one dump in the last 10 seconds. In this case, sp_server_diagnostics running in SQL Server determines that SQL Server system component is in error state and notifies failover WSFC to initiate a failover, if automatic failover is enabled. " },
           { "Memory scribbler", "SQL Server has write access violation and the write address is more than 64KB. If there are more than three such memory corruptions since SQL Server started,, sp_server_diagnostics running in SQL Server determines that  SQL Server system component is in  error state and notifies failover WSFC to initiate a failover, if automatic failover is enabled. " },
           { "Sick SpinLock", "After an access violation, a spin-lock is marked as sick if it backs off more than three times, which is the threshold. In this case, sp_server_diagnostics running in SQL Server determines that SQL Server system component is in error state and notifies failover WSFC to initiate a failover, if automatic failover is enabled. " },
           { "Unexpected Crash", "SQL Server service was shut down unexpectedly. In this scenario, SQL Server crashed without error message or exception thrown. Resources host service (rhs.exe) does not detect lease check from SQL Server about availability group lease. This results an AG lease timeout signal to WSFC and WSFC will initiate a failover if automatic failover is enabled." },
           { "Long Dump", "SQL Server is creating a dump file. During the process, threads handle AG lease are frozen and exceed lease timeout. Resources host service (rhs.exe)  can not detect lease check from SQL Server for availability group lease. This results an AG lease timeout signal to WSFC and WSFC will initiate a failover if automatic failover is enabled." },
           { "RHS Stopped", "" },
           { "Cluster Service halted", "Cluster service on primary was halted, resulting in  primary unable to communicate with other nodes in the cluster. Cluster will initiate quorum vote and determine a new primary to fail over to." },
           { "Cluster Node Offline", "When primary node is frozen or loses power, WSFC loses connection from and to the primary. Failover cluster decide to initiate failover and pick a primary from other possible nodes" },
           { "Network interface failure", "Network interface used to communicate between cluster nodes fails. Primary and secondary replicas cannot communicate. WSFC will initiate quorum vote and determine a new primary to fail over to." },
           { "Lost Quorum", "AG resource is brought offline because quorum is lost. This could be due to connectivity issue, but we do not have further evidence to conclude more detail answer." },
           {"Unknown", "We cannot determine exact failover root cause at this case. " },
           {"Unsuccessful Failover", "Failover was initiated by WSFC, but availability group did not failover to any secondary replica successfully. original primary replica become primary role again. Most two common reasons to this behavior are secondary replica lost connection to primary and failover cluster exceed failover threshold in one hour. "},
           {"High I/O", "System wide performance issue causes SQL Server service unable respond to AG lease handler. It is possible that other process or SQL Server has high I/O requests for a long time." },
           {"High CPU utilization", "System wide performance issue causes SQL Server service unable respond to AG lease handler. It is possible that other process or SQL Server is taking 100% CPU resource for a long time." }

        };
    }

    [DataContract]
    public class PartialReport
    {

        [DataContract]
        public class MessageMgr
        {
            // Raw message section
            [DataContract]
            private class RawMessage
            {
                public DateTimeOffset Timestamp { get; }
                [DataMember(Name = "msg")]
                public string Message { get; }

                public RawMessage(DateTimeOffset timestamp, string message)
                {
                    Timestamp = timestamp;
                    Message = message;
                }



                public void Show()
                {
                    Console.WriteLine("{0}{1}", Timestamp, Message);
                }
            }

            [DataContract]
            private class DataSource
            {
                public DataSource(string instanceName)
                {
                    InstanceName = instanceName;
                    MessagList = new List<RawMessage>();
                }

                private string InstanceName { get; }
                [DataMember(Name = "Messages")]
                private List<RawMessage> MessagList { get; }

                public void AddMessage(DateTimeOffset timestamp, string msg)
                {
                    RawMessage pMessage = new RawMessage(timestamp, msg);
                    MessagList.Add(pMessage);
                    MessagList.Sort((msg1, msg2) => DateTimeOffset.Compare(msg1.Timestamp, msg2.Timestamp));
                }

                public void Show()
                {
                    Console.WriteLine("Log recorded at {0}", InstanceName);
                    foreach (RawMessage message in MessagList)
                    {
                        message.Show();
                    }
                }

            }
            [DataContract]
            private class DataSourceSet
            {
                [DataMember(Name = "Set")]
                private Dictionary<string, DataSource> DataSources;
                public Constants.SourceType ResourceType { get; }
                private bool DataEntryFound { set; get; }
                public DataSourceSet(Constants.SourceType type)
                {
                    DataSources = new Dictionary<string, DataSource>();
                    ResourceType = type;
                    DataEntryFound = false;
                }

                protected internal void AddNewMessage(string instance, DateTimeOffset timestamp, string msg)
                {
                    DataSource pDataSource = null;
                    if (!DataSources.ContainsKey(instance))
                    {
                        pDataSource = new DataSource(instance);
                        DataSources[instance] = pDataSource;
                    }
                    else
                    {
                        pDataSource = DataSources[instance];
                    }

                    pDataSource.AddMessage(timestamp, msg);
                    DataEntryFound = true;
                }

                public void Show()
                {
                    if (DataEntryFound)
                    {
                        Console.WriteLine("{0} entries that related with this failover", ResourceType);
                        foreach (var datasourcePairs in DataSources)
                        {
                            DataSource pDataSource = datasourcePairs.Value;
                            pDataSource.Show();
                        }
                    }

                }
            }

            [DataMember(Name = "Data Source")]
            // MessageMgr part
            private Dictionary<Constants.SourceType, DataSourceSet> rawDataEntries;

            private Dictionary<string, List<string>> messageSet;
            public MessageMgr()
            {
                rawDataEntries = new Dictionary<Constants.SourceType, DataSourceSet>();
                foreach (Constants.SourceType type in Enum.GetValues(typeof(Constants.SourceType)))
                {
                    rawDataEntries[type] = new DataSourceSet(type);
                }

                messageSet = new Dictionary<string, List<string>>();
            }

            public void AddNewMessage(Constants.SourceType type, string instance, DateTimeOffset timestamp, string msg)
            {
                // if current instance log is not existed, we create an dataSource

                DataSourceSet sourceSet = rawDataEntries[type];
                sourceSet.AddNewMessage(instance, timestamp, msg);


            }

            public void AddNewMessage(string instance, string msg)
            {
                if (!messageSet.ContainsKey(instance))
                {
                    List<string> tList = new List<string>();
                    tList.Add(msg);
                    messageSet[instance] = tList;
                }else
                {
                    List<string> pList = messageSet[instance];
                    pList.Add(msg);
                }
            }

            public void Show()
            {
                
                foreach (KeyValuePair<string,List<string>> kvp in messageSet)
                {
                    Console.WriteLine("Following log entries are captured from: {0}", kvp.Key);
                    List<string> pList = kvp.Value;
                    foreach(string msg in pList)
                    {
                        Console.WriteLine(msg);
                    }
                }
            }

        }

        // shameless copy from hadrarstatetransition.h
        private enum EHadrArRole
        {
            HadrArRoleResolvingNormal = 0,
            HadrArRoleResolvingPendingFailover,
            HadrArRolePrimaryPending,
            HadrArRolePrimaryNormal,
            HadrArRoleSecondaryNormal,
            HadrArRoleNotAvailable,
            HadrArRoleGlobalPrimary,
            HadrArRoleForwarder,
            HadrArRoleLast,
            HadrArRoleCount = HadrArRoleLast
        }


        [DataMember(Name = "Failover Detected")]
        [JsonProperty(Order = 1)]
        public bool FailoverDetected;


        public string AgName { get; set; }
        // TODO use public for now, change to private and use function to wrap it up. 
        public HashSet<string> MessageSet;

        [DataMember(Name = "Start Time")]
        [JsonProperty(Order = 2)]
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public string AgId { get; set; }

        [DataMember(Name = "Previous Primary")]
        [JsonProperty(Order = 3)]
        public string OldPrimary { get; set; }

        [DataMember(Name = "Current Primary")]
        [JsonProperty(Order = 4)]
        public string NewPrimary { get; set; }

        [DataMember(Name = "Root Cause")]
        [JsonProperty(Order = 5)]
        public string RootCause { get; set; }
        [DataMember(Name = "Verbal Description")]
        [JsonProperty(Order = 5)]
        public string RootCauseDescription { get; set; }
        [DataMember(Name = "Estimate Result")]
        [JsonProperty(Order = 5)]
        public bool EstimateResult { get; set; }
        // Root cause info
        [DataMember(Name = "Lease Time out Found")]
        [JsonProperty(Order = 5)]
        public bool LeaseTimeoutFound { get; set; }

        [DataMember(Name = "Force Failover")]
        [JsonProperty(Order = 5)]
        public bool ForceFailoverFound { get; set; }
        [DataMember(Name = "Unsuccessed Failover")]
        [JsonProperty(Order = 5)]
        public bool UnsuccessedFailover { get; set; }

        public bool SystemUnhealthFound { get; set; }
        public bool ExceedDumpThreshold { get; set; }
        public bool Memorycribbler { get; set; }
        public bool SickSpinLock { get; set; }
        public bool SqlOOM { get; set; }
        public bool UnresolvedDeadlock { get; set; }
        public bool LongIO { get; set; }

        public bool SqlLowMemory { get; set; }

        public UInt32 systemCpuUtilization { get; set; }
        public UInt32 sqlCpuUtilization { get; set; }
        public UInt32 pendingTasksCount { get; set; }
        public UInt32 intervalLongIos { get; set; }

        [DataMember(Name = "Role Transition")]
        [JsonProperty(Order = 9)]
        private Dictionary<string, List<EHadrArRole>> _roleTransition;

        // Resource Tyep name, and raw message list
        [DataMember(Name = "Raw Data Set")]
        [JsonProperty(Order = 10)]
        private MessageMgr pMessageMgr;





        public PartialReport()
        {
            _roleTransition = new Dictionary<string, List<EHadrArRole>>();

            FailoverDetected = false;
            OldPrimary = String.Empty;
            NewPrimary = String.Empty;
            AgId = String.Empty;
            AgName = String.Empty;
            RootCause = "Unknown";
            EstimateResult = false;
            MessageSet = new HashSet<string>();

            // contains raw message, coressponding to MessageSet
            pMessageMgr = new MessageMgr();


        }

        // compare function 
        // to compare if two Partial Reports are equal we make sure all content are same at following field
        //
        //_roleTransition 
        //FailoverDetected
        //OldPrimary 
        //NewPrimary 
        //AgId 
        //AgName 
        public bool Equals(PartialReport other)
        {
            if (! (FailoverDetected == other.FailoverDetected &&
                OldPrimary == other.OldPrimary &&
                NewPrimary == other.NewPrimary &&
                AgId == other.AgId &&
                AgName == other.AgName &&
                MessageSet.SetEquals(other.MessageSet)))
            {
                return false;
            }

            if (_roleTransition.Count != other._roleTransition.Count)
                return false;
            if (!(_roleTransition.Keys.SequenceEqual(other._roleTransition.Keys)))
                return false;

            foreach (var key in _roleTransition.Keys)
            {
                if (!_roleTransition[key].SequenceEqual(other._roleTransition[key]))
                {
                    return false;
                }
            }

            return true;
        }
            // Morethan function compare two partial Report
        // return true if this EndTime is lager than other
        // current Report is older
        public bool MoreThan(PartialReport other)
        {
            return EndTime > other.EndTime;
        }

        public bool MergeReport(PartialReport other)
        {
            if (AgName != other.AgName)
            {
                return false;
            }

            FailoverDetected |= other.FailoverDetected;
            ForceFailoverFound |= other.ForceFailoverFound;
            UnsuccessedFailover |= other.UnsuccessedFailover;
            SystemUnhealthFound |= other.SystemUnhealthFound;
            ExceedDumpThreshold |= other.ExceedDumpThreshold;
            Memorycribbler |= other.Memorycribbler;
            SickSpinLock |= other.SickSpinLock;
            SqlOOM |= other.SqlOOM;
            UnresolvedDeadlock |= other.UnresolvedDeadlock;
            LongIO |= other.LongIO;
            SqlLowMemory |= other.SqlLowMemory;
            EstimateResult |= other.EstimateResult;
            LeaseTimeoutFound |= other.LeaseTimeoutFound;

            StartTime = StartTime < other.StartTime ? StartTime : other.StartTime;

            EndTime = EndTime > other.EndTime ? EndTime : other.EndTime;

            var result = other._roleTransition.Keys.Except(_roleTransition.Keys);
            foreach(var key in result)
            {
                _roleTransition[key] = other._roleTransition[key];
            }
            // message set
            MessageSet.UnionWith(other.MessageSet);

            return true;

        }

        public bool IsEmptyRole( string currentNode)
        {
            if (!_roleTransition.ContainsKey(currentNode) || !_roleTransition[currentNode].Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private EHadrArRole ParseHadrRole(string cRole)
        {
            EHadrArRole mRole = EHadrArRole.HadrArRoleLast;
            switch (cRole)
            {
                case "RESOLVING_NORMAL":
                    mRole = EHadrArRole.HadrArRoleResolvingNormal;
                    break;
                case "RESOLVING_PENDING_FAILOVER":
                    mRole = EHadrArRole.HadrArRoleResolvingPendingFailover;
                    break;
                case "PRIMARY_PENDING":
                    mRole = EHadrArRole.HadrArRolePrimaryPending;
                    break;
                case "PRIMARY_NORMAL":
                    mRole = EHadrArRole.HadrArRolePrimaryNormal;
                    break;
                case "SECONDARY_NORMAL":
                    mRole = EHadrArRole.HadrArRoleSecondaryNormal;
                    break;
                case "NOT_AVAILABLE":
                    mRole = EHadrArRole.HadrArRoleNotAvailable;
                    break;
                case "GLOBAL_PRIMARY":
                    mRole = EHadrArRole.HadrArRoleGlobalPrimary;
                    break;
                case "FORWARDER":
                    mRole = EHadrArRole.HadrArRoleForwarder;
                    break;
                default:
                    break;
            }
            return mRole;
        }
        public void AddRoleTransition(string currentNode, string cRole)
        {

            EHadrArRole mRole = ParseHadrRole(cRole);
            if( IsEmptyRole(currentNode))
            {
                _roleTransition.Add(currentNode, new List<EHadrArRole>());
            }
            _roleTransition[currentNode].Add(mRole);

        }

        // message below should be a full/original message
        public void AddNewMessage(Constants.SourceType type, string instance, DateTimeOffset timestamp, string msg)
        {
            // when we init RawMessageMgr, we make sure all type is initialted
            pMessageMgr.AddNewMessage(type, instance, timestamp, msg);
        }

        public void AddNewMessage(string instance, string msg)
        {
            pMessageMgr.AddNewMessage(instance, msg);
        }

        // helper function that used to show result or debugging.

        public void ShowRoleTransition()
        {
            foreach( KeyValuePair<string, List<EHadrArRole>> kvp in _roleTransition)
            {
                Console.WriteLine("Instance name: {0}", kvp.Key);
                foreach( EHadrArRole aRole in kvp.Value)
                {
                    Console.WriteLine("Current: {0}", aRole.ToString());

                }
            }
            
        }
        
        public void ShowMessageSet()
        {
            Console.WriteLine("{0}This RCA was determined by following message:", Environment.NewLine);
            pMessageMgr.Show();

        }

        // return how many instance for role transition list was found at this report
        public int GetRoleInstanceNumber()
        {
            return _roleTransition.Count;
        }


        // return list of instance names for role transition list was found at this report
        public List<string> GetRoleInstanceNames()
        {
            return _roleTransition.Keys.ToList();
        }

        public void IdentifyRoles()
        {
            //  iterate through roleTransition
            //  find prev Primary is the keyword
            foreach(KeyValuePair<string, List<EHadrArRole>> kvp in _roleTransition)
            {
                string instanceName = kvp.Key;
                List<EHadrArRole> tRoleSet = kvp.Value;

                EHadrArRole initState = tRoleSet.FirstOrDefault();
                EHadrArRole endState = EHadrArRole.HadrArRoleLast;
                bool restarted = false;
                if (initState == EHadrArRole.HadrArRoleNotAvailable)
                {
                    restarted = true;
                    initState = tRoleSet[1];
                }
                endState = tRoleSet.LastOrDefault();

                if(  endState == EHadrArRole.HadrArRolePrimaryNormal)
                {
                    NewPrimary = instanceName;
                }
                if ((initState == EHadrArRole.HadrArRolePrimaryNormal) ||
                    (restarted && OldPrimary==String.Empty))
                {
                    OldPrimary = instanceName;
                }
       
            }
            if (OldPrimary == NewPrimary)
            {
                UnsuccessedFailover = true;
            }
        }
        
        public bool SearchFailoverRole()
        {
            Dictionary<string, List<EHadrArRole>>.ValueCollection vRoleTransition = _roleTransition.Values;
            foreach(List<EHadrArRole> pList in vRoleTransition)
            {
                EHadrArRole prevRole = pList.FirstOrDefault();

                // role transition always is a pair. 
                foreach(EHadrArRole currentRole in pList)
                {
                    if( currentRole == EHadrArRole.HadrArRoleResolvingPendingFailover)
                    {
                        FailoverDetected = true;
                        ForceFailoverFound = true;
                        return true;
                    }
                    if (prevRole.Equals(EHadrArRole.HadrArRolePrimaryPending) && currentRole.Equals(EHadrArRole.HadrArRolePrimaryNormal))
                    {
                        FailoverDetected = true;
                        return true;
                    }
                    prevRole = currentRole;
                }
            }
            return false;
        }

    }

    // singleton class, only one Report manager existed
    // no multi thread at this point. Not Thread safe
    // Data Contract to output as json format
    [DataContract]
    public sealed class ReportMgr
    {
        private static ReportMgr instance = null;

        private ReportMgr()
        {
        }
        public static ReportMgr ReportMgrInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ReportMgr();
                    AgReports = new Dictionary<string, AgReport>();
                }
                return instance;
            }
        }

        // pair <AGName, Report Collection>
        [DataMember(Name = "AG Collection")]
        private static Dictionary<string, AgReport> AgReports;



        // iterator
        public IEnumerable<AgReport> AgReportIterator()
        {
            foreach (var agReport in AgReports)
            {
                yield return agReport.Value;
            }
        }
        public  IEnumerable<KeyValuePair<string, AgReport>> ReportValuePairsIterator()
        {
            foreach (var report in AgReports)
            {
                yield return report;
            }
        }



        public IEnumerable<PartialReport> ReportIterator()
        {
            List<PartialReport> AgIteratorList = new List<PartialReport>();
            // Each AgReport contains a List of Report
            // AgIteratorList above will store iterator for each AgReport
            foreach (var agReport in AgReports)
            {
                foreach (PartialReport report in agReport.Value)
                {
                    AgIteratorList.Add(report);
                }
            }
            AgIteratorList.Sort((rp1, rp2) => DateTimeOffset.Compare(rp1.EndTime, rp2.EndTime));
            foreach (PartialReport report in AgIteratorList)
            {
                yield return report;
            }
        }

        public IEnumerator ReportVisitor()
        {
            ReportEnum iterator = null;
            foreach (var agReport in AgReports)
            {

                if (iterator== null)
                {
                    iterator = new ReportEnum(agReport.Value.Reports);
                }
                else
                {
                    // Add object into a read pointer
                    iterator.MergeMultipleReports(agReport.Value.Reports);
                }
            }
            return iterator;
        }

        public AgReport GetAgReports(string agName)
        {
            if (AgReports.ContainsKey(agName))
            {
                return AgReports[agName];
            }
            else
            {
                return null;
            }
        }

        public AgReport AddNewAgReport(string agName, string serverName)
        {
            if (AgReports.ContainsKey(agName))
            {
                return AgReports[agName];
            }
            else
            {
                // create a new agReport in hashMap
                AgReport tmpAgReport = new AgReport(agName, serverName);
                AgReports[agName] = tmpAgReport;
                return tmpAgReport;
            }
        }

        public void MergeReports()
        {
            foreach(var agReport in AgReports)
            {
                agReport.Value.MergeReports();
            }
        }

        public void AnalyzeReports()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            foreach (AgReport rlMgr in pReportMgr.AgReportIterator())
            {
                rlMgr.AnalyzeReport();
                rlMgr.AnalyzeRootCause();
            }
        }
        public void ShowAgRoleTransition()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            foreach (AgReport rlMgr in pReportMgr.AgReportIterator())
            {
                rlMgr.ShowReportArRoleTransition();
            }
        }

        public void ShowFailoverReports()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            foreach (AgReport rlMgr in pReportMgr.AgReportIterator())
            {
                rlMgr.ShowReport();
            }

        }

        public void SaveReportsToJson()
        {
            ReportMgr pReportMgr = ReportMgrInstance;
            string output = JsonConvert.SerializeObject(pReportMgr);
            string timeFormat = "yyyy-MM-dd-h-mm-ss";
            string outputFile = "result_" + DateTimeOffset.Now.ToString(timeFormat) + ".json";
            string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            outputFile = Path.Combine(rootDirectory, "Result", outputFile);
            using (var stringReader = new StringReader(output))
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(sw) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);

            }

        }

    }


    [DataContract]
    public class AgReport : IEnumerable
    {


        readonly string _serverName;
        // TODO
        // set healthLevel from configuration
        readonly int HealthLevel;

        public IEnumerable<PartialReport> AgReportIterator()
        {
            foreach (var report in Reports)
            {
                yield return report;
            }
        }
        [DataMember(Name = "AG Name")]
        [JsonProperty(Order = 1)]
        public string AgName { get; set; }

        [DataMember(Name = "Reports")]
        [JsonProperty(Order = 2)]

        public List<PartialReport> Reports { get; set; }


        public AgReport(string agName, string instanceName)
        {
            Reports = new List<PartialReport>();
            AgName = agName;
            _serverName = instanceName;
            HealthLevel = 3;
        }

        public AgReport(string agName)
        {
            AgName = agName;
            HealthLevel = 3;
            Reports = new List<PartialReport>();
         
        }

        public PartialReport FGetReport(DateTimeOffset pTimeStamp)
        {
            foreach (var report in Reports)
            {
                if (((pTimeStamp - report.EndTime).TotalMinutes < Constants.DefaultInterval)
                    && ((report.StartTime - pTimeStamp).TotalMinutes < Constants.DefaultInterval))
                {
                    // update time
                    if (pTimeStamp < report.StartTime)
                    {
                        report.StartTime = pTimeStamp;
                    }
                    if (pTimeStamp > report.EndTime)
                    {
                        report.EndTime = pTimeStamp;
                    }
                    return report;
                }
            }

            // no find any overlapped report, so we create a new one
            PartialReport pReport = new PartialReport()
            {
                // update time
                StartTime = pTimeStamp,
                EndTime = pTimeStamp,
            };
            Reports.Add(pReport);

            return pReport;
        }

        // XEvent somehow doesn't sorted by timestamp. 
        // once we have a list of Report, we need to sort them before merge two instance reports
        public void SortReports()
        {
            // just simply sort by Endtime stamp
            Reports.Sort((rp1, rp2) => DateTimeOffset.Compare(rp1.EndTime, rp2.EndTime));
        }

        // I think we can add a bit of tolerance at time to allow two report merge into one
        // #1 if two reports has complementary roles: each report has different role transition
        public void MergeReports()
        {
            SortReports();
            List<PartialReport> tReportList = new List<PartialReport>();
            foreach(PartialReport cReport in Reports)
            {
                PartialReport pReport = tReportList.LastOrDefault();

                // first item
                if (pReport == null)
                {
                    tReportList.Add(cReport);
                }
                else
                {
                    // either there is not role transition or only one instance
                    // existed in this report. We should merge two report if both
                    // report only contain less value.
                    if (pReport.GetRoleInstanceNumber() < 2)
                    {
                        List<string> pInstanceList = pReport.GetRoleInstanceNames();
                        List<string> cInstanceList = cReport.GetRoleInstanceNames();
                        if (!pInstanceList.Intersect(cInstanceList).Any())
                        {
                            // merge pReport with cReport
                            pReport.MergeReport(cReport);
                        }else
                        {
                            tReportList.Add(cReport);
                        }
                    }else
                    {
                        tReportList.Add(cReport);
                    }
                }
            }
            Reports = tReportList;
        }

        public void ShowReportArRoleTransition()
        {
            foreach (PartialReport pReport in Reports)
            {

                pReport.ShowRoleTransition();
                Console.WriteLine();
            }
        }
        public void ShowReportFailoverArRoleTransition()
        {
            foreach (PartialReport pReport in Reports)
            {
                Console.WriteLine("-------------------------");
                Console.WriteLine("A report happended at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.StartTime.ToString());
                // Old Primary
                Console.WriteLine("Old Primary: {0}", pReport.OldPrimary);
                // New Primary
                Console.WriteLine("New Primary: {0}", pReport.NewPrimary);
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Yellow;
                if (pReport.UnsuccessedFailover)
                {
                    Console.WriteLine("Unsuccessful Failover");
                    Console.WriteLine(Constants.RootCauseMapping["Unsuccessful Failover"]);
                }
                Console.ResetColor();

                // Root Cause:
                if (pReport.EstimateResult)
                {
                    Console.WriteLine( "We cannot determine a concrete root cause, This is an estimated result");
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Root Cause: {0}", pReport.RootCause);
                Console.ResetColor();
                Console.WriteLine("Descrption: {0}", pReport.RootCauseDescription);

                Console.WriteLine();
                pReport.ShowRoleTransition();

                pReport.ShowMessageSet();
                if (pReport.EstimateResult)
                {
                    // show CPU state as reference
                    Console.WriteLine("System CPU Utilization: {0}\nSQL CPU Utilization: {1}\nPending Tasks: {2}\nLong Disk I/O wait:{3}", pReport.systemCpuUtilization, pReport.sqlCpuUtilization, pReport.pendingTasksCount, pReport.intervalLongIos);
                }


                Console.ReadLine();
            }
        }

        public void ShowReport()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("AG name: {0}", AgName);
            Console.ForegroundColor = ConsoleColor.White;
            ShowReportFailoverArRoleTransition();
        }

        // before you call this method, you should finish parsing all AlwaysOn Xevent
        // you have a list of partialReport
        // search signs of failover from these report
        public void AnalyzeReport()
        {
            // let me do a little sorting at here
            SortReports();
            List<PartialReport> _mFailoverReport = new List<PartialReport>();
            foreach (PartialReport pReport in Reports)
            {
                pReport.IdentifyRoles();

                if (pReport.MessageSet.Intersect(new HashSet<string>()
                {
                    "19407",
                    "19421",
                    "19422",
                    "19423"
                }).Any() )
                {
                    pReport.LeaseTimeoutFound = true;
                }

                if (pReport.ForceFailoverFound)
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    pReport.FailoverDetected = true;
                    _mFailoverReport.Add(pReport);
                }else 
                if (pReport.LeaseTimeoutFound)
                {
                    pReport.FailoverDetected = true;
                    _mFailoverReport.Add(pReport);
                }else if (pReport.SearchFailoverRole())
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    pReport.FailoverDetected = true;
                    _mFailoverReport.Add(pReport);
                }

                SpecialRecipe(pReport);
            }

            Reports = _mFailoverReport;
        }

        public void AnalyzeRootCause()
        {
            foreach (PartialReport pReport in Reports)
            {
                // search root cause property.
                if (pReport.ForceFailoverFound)
                {
                    pReport.RootCause = "Force Failover";
                }
                else if (pReport.MessageSet.Contains("17147"))
                {
                    pReport.RootCause = "Shut Down Server";

                }
                else if (pReport.MessageSet.Contains("17148"))
                {
                    pReport.RootCause = "Stop Service";
                }

                // search if system components is not health
                // failover level == 3, trigger failover at system component
                // failover level == 4, trigger failover at resource component error
                if (pReport.SystemUnhealthFound)
                {
                    if (HealthLevel >3)
                    {
                        if (pReport.SqlOOM)
                        {
                            pReport.RootCause = "SQL Out Of Memeory";
                        }else if (pReport.UnresolvedDeadlock)
                        {
                            pReport.RootCause = "Unresolved Deadlock";
                        }
                        
                    }else if  (HealthLevel >2)
                    {
                        if (pReport.ExceedDumpThreshold)
                        {
                            pReport.RootCause = "Exceed Dump Threshold";
                        }else if (pReport.Memorycribbler)
                        {
                            pReport.RootCause = "Memory scribbler";
                        }else if (pReport.SickSpinLock)
                        {
                            pReport.RootCause = "Sick SpinLock";
                        }
                    }
                }

                // 1205 indicate cluster AG component offline
                if (pReport.MessageSet.Contains("1205"))
                {
                    if (pReport.MessageSet.Contains("Crash"))
                    {
                        pReport.RootCause = "Unexpected Crash";
                    }else if (pReport.MessageSet.Contains("Dump"))
                    {
                        pReport.RootCause = "Long Dump";
                    }
                }

                if (pReport.MessageSet.Contains("1146"))
                {
                    pReport.RootCause = "RHS Stopped";
                }

                if (pReport.MessageSet.Contains("1177"))
                {
                    if (pReport.MessageSet.Contains("1135"))
                    {
                        if (pReport.MessageSet.Contains("1006"))
                        {
                            pReport.RootCause = "Cluster Service halted";
                        }
                        else
                        {
                            pReport.RootCause = "Cluster Node Offline";
                            pReport.EstimateResult = true;
                        }
                    }

                    if (pReport.MessageSet.Contains("1069"))
                    {
                        pReport.RootCause = "Network interface failure";
                    }
                    else
                    {
                        pReport.RootCause = "Lost Quorum";
                    }
                }


                // at this point we search every condition we may expected.
                // but we still cannot find root cause. we kick in estimation
                if (pReport.RootCause == string.Empty)
                {
                    // TODO
                    // estimate high cpu, low memory or high disk i/o
                    if (pReport.LongIO || pReport.pendingTasksCount > 0)
                    {
                        pReport.RootCause = "High I/O";
                        pReport.EstimateResult = true;
                    }else if (pReport.systemCpuUtilization > 95 || pReport.sqlCpuUtilization > 95)
                    {
                        pReport.RootCause = "High CPU utilization";
                        pReport.EstimateResult = true;
                    }
                }


                // Map root cause to short description
                pReport.RootCauseDescription = Constants.RootCauseMapping[pReport.RootCause];

            }
        }

        // little special recipe for demo
        public void SpecialRecipe(PartialReport pReport)
        {
            DateTimeOffset SpecialTime = new DateTimeOffset(2017,10,23,20,16,45,TimeSpan.Zero);
            if (pReport.StartTime < SpecialTime && pReport.EndTime > SpecialTime)
            {
                pReport.MessageSet.Add("Crash");
            }
        }
        public ReportEnum GetEnumerator()
        {
            return new ReportEnum(Reports);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Reports).GetEnumerator();
        }
    }

    public class ReportEnum : IEnumerator
    {
        private List<PartialReport> _reports;
        private int position = -1;


        object IEnumerator.Current
        {
            get { return Current; }
            
        }

        public ReportEnum(List<PartialReport> other)
        {
            _reports = new List<PartialReport>();
            foreach(var report in other)
            {
                _reports.Add(report);
            }
        }
        public void MergeMultipleReports(List<PartialReport> other)
        {

            _reports.AddRange(other);
            _reports.Sort((rp1, rp2) => DateTimeOffset.Compare(rp1.EndTime, rp2.EndTime));
        }
        public bool MoveNext()
        {
            position++;
            return (position < _reports.Count);
        }

        public void Reset()
        {
            position = -1;
        }
        public PartialReport Current
        {
            get
            {
                try
                {
                    return _reports[position];

                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

    }

}
