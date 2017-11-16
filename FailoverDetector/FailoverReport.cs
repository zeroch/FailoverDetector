using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FailoverDetector
{

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

        public string AgName { get; set; }

        public PartialReport()
        {
            _roleTransition = new Dictionary<string, List<EHadrArRole>>();

            _failoverDetected = false;
            OldPrimary = String.Empty;
            NewPrimary = String.Empty;
            AgId = String.Empty;
            AgName = String.Empty;

            MessageSet = new HashSet<string>();

            // TODO
            // move this sysData into MessageSet
            _mSysData = new SystemHealthData();

        }

        }

        // TODO use public for now, change to private and use function to wrap it up. 
        public HashSet<string> MessageSet;
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public bool LeaseTimeoutFound { get; set; }

        public bool ForceFailoverFound { get; set; }

        public string AgId { get; set; }

        public string OldPrimary { get; set; }

        public string NewPrimary { get; set; }


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
                if (initState == EHadrArRole.HadrArRolePrimaryNormal)
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
        public IEnumerable<AgReport> ReportIterator()
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
        
    }

    public class AgReport
    {
        readonly int _defaultInterval = 5;
        readonly List<PartialReport> _mFailoverReport;
        readonly string _serverName;

        public List<PartialReport> Reports { get; set; }

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
                if (((pTimeStamp - report.EndTime).TotalMinutes < _defaultInterval)
                    && ((report.StartTime - pTimeStamp).TotalMinutes < _defaultInterval))
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
            PartialReport pReport = new PartialReport(_serverName)
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
                // Lease timeout
                if (pReport.LeaseTimeoutFound)
                {
                    Console.WriteLine("Failover due to AG LeaseTimeout: {0}, Error: 19407", pReport.LeaseTimeoutFound);
                    Console.WriteLine("Detail:");
                    Console.WriteLine("Windows Server Failover Cluster did not receive a process event signal from SQL Server hosting availability group {0} within the lease timeout period.", pReport.AgName);
                    Console.WriteLine("Error: 19419, Severity: 16, State: 1.");
                    Console.WriteLine();

                }
                Console.WriteLine("Failover due to AG LeaseTimeout: {0}", pReport.LeaseTimeoutFound);
                // Force failover
                Console.WriteLine("Failover due to Force Failover DDL: {0}", pReport.ForceFailoverFound);
                // Old Primary
                Console.WriteLine("Primary before Failover: {0}", pReport.OldPrimary);
                // New Primary
                Console.WriteLine("Primary after Failover: {0}", pReport.NewPrimary);
                // Role Transition
                pReport.ShowSystemData();

                pReport.ShowRoleTransition();

                Console.WriteLine("A report ends at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.EndTime.ToString());
                Console.WriteLine();
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
                if (pReport.ForceFailoverFound)
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    _mFailoverReport.Add(pReport);
                }else 
                if (pReport.LeaseTimeoutFound)
                {
                    _mFailoverReport.Add(pReport);
                }else 
                // search roleTransition from Primary Pending to Primary Normal
                if (pReport.SearchFailoverRole())
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    _mFailoverReport.Add(pReport);
                }
                pReport.ProcessSystemData();

            }
        }

        public void AnalyzeRootCause()
        {
            foreach (PartialReport pReport in _mFailoverReport)
            {
                // search root cause property.
                if (pReport.ForceFailoverFound)
                {
                }
            }
        }
    }

}
