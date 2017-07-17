using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    // shameless copy from hadrarstatetransition.h

    public class PartialReport : FailoverReport
    {
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

        Dictionary<string, List<EHadrArRole>> roleTransition;

        public string AgName { get => agName; set => agName = value; }

        public PartialReport(string serverName)
        {
            roleTransition = new Dictionary<string, List<EHadrArRole>>();
            List<EHadrArRole> tempList = new List<EHadrArRole>();
            roleTransition.Add(serverName, tempList);
            failoverDetected = false;
            merged = false;
        }

        public DateTimeOffset StartTime { get => startTime; set => startTime = value; }
        public DateTimeOffset EndTime { get => endTime; set => endTime = value; }
        public bool LeaseTimeoutFound { get => leaseTimeoutFound; set => leaseTimeoutFound = value; }
        public bool ForceFailoverFound { get => forceFailoverFound; set => forceFailoverFound = value; }
        public string AgId { get => agId; set => agId = value; }
        public bool merged { get; private set; }

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

                if( initState == EHadrArRole.HADR_AR_ROLE_SECONDARY_NORMAL && endState == EHadrArRole.HADR_AR_ROLE_PRIMARY_NORMAL)
                {
                    newPrimary = instanceName;
                }
                if (initState == EHadrArRole.HADR_AR_ROLE_PRIMARY_NORMAL && endState == EHadrArRole.HADR_AR_ROLE_SECONDARY_NORMAL)
                {
                    oldPrimary = instanceName;
                }
       
            }
        }
        public bool SearchFailoverRole()
        {
            Dictionary<string, List<EHadrArRole>>.ValueCollection vRoleTransition = roleTransition.Values;
            EHadrArRole prevRole = vRoleTransition.First()[0];
            foreach(EHadrArRole CurrentRole in vRoleTransition.First())
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
            return false;
        }

    }
    public class AgReportMgr
    {
        readonly int DefaultInterval = 10;
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
            if (!m_reports.Any() || ((evt.Timestamp - m_reports.Last().EndTime).TotalMinutes > DefaultInterval))
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
                pReport.ShowRoleTransition();
                Console.WriteLine("A report ends at : {0:MM/dd/yy H:mm:ss zzz} ", pReport.EndTime.ToString());
                Console.WriteLine();
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
            foreach (PartialReport pReport in m_reports)
            {
                pReport.IdentifyRoles();
                if (pReport.ForceFailoverFound)
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    m_failoverReport.Add(pReport);
                }

                // search roleTransition from Primary Pending to Primary Normal
                if (pReport.SearchFailoverRole())
                {
                    // this report is useful, I will push it into Failover Report for future investigation
                    m_failoverReport.Add(pReport);
                }

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
