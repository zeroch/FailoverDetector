using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.XEvent.Linq;
using System.Linq;

namespace FailoverDetector
{
    public class AlwaysOnData
    {

        List<PartialReport> m_reports;
        public AlwaysOnData()
        {

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
                    if(!m_reports.Any() || ((m_reports.Last().EndTime - evt.Timestamp).TotalMinutes > 5) )
                    {
                        PartialReport pReport = new PartialReport();
                        pReport.StartTime = evt.Timestamp;
                        pReport.EndTime = evt.Timestamp;
                        pReport.AgName = evt.Fields["availability_group_name"].Value.ToString();
                        pReport.AgId = evt.Fields["availability_group_id"].Value.ToString();
                        pReport.ForceFailoverFound = true;

                        m_reports.Add(pReport);
                    }else
                    {
                        PartialReport prevReport = m_reports.Last();
                        {
                            prevReport.EndTime = evt.Timestamp;
                            prevReport.AgName = evt.Fields["availability_group_name"].Value.ToString();
                            prevReport.AgId = evt.Fields["availability_group_id"].Value.ToString();
                            prevReport.ForceFailoverFound = true;
                        }
                    }

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
        }
        public void HandleARState(PublishedEvent evt) { }
        public void HandleARStateChange(PublishedEvent evt) { }
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
