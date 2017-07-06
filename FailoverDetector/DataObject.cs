using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.XEvent.Linq;
using System.Linq;

namespace FailoverDetector
{
    public class ReportListManager
    {
        List<PartialReport> m_reports;
        string m_agName;
        public ReportListManager()
        {
            m_reports = new List<PartialReport>();
            m_agName = "";
        }
        public PartialReport FGetReport(PublishedEvent evt)
        {
            PartialReport pReport;
            if (!m_reports.Any() || ((evt.Timestamp - m_reports.Last().EndTime).TotalMinutes >5))
            {

                pReport = new PartialReport();
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

        // we should always has pReport populate from list
        // but I will check list anyway
        public void UpdateReport(PartialReport pReport)
        {
            if(m_reports.Any())
            {
                m_reports.Remove(m_reports.Last());
                m_reports.Add(pReport);
            }
        }
    }
    public class AlwaysOnData
    {

        List<PartialReport> m_reports;
        Dictionary<string, ReportListManager> agEventMap;
        public AlwaysOnData()
        {
            agEventMap = new Dictionary<string, ReportListManager>();
        }
        enum AlwaysOn_EventType {
            DLL_EXECUTED,
            AG_LEASE_EXPIRED,
            AR_MANGER_STATE_CHANGE,
            AR_STATE,
            AR_STATE_CHANGE,
            LOCK_REDO_BLOCKED,
            ERROR
        }
        public void HandleDDLExecuted(PublishedEvent evt)
        {
            // find active alter ag failover
            // we only can find based on statement:
            // pattern is 
            // ALTER AVAILABILITY GROUP [ag_name] failover
            // ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss
            string evt_statement = evt.Fields["statement"].Value.ToString();
            bool isForceFailover = ParseStatement(evt_statement);
            if (isForceFailover)
            {
                // receive a agName, which mean PrseStatement valid a failover statement
                // check fill report or populate a report
                bool commited = (evt.Fields["ddl_phase"].Value.ToString() == "commit");
                if (commited)
                {
                    string agName = evt.Fields["availability_group_name"].Value.ToString();
                    // get List of report for this ag
                    if (!agEventMap.TryGetValue(agName, out ReportListManager m_reports))
                    {
                        m_reports = new ReportListManager();
                        agEventMap.Add(agName, m_reports);
                    }
                    PartialReport pReport = m_reports.FGetReport(evt);
                    pReport.ForceFailoverFound = true;

                    m_reports.UpdateReport(pReport);

                }

            }

        }
        // parse and find failover statement
        // ALTER AVAILABILITY GROUP [ag_name] failover
        // ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss
        public bool ParseStatement(string str)
        {

            string[] wds = str.Split(' ');
            List<string> words = wds.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ;
            string[] compare = { "alter", "availability", "group" };
            string[] failover = { "failover", "force_failover_allow_data_loss" };

            string command = String.Join(" ", words.Take(3)).ToLower();
            string parameter = words[4].ToLower();
            if (command.Equals("alter availability group"))
            {
                if (parameter.Equals("failover") || parameter.Equals("force_failover_allow_data_loss"))
                {
                    return true;
                }
            }
            return false;
        }


        public void HandleAGLeaseExpired(PublishedEvent evt)
        {

        }
        public void HandleARMgrStateChange(PublishedEvent evt)
        {
;
        }
        public void HandleARState(PublishedEvent evt) { }
        public void HandleARStateChange(PublishedEvent evt)
        {
            string agName = evt.Fields["availability_group_name"].Value.ToString();
            // get List of report for this ag
            if (!agEventMap.TryGetValue(agName, out ReportListManager m_reports))
            {
                m_reports = new ReportListManager();
                agEventMap.Add(agName, m_reports);
            }
            PartialReport pReport = m_reports.FGetReport(evt);
            pReport.AddRoleTransition(evt.Fields["previous_state"].Value.ToString());
            pReport.AddRoleTransition(evt.Fields["current_state"].Value.ToString());

            m_reports.UpdateReport(pReport);

        }


        public void HandleLockRedoBlocked(PublishedEvent evt) { }
        public void HandleErrorReported(PublishedEvent evt) { }
        private void DispatchEvent(PublishedEvent evt)
        {
            switch(evt.Name)
            {
                case "alwayson_ddl_executed":
                    HandleDDLExecuted(evt);
                    break;
                case "availability_group_lease_expired":
                    HandleAGLeaseExpired(evt);
                    break;
                case "availability_replica_manager_state_change":
                    HandleARMgrStateChange(evt);
                    break;
                case "availability_replica_state":
                    HandleARState(evt);
                    break;
                case "availability_replica_state_change":
                    HandleARStateChange(evt);
                    break;
                case "lock_redo_blocked":
                    HandleLockRedoBlocked(evt);
                    break;
                case "error_reported":
                    HandleErrorReported(evt);
                    break;
                default:
                    break;
            }
        }
        public void loadData(string xelFileName, string serverName)
        {
            // load xel File
            using (QueryableXEventData events = new QueryableXEventData(xelFileName))
            {
                foreach(PublishedEvent evt in events)
                {
                    // dispatch event and handle by own method.
                    DispatchEvent(evt);
                }
            }

           
        }
        
    }
    public class SystemData
    {
        public EventList spDiagResultEvents;
        public SystemData()
        {
            spDiagResultEvents = new EventList();
        }
        public void loadData(string xelFileName, string serverName) { }
    }
    public class EventList : IEnumerable
    {
        public List<PublishedEvent> events;

        public EventList()
        {
            events = new List<PublishedEvent>();
        }
        public void append(PublishedEvent evt) { events.Add(evt); }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return (IEnumerable)GetEnumerator();
        }
        public EventListEnum GetEnumerator()
        {
            return new EventListEnum(events);
        }
    }
    public class EventListEnum : IEnumerator
    {
        public List<PublishedEvent> _events;
        int pos = -1;

        public EventListEnum(List<PublishedEvent> list)
        {
            _events = list;
        }
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }
        
        public PublishedEvent Current
        {
            get
            {
                try
                {
                    return _events[pos];
                }catch( IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public bool MoveNext()
        {
            pos++;
            return (pos < _events.Count);
        }

        public void Reset()
        {
            pos = -1;
        }
    }
}
