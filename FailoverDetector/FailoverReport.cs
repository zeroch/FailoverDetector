using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FailoverDetector
{
    public class FailoverReport
    {
        // property
        private DateTimeOffset  failoverTime;
        private string          currentPrimary;
        private string previousPrimary;
        private bool failoverResult;
        private string rootCause;
        public virtual void buildReport()
        {
            // build dataSource
        }

        public virtual void analyzeData()
        {
            // utilize data from container
            // execute search codepath
            // fill data to property

        }
        public virtual void showReport()
        {
            // simple display data from property
        }


    }


    public class PartialReport : FailoverReport
    {
        // shameless copy from hadrarstatetransition.h
        enum EHadrArRole
        {
            HADR_AR_ROLE_RESOLVING_NORMAL = 0,
            HADR_AR_ROLE_RESOLVING_PENDING_FAILOVER,
            HADR_AR_ROLE_PRIMARY_PENDING,
            HADR_AR_ROLE_PRIMARY_NORMAL,
            HADR_AR_ROLE_SECONDARY_NORMAL,
            HADR_AR_ROLE_NOT_AVAILABLE,
            HADR_AR_ROLE_GLOBAL_PRIMARY,
            HADR_AR_ROLE_FORWARDER,
            HADR_AR_ROLE_LAST,
            HADR_AR_ROLE_COUNT = HADR_AR_ROLE_LAST
        }

        string agName;
        string agId;
        DateTimeOffset startTime;
        DateTimeOffset endTime;
        bool leaseTimeoutFound;
        bool forceFailoverFound;
        bool failoverDetected;

        string oldPrimary;
        string newPrimary;

        SystemHealthData m_sysData;

        Dictionary<string, List<EHadrArRole>> roleTransition;

        public string AgName { get => agName; set => agName = value; }

        public PartialReport(string serverName)
        {
            roleTransition = new Dictionary<string, List<EHadrArRole>>();
            List<EHadrArRole> tempList = new List<EHadrArRole>();
            roleTransition.Add(serverName, tempList);
            failoverDetected = false;
            merged = false;
            oldPrimary = "";
            newPrimary = "";
            m_sysData = new SystemHealthData();

        }

        public DateTimeOffset StartTime { get => startTime; set => startTime = value; }
        public DateTimeOffset EndTime { get => endTime; set => endTime = value; }
        public bool LeaseTimeoutFound { get => leaseTimeoutFound; set => leaseTimeoutFound = value; }
        public bool ForceFailoverFound { get => forceFailoverFound; set => forceFailoverFound = value; }
        public string AgId { get => agId; set => agId = value; }
        public bool merged { get; private set; }
        public string OldPrimary { get => oldPrimary; set => oldPrimary = value; }
        public string NewPrimary { get => newPrimary; set => newPrimary = value; }

        public void MergeReport(PartialReport other)
        {
            // merge timestamp
            if (this.StartTime > other.StartTime)
            {
                this.StartTime = other.StartTime;
            }
            if (this.EndTime < other.EndTime)
            {
                this.EndTime = other.EndTime;
            }
            
            // merge boolean property
            this.ForceFailoverFound = (this.ForceFailoverFound | other.ForceFailoverFound);
            this.leaseTimeoutFound = (this.leaseTimeoutFound | other.leaseTimeoutFound);

            // merge another RoleChange set
            // FIXME: this is not a good expression. 
            roleTransition.Add(other.roleTransition.First().Key, other.roleTransition.First().Value);
            merged = true;


        }
        public bool IsEmptyRole() { return roleTransition.First().Value.Any() ? false : true; }
        public void AddRoleTransition(string cRole)
        {
            EHadrArRole m_role = EHadrArRole.HADR_AR_ROLE_LAST;
            switch(cRole)
            {
                case "RESOLVING_NORMAL":
                    m_role = EHadrArRole.HADR_AR_ROLE_RESOLVING_NORMAL;
                    break;
                case "RESOLVING_PENDING_FAILOVER":
                    m_role = EHadrArRole.HADR_AR_ROLE_RESOLVING_PENDING_FAILOVER;
                    break;
                case "PRIMARY_PENDING":
                    m_role = EHadrArRole.HADR_AR_ROLE_PRIMARY_PENDING;
                    break;
                case "PRIMARY_NORMAL":
                    m_role = EHadrArRole.HADR_AR_ROLE_PRIMARY_NORMAL;
                    break;
                case "SECONDARY_NORMAL":
                    m_role = EHadrArRole.HADR_AR_ROLE_SECONDARY_NORMAL;
                    break;
                case "NOT_AVAILABLE":
                    m_role = EHadrArRole.HADR_AR_ROLE_NOT_AVAILABLE;
                    break;
                case "GLOBAL_PRIMARY":
                    m_role = EHadrArRole.HADR_AR_ROLE_GLOBAL_PRIMARY;
                    break;
                case "FORWARDER":
                    m_role = EHadrArRole.HADR_AR_ROLE_FORWARDER;
                    break;
                default:
                    break;
            }

            roleTransition.First().Value.Add(m_role);

        }
        public void ShowRoleTransition()
        {
            foreach( KeyValuePair<string, List<EHadrArRole>> kvp in roleTransition)
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
            if (m_sysData.IsSystemHealth())
            {
                Console.WriteLine("sp_server_diagnostics is in unhealthy state.");
                Console.Write("System Component is in Error: ");
                Console.WriteLine("Too much Dump");
                Console.WriteLine("TotalDumps Request: {0}, Interval Dump Request: {1}", m_sysData.SysComp.TotalDump, m_sysData.SysComp.IntervalDump);
            }
                
        }
        public void IdentifyRoles()
        {
            //  iterate through roleTransition
            //  find prev Primary is the keyword
            foreach(KeyValuePair<string, List<EHadrArRole>> kvp in roleTransition)
            {
                string instanceName = kvp.Key;
                List<EHadrArRole> tRoleSet = kvp.Value;

                EHadrArRole initState = tRoleSet.FirstOrDefault();
                EHadrArRole endState = EHadrArRole.HADR_AR_ROLE_LAST;
                bool restarted = false;
                if (initState == EHadrArRole.HADR_AR_ROLE_NOT_AVAILABLE)
                {
                    restarted = true;
                    initState = tRoleSet[1];
                }
                endState = tRoleSet.LastOrDefault();

                if(  endState == EHadrArRole.HADR_AR_ROLE_PRIMARY_NORMAL)
                {
                    newPrimary = instanceName;
                }
                if (initState == EHadrArRole.HADR_AR_ROLE_PRIMARY_NORMAL)
                {
                    oldPrimary = instanceName;
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
            if ( oldPrimary.Length != 0)
            {
                url = Directory.GetCurrentDirectory();
                url += @"\Data\";
                url += oldPrimary;
                url += @"\";
                url += @"system_health*.xel";
            }
            SystemHealthParser parser = new SystemHealthParser(m_sysData);
            TimeSpan diff = new TimeSpan(0, 5, 0);
            using (QueryableXEventData evts = new QueryableXEventData(url))
            {
                foreach (PublishedEvent evt in evts)
                {
                    if (evt.Timestamp > (StartTime - diff) && evt.Timestamp < (EndTime + diff))
                    {
                        if (evt.Name.ToString() == "sp_server_diagnostics_component_result")
                        {
                            String t_component = evt.Fields["component"].Value.ToString();
                            String t_data = evt.Fields["data"].Value.ToString();
                            switch (t_component)
                            {
                                case "QUERY_PROCESSING":
                                    // fix it later
                                    if (!parser.ParseQPComponent(t_data))
                                    {
                                        //Console.WriteLine("Event: {0}, time:{1} ", evt.Name, evt.Timestamp);
                                    }
                                    break;
                                case "SYSTEM":
                                    // component data should written in side parser, pass by reference
                                    if (parser.ParseSystemComponent(t_data))
                                    {
                                        // mark the time stamp, since inside parser doesn't come with time. 
                                        m_sysData.SysComp.Timestamp = evt.Timestamp;
                                    }
                                    break;
                                case "RESOURCE":
                                    parser.ParseResource(t_data);
                                    break;
                                case "IO_SUBSYSTEM":
                                    parser.ParseIOSubsytem(t_data);
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
            Dictionary<string, List<EHadrArRole>>.ValueCollection vRoleTransition = roleTransition.Values;
            foreach(List<EHadrArRole> pList in vRoleTransition)
            {
                EHadrArRole prevRole = pList.FirstOrDefault();
                foreach(EHadrArRole CurrentRole in pList)
                {
                    if( CurrentRole == EHadrArRole.HADR_AR_ROLE_RESOLVING_PENDING_FAILOVER)
                    {
                        failoverDetected = true;
                        ForceFailoverFound = true;
                    }
                    if (prevRole.Equals(EHadrArRole.HADR_AR_ROLE_PRIMARY_PENDING) && CurrentRole.Equals(EHadrArRole.HADR_AR_ROLE_PRIMARY_NORMAL))
                    {
                        failoverDetected = true;
                        return true;
                    }
                    prevRole = CurrentRole;
                }
            }
            return false;
        }

    }
    public class AgReportMgr
    {
        readonly int DefaultInterval = 5;
        List<PartialReport> m_reports;
        List<PartialReport> m_failoverReport;
        string m_agName;
        string serverName;

        public List<PartialReport> Reports { get => m_reports; set => m_reports = value; }

        public string AgName { get => m_agName; set => m_agName = value; }

        public AgReportMgr(string agName, string instanceName)
        {
            m_reports = new List<PartialReport>();
            m_failoverReport = new List<PartialReport>();
            AgName = agName;
            serverName = instanceName;
        }
        public PartialReport FGetReport(PublishedEvent evt)
        {
            PartialReport pReport;
            if (    !m_reports.Any() 
                || ((evt.Timestamp - m_reports.Last().EndTime).TotalMinutes > DefaultInterval) 
                || ((m_reports.Last().EndTime - evt.Timestamp).TotalMinutes > DefaultInterval))
            {

                pReport = new PartialReport(this.serverName);
                pReport.StartTime = evt.Timestamp;
                pReport.EndTime = evt.Timestamp;
                pReport.AgName = evt.Fields["availability_group_name"].Value.ToString();
                pReport.AgId = evt.Fields["availability_group_id"].Value.ToString();


                m_reports.Add(pReport);
            }
            else
            {
                pReport = m_reports.Last();
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
            m_reports.Sort((rp1, rp2) => DateTimeOffset.Compare(rp1.EndTime, rp2.EndTime));
        }
 
        public void ShowReportArRoleTransition()
        {
            foreach (PartialReport pReport in m_reports)
            {
                Console.WriteLine("A report starts at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.StartTime.ToString());
                pReport.ShowRoleTransition();
                Console.WriteLine("A report ends at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.EndTime.ToString());
                Console.WriteLine();
            }
        }
        public void ShowReportFailoverArRoleTransition()
        {
            foreach (PartialReport pReport in m_failoverReport)
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
            if (m_reports.Any())
            {
                m_reports.Remove(m_reports.Last());
                m_reports.Add(pReport);
            }
        }
        // before you call this method, you should finish parsing all AlwaysOn Xevent
        // you have a list of partialReport
        // search signs of failover from these report
        public void AnalyzeReport()
        {
            // let me do a little sorting at here
            this.SortReports();
            foreach (PartialReport pReport in m_reports)
            {
                pReport.IdentifyRoles();
                if (pReport.ForceFailoverFound)
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    m_failoverReport.Add(pReport);
                }else 
                if (pReport.LeaseTimeoutFound)
                {
                    m_failoverReport.Add(pReport);
                }else 
                // search roleTransition from Primary Pending to Primary Normal
                if (pReport.SearchFailoverRole())
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    m_failoverReport.Add(pReport);
                }
                pReport.ProcessSystemData();

            }
        }

        public void AnalyzeRootCause()
        {
            foreach (PartialReport pReport in m_failoverReport)
            {
                // search root cause property.
                if (pReport.ForceFailoverFound)
                {
                }
            }
        }
    }

}
