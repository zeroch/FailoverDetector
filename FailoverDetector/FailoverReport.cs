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

        public string AgName { get => agName; set => agName = value; }



        public DateTimeOffset StartTime { get => startTime; set => startTime = value; }
        public DateTimeOffset EndTime { get => endTime; set => endTime = value; }
        public bool LeaseTimeoutFound { get => leaseTimeoutFound; set => leaseTimeoutFound = value; }
        public bool ForceFailoverFound { get => forceFailoverFound; set => forceFailoverFound = value; }
        public string AgId { get => agId; set => agId = value; }
    }

    class AutoFailoverReport : FailoverReport
    {

    }
}
