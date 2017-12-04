using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;

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


    public class PartialReport
    {
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

        private bool _failoverDetected;

        private SystemHealthData _mSysData;

        private Dictionary<string, List<EHadrArRole>> _roleTransition;







        public class MessageMgr
        {
            // Raw message section
            private class RawMessage
            {
                public DateTimeOffset Timestamp { get; }
                public string Message { get; }

                public RawMessage(DateTimeOffset timestamp, string message)
                {
                    Timestamp = timestamp;
                    Message = message;
                }



                public void Show()
                {
                    Console.WriteLine("{1}", Timestamp, Message);
                }
            }

            private class DataSource
            {
                public DataSource(string instanceName)
                {
                    InstanceName = instanceName;
                    MessagList = new List<RawMessage>();
                }

                private string InstanceName { get; }
                private List<RawMessage> MessagList { get; }

                public void AddMessage(DateTimeOffset timestamp, string msg)
                {
                    RawMessage pMessage = new RawMessage(timestamp, msg);
                    MessagList.Add(pMessage);
                    MessagList.Sort((msg1, msg2) => DateTimeOffset.Compare(msg1.Timestamp, msg2.Timestamp));
                }

                public void Show()
                {
                    Console.WriteLine("Display log entries at Instance: {0}", InstanceName);
                    foreach (RawMessage message in MessagList)
                    {
                        message.Show();
                    }
                }

            }

            private class DataSourceSet
            {
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
                        Console.WriteLine("Display Log Entries for {0}.", ResourceType);
                        foreach (var datasourcePairs in DataSources)
                        {
                            DataSource pDataSource = datasourcePairs.Value;
                            pDataSource.Show();
                        }
                    }

                }
            }


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



        // Resource Tyep name, and raw message list

        private MessageMgr pMessageMgr;

        // message below should be a full/original message
        public void AddNewMessage(Constants.SourceType type, string instance, DateTimeOffset timestamp, string msg)
        {
            // when we init RawMessageMgr, we make sure all type is initialted
            pMessageMgr.AddNewMessage(type, instance, timestamp, msg);
        }


        public string AgName { get; set; }

        public PartialReport()
        {
            _roleTransition = new Dictionary<string, List<EHadrArRole>>();

            _failoverDetected = false;
            OldPrimary = String.Empty;
            NewPrimary = String.Empty;
            AgId = String.Empty;
            AgName = String.Empty;
            RootCause = String.Empty;
            EstimateResult = false;
            MessageSet = new HashSet<string>();

            // contains raw message, coressponding to MessageSet
            pMessageMgr = new MessageMgr();

            // TODO
            // move this sysData into MessageSet
            _mSysData = new SystemHealthData();

        }

        // compare function 
        // to compare if two Partial Reports are equal we make sure all content are same at following field
        //
        //_roleTransition 
        //_failoverDetected
        //OldPrimary 
        //NewPrimary 
        //AgId 
        //AgName 
        public bool Equals(PartialReport other)
        {
            if (! (_failoverDetected == other._failoverDetected &&
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

        // TODO use public for now, change to private and use function to wrap it up. 
        public HashSet<string> MessageSet;
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        // Root cause info
        public bool LeaseTimeoutFound { get; set; }

        public bool ForceFailoverFound { get; set; }

        public string AgId { get; set; }

        public string OldPrimary { get; set; }

        public string NewPrimary { get; set; }

        public string RootCause { get; set; }
        public string RootCauseDescription { get; set; }
        public bool EstimateResult { get; set; }

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
        
        public void ShowSystemData()
        {
            if (_mSysData.IsSystemHealth())
            {
                Console.WriteLine("sp_server_diagnostics is in unhealthy state.");
                Console.Write("System Component is in Error: ");
                Console.WriteLine("Too much Dump");
                Console.WriteLine("TotalDumps Request: {0}, Interval Dump Request: {1}", _mSysData.SysComp.TotalDump, _mSysData.SysComp.IntervalDump);
            }
                
        }

        public void ShowMessageSet()
        {
            Console.WriteLine("Following Error Message was detect for this failover:");
            pMessageMgr.Show();

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

        public void ProcessSystemData()
        {
            // open the system xevent, search sp_server_diagnostics_component_result
            // in the timeline, this is a bit brute force, but we can optimize  later time
            string url = "C:\\Users\\zeche\\Documents\\WorkItems\\POC\\SYS001_0.xel";
            // we should have a prev primary at this point now. 
            // use primary name to determine which .xel file to open
            if ( OldPrimary.Length != 0)
            {
                url = Directory.GetCurrentDirectory();
                url += @"\Data\";
                url += OldPrimary;
                url += @"\";
                url += @"system_health*.xel";
            }
            SystemHealthParser parser = new SystemHealthParser(_mSysData);
            TimeSpan diff = new TimeSpan(0, 5, 0);
            using (QueryableXEventData evts = new QueryableXEventData(url))
            {
                foreach (PublishedEvent evt in evts)
                {
                    if (evt.Timestamp > (StartTime - diff) && evt.Timestamp < (EndTime + diff))
                    {
                        if (evt.Name == "sp_server_diagnostics_component_result")
                        {
                            String tComponent = evt.Fields["component"].Value.ToString();
                            String tData = evt.Fields["data"].Value.ToString();
                            switch (tComponent)
                            {
                                case "QUERY_PROCESSING":
                                    // fix it later
                                    if (!parser.ParseQpComponent(tData))
                                    {
                                        //Console.WriteLine("Event: {0}, time:{1} ", evt.Name, evt.Timestamp);
                                    }
                                    break;
                                case "SYSTEM":
                                    // component data should written in side parser, pass by reference
                                    if (parser.ParseSystemComponent(tData))
                                    {
                                        // mark the time stamp, since inside parser doesn't come with time. 
                                        _mSysData.SysComp.Timestamp = evt.Timestamp;
                                    }
                                    break;
                                case "RESOURCE":
                                    parser.ParseResource(tData);
                                    break;
                                case "IO_SUBSYSTEM":
                                    parser.ParseIoSubsytem(tData);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    }
                }
            }
        
        public bool SearchFailoverRole()
        {
            Dictionary<string, List<EHadrArRole>>.ValueCollection vRoleTransition = _roleTransition.Values;
            foreach(List<EHadrArRole> pList in vRoleTransition)
            {
                EHadrArRole prevRole = pList.FirstOrDefault();
                foreach(EHadrArRole currentRole in pList)
                {
                    if( currentRole == EHadrArRole.HadrArRoleResolvingPendingFailover)
                    {
                        _failoverDetected = true;
                        ForceFailoverFound = true;
                    }
                    if (prevRole.Equals(EHadrArRole.HadrArRolePrimaryPending) && currentRole.Equals(EHadrArRole.HadrArRolePrimaryNormal))
                    {
                        _failoverDetected = true;
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

    }



    public class AgReport : IEnumerable
    {

        readonly List<PartialReport> _mFailoverReport;
        readonly string _serverName;

        public List<PartialReport> Reports { get; set; }

        public IEnumerable<PartialReport> AgReportIterator()
        {
            foreach (var report in Reports)
            {
                yield return report;
            }
        }

        public string AgName { get; set; }

        public AgReport(string agName, string instanceName)
        {
            Reports = new List<PartialReport>();
            _mFailoverReport = new List<PartialReport>();
            AgName = agName;
            _serverName = instanceName;
        }

        public AgReport(string agName)
        {
            AgName = agName;
             Reports = new List<PartialReport>();
            _mFailoverReport = new List<PartialReport>();
         
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
 
        public void ShowReportArRoleTransition()
        {
            foreach (PartialReport pReport in Reports)
            {
                Console.WriteLine("A report starts at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.StartTime.ToString());
                pReport.ShowRoleTransition();
                Console.WriteLine("A report ends at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.EndTime.ToString());
                Console.WriteLine();
            }
        }
        public void ShowReportFailoverArRoleTransition()
        {
            foreach (PartialReport pReport in _mFailoverReport)
            {
                Console.WriteLine("A report starts at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.StartTime.ToString());
                Console.WriteLine("A report ends at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.EndTime.ToString());
                Console.WriteLine();
                // Old Primary
                Console.WriteLine("Primary before Failover: {0}", pReport.OldPrimary);
                // New Primary
                Console.WriteLine("Primary after Failover: {0}", pReport.NewPrimary);
                Console.WriteLine();
                // Root Cause:
                Console.WriteLine("Root Cause: {0}", pReport.RootCause == String.Empty ? "We cannot determine Failover at this case" : pReport.RootCause);
                Console.WriteLine("{0}", pReport.EstimateResult ? "We cannot determine a concrete root cause, This is an estimated result" : "");
                Console.WriteLine("Descrption: ");

                // Lease timeout
                Console.WriteLine("Failover due to AG LeaseTimeout: {0}", pReport.LeaseTimeoutFound);
                // Force failover
                Console.WriteLine("Failover due to Force Failover DDL: {0}", pReport.ForceFailoverFound);

                // Role Transition
                pReport.ShowSystemData();

                Console.WriteLine();
                pReport.ShowRoleTransition();

                pReport.ShowMessageSet();


                Console.ReadLine();
            }
        }

        public void ShowReport()
        {
            Console.WriteLine("AG name: {0}", AgName);
            ShowReportFailoverArRoleTransition();
        }

        // we should always has pReport populate from list
        // but I will check list anyway
        public void UpdateReport(PartialReport pReport)
        {
            if (Reports.Any())
            {
                Reports.Remove(Reports.Last());
                Reports.Add(pReport);
            }
        }
        // before you call this method, you should finish parsing all AlwaysOn Xevent
        // you have a list of partialReport
        // search signs of failover from these report
        public void AnalyzeReport()
        {
            // let me do a little sorting at here
            SortReports();
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
                    _mFailoverReport.Add(pReport);
                }else 
                if (pReport.LeaseTimeoutFound)
                {
                    _mFailoverReport.Add(pReport);
                }else if (pReport.SearchFailoverRole())
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    _mFailoverReport.Add(pReport);
                }
//                pReport.ProcessSystemData();
                SpecialRecipe(pReport);
            }
        }

        public void AnalyzeRootCause()
        {
            foreach (PartialReport pReport in _mFailoverReport)
            {
                // search root cause property.
                if (pReport.ForceFailoverFound)
                {
                    pReport.RootCause = "ForceFailover";
                    pReport.EstimateResult = false;
                }
                else if (pReport.MessageSet.Contains("17147"))
                {
                    pReport.RootCause = "ShutDownServer";
                    pReport.EstimateResult = false;

                }
                else if (pReport.MessageSet.Contains("17148"))
                {
                    pReport.RootCause = "StopService";
                    pReport.EstimateResult = false;

                }


                if (pReport.MessageSet.Contains("1205"))
                {
                    if (pReport.MessageSet.Contains("Crash"))
                    {
                        pReport.RootCause = "Crash";
                        pReport.EstimateResult = false;
                    }else if (pReport.MessageSet.Contains("Dump"))
                    {
                        pReport.RootCause = "Long Dump";
                        pReport.EstimateResult = false;

                    }
                }

                if (pReport.MessageSet.Contains("1135"))
                {
                    if (pReport.MessageSet.Contains("1146"))
                    {
                        pReport.RootCause = "Network Loss";
                        pReport.EstimateResult = true;
                    }
                    else if (pReport.MessageSet.Contains("1177"))
                    {
                        
                        pReport.RootCause = "Lost Quorum";
                        pReport.EstimateResult = false;

                    }

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
            _reports = other;
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
