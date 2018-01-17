﻿using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;

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
        }

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

            public MessageMgr()
            {
                rawDataEntries = new Dictionary<Constants.SourceType, DataSourceSet>();
                foreach (Constants.SourceType type in Enum.GetValues(typeof(Constants.SourceType)))
                {
                    rawDataEntries[type] = new DataSourceSet(type);
                }
            }

            public void AddNewMessage(Constants.SourceType type, string instance, DateTimeOffset timestamp, string msg)
            {
                // if current instance log is not existed, we create an dataSource

                DataSourceSet sourceSet = rawDataEntries[type];
                sourceSet.AddNewMessage(instance, timestamp, msg);


            }

            public void Show()
            {
                Console.WriteLine("Following log entries are related with Failover.");
                foreach (KeyValuePair<Constants.SourceType, DataSourceSet> dataSourceSet in rawDataEntries)
                {
                    dataSourceSet.Value.Show();
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

        public bool SystemUnhealthFound { get; set; }
        public bool ExceedDumpThreshold { get; set; }
        public bool Memorycribbler { get; set; }
        public bool SickSpinLock { get; set; }
        public bool SqlOOM { get; set; }
        public bool UnresolvedDeadlock { get; set; }
        public bool LongIO { get; set; }

        public bool SqlLowMemory { get; set; }



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
            RootCause = String.Empty;
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
            Console.WriteLine("This RCA was determined by following message:");
            //pMessageMgr.Show();

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
                        }
                    }
                }
            }
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

                // Root Cause:
                if (pReport.EstimateResult)
                {
                    Console.WriteLine("{0}", pReport.EstimateResult ? "We cannot determine a concrete root cause, This is an estimated result" : "");
                }
                Console.WriteLine("Root Cause: {0}", pReport.RootCause == String.Empty ? "We cannot determine Failover at this case" : pReport.RootCause);

                Console.WriteLine("Descrption: {0}", pReport.RootCauseDescription);

                Console.WriteLine();
                pReport.ShowRoleTransition();

                pReport.ShowMessageSet();


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
//                pReport.ProcessSystemData();
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
                    pReport.RootCause = "ForceFailover";
                }
                else if (pReport.MessageSet.Contains("17147"))
                {
                    pReport.RootCause = "ShutDownServer";

                }
                else if (pReport.MessageSet.Contains("17148"))
                {
                    pReport.RootCause = "StopService";
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
                        pReport.RootCause = "Crash";
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
                }

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
