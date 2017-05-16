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
        public void loadData(string xelFileName, string serverName)
        {  
            // load xel File
            // filter data
            // extract into alwaysOnData
           
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
