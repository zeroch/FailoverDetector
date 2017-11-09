using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FailoverDetector
{
    public class FailoverReport
    {
        // property
        private DateTimeOffset  _failoverTime;
        private string          _currentPrimary;
        private string _previousPrimary;
        private bool _failoverResult;
        private string _rootCause;
        public virtual void BuildReport()
        {
            // build dataSource
        }

        public virtual void AnalyzeData()
        {
            // utilize data from container
            // execute search codepath
            // fill data to property

        }
        public virtual void ShowReport()
        {
            // simple display data from property
        }


    }


    public class PartialReport : FailoverReport
    {
        // shameless copy from hadrarstatetransition.h
        enum EHadrArRole
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

        bool _failoverDetected;

        readonly SystemHealthData _mSysData;

        readonly Dictionary<string, List<EHadrArRole>> _roleTransition;

        public string AgName { get; set; }

        public PartialReport(string serverName)
        {
            _roleTransition = new Dictionary<string, List<EHadrArRole>>();
            List<EHadrArRole> tempList = new List<EHadrArRole>();
            _roleTransition.Add(serverName, tempList);
            _failoverDetected = false;
            Merged = false;
            OldPrimary = "";
            NewPrimary = "";
            _mSysData = new SystemHealthData();

        }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public bool LeaseTimeoutFound { get; set; }

        public bool ForceFailoverFound { get; set; }

        public string AgId { get; set; }

        public bool Merged { get; private set; }
        public string OldPrimary { get; set; }

        public string NewPrimary { get; set; }

        public void MergeReport(PartialReport other)
        {
            // merge timestamp
            if (StartTime > other.StartTime)
            {
                StartTime = other.StartTime;
            }
            if (EndTime < other.EndTime)
            {
                EndTime = other.EndTime;
            }
            
            // merge boolean property
            ForceFailoverFound = (ForceFailoverFound | other.ForceFailoverFound);
            LeaseTimeoutFound = (LeaseTimeoutFound | other.LeaseTimeoutFound);

            // merge another RoleChange set
            // FIXME: this is not a good expression. 
            _roleTransition.Add(other._roleTransition.First().Key, other._roleTransition.First().Value);
            Merged = true;


        }
        public bool IsEmptyRole()
        {
            return !_roleTransition.First().Value.Any();
        }

        public void AddRoleTransition(string cRole)
        {
            EHadrArRole mRole = EHadrArRole.HadrArRoleLast;
            switch(cRole)
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

            _roleTransition.First().Value.Add(mRole);

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
    public class AgReportMgr
    {
        readonly int _defaultInterval = 5;
        readonly List<PartialReport> _mFailoverReport;
        readonly string _serverName;

        public List<PartialReport> Reports { get; set; }

        public string AgName { get; set; }

        public AgReportMgr(string agName, string instanceName)
        {
            Reports = new List<PartialReport>();
            _mFailoverReport = new List<PartialReport>();
            AgName = agName;
            _serverName = instanceName;
        }
        public PartialReport FGetReport(PublishedEvent evt)
        {
            PartialReport pReport;
            if (    !Reports.Any() 
                || ((evt.Timestamp - Reports.Last().EndTime).TotalMinutes > _defaultInterval) 
                || ((Reports.Last().EndTime - evt.Timestamp).TotalMinutes > _defaultInterval))
            {

                pReport = new PartialReport(_serverName)
                {
                    StartTime = evt.Timestamp,
                    EndTime = evt.Timestamp,
                    AgName = evt.Fields["availability_group_name"].Value.ToString(),
                    AgId = evt.Fields["availability_group_id"].Value.ToString()
                };


                Reports.Add(pReport);
            }
            else
            {
                pReport = Reports.Last();
                pReport.EndTime = evt.Timestamp;
                pReport.AgName = evt.Fields["availability_group_name"].Value.ToString();
                pReport.AgId = evt.Fields["availability_group_id"].Value.ToString();
            }
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
