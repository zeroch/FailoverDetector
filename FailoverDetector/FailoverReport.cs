using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FailoverDetector
{
    class FailoverReport
    {
        // property
        private DateTimeOffset  failoverTime;
        private string          currentPrimary;
        private string previousPrimary;
        private bool failoverResult;
        private string rootCause;
        private IList<XEventContainer> dataSourceList;

        public void addDataSource(XEventContainer dataContainer)
        {
            dataSourceList.Add(dataContainer);
        }
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

    class PartialReport : FailoverReport
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
        List<EHadrArRole> roleTransition;

        public string AgName { get => agName; set => agName = value; }



        public DateTimeOffset StartTime { get => startTime; set => startTime = value; }
        public DateTimeOffset EndTime { get => endTime; set => endTime = value; }
        public bool LeaseTimeoutFound { get => leaseTimeoutFound; set => leaseTimeoutFound = value; }
        public bool ForceFailoverFound { get => forceFailoverFound; set => forceFailoverFound = value; }
        public string AgId { get => agId; set => agId = value; }

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

            roleTransition.Add(m_role);

        }
    }

    class AutoFailoverReport : FailoverReport
    {

    }
}
