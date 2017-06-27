using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.XEvent.Linq;

namespace FailoverDetector
{
    public class AlwaysOnData
    {
        public EventList arStateChangeEvent;
        public EventList arMgrStateChangeEvent;
        public EventList errorEvent;

        public AlwaysOnData()
        {
            arStateChangeEvent = new EventList();
            arMgrStateChangeEvent = new EventList();
            errorEvent = new EventList();
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
        public void HandleDDLExecuted(PublishedEvent evt) { }
        public void HandleAGLeaseExpired(PublishedEvent evt) { }
        public void HandleARMgrStateChange(PublishedEvent evt) { }
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
