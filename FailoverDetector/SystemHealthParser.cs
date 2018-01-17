using System;
using System.Text;
using System.Xml;
using System.IO;
using Microsoft.SqlServer.XEvent.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace FailoverDetector
{

    public class SystemHealthParser : XeventParser
    {

        public SystemHealthParser()
        {
           
        }
        PartialReport pCurrentReport;
        public void LoadXevents(string xelFileName, string serverName)
        {
            _instanceName = serverName;
            // load xel File
            try
            {
                using (QueryableXEventData events = new QueryableXEventData(xelFileName))
                {
                    ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;

                    // I need an interator that I don't care which AG it is
                    // only iterate through by time.
                    //pReportMgr.
                    IEnumerator ReportIterator = pReportMgr.ReportVisitor();
                    if (!ReportIterator.MoveNext())
                        return;

                    IEnumerator pXEventIterator = events.GetEnumerator();

                    foreach (PublishedEvent evt in events)
                    {
                        // dispatch event and handle by own method.
                        DateTimeOffset messageTime = evt.Timestamp;
                        // compare time
                        PartialReport reportInstance = (PartialReport)ReportIterator.Current;

                        // find a report later than current message

                        while (reportInstance != null && (messageTime > reportInstance.EndTime.AddMinutes(Constants.DefaultInterval)))
                        {
                            // if we go through all reports but cannot find a report is older than this message
                            // we know all evetns behinds it are too late. 
                            if (!ReportIterator.MoveNext())
                            {
                                return;
                            }
                            reportInstance = (PartialReport)ReportIterator.Current;
                        }

                        // now we iterate events to meet time interval. 
                        if (messageTime < (reportInstance.StartTime.AddMinutes(-1 * Constants.DefaultInterval)))
                        {
                            continue;
                        }
                        else
                        {

                            //if (messageTime < sometime upper bound
                            //    && messageTime > sometime lower bound)
                            // it is time to dispatch event, but we only care about sp_server_diag at this point. 
                            if (evt.Name == "sp_server_diagnostics_component_result")
                            {
                                // we know that we want to handle this event, and it is working with a report. use pCurrentReport to hold this pointer. 
                                pCurrentReport = reportInstance;
                                DispatchEvent(evt);
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {

                Console.WriteLine(e.Message);
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                return;
            }

        }
        public override void DispatchEvent(PublishedEvent evt)
        {
            string t_component = evt.Fields["component"].Value.ToString();
            string t_data = evt.Fields["data"].Value.ToString();

            switch (t_component)
            {
                case "SYSTEM":
                    ParseSystemComponent(t_data);
                    break;
                case "RESOURCE":
                    ParseResource(t_data);
                    break;
                case "QUERY_PROCESSING":
                    ParseQpComponent(t_data);
                    break;
                case "IO_SUBSYSTEM":
                    ParseIoSubsytem(t_data);
                    break;
                default:
                    break;
            }

            // insert data to raw message
            pCurrentReport.AddNewMessage(Constants.SourceType.SystemHealthXevent, _instanceName, evt.Timestamp, t_data);
        }

        // Data structure used to parse xml and json format
        // IExtensibleDataObject is used to bypass invalide namespace issue. 
        [XmlRootAttribute("system", Namespace = "",
IsNullable = false)]
//        [DataContract(Name = "system", Namespace ="")]
        public class SystemComponent : IExtensibleDataObject
        {

            public ExtensionDataObject ExtensionData { get; set; }
            [XmlAttribute("sickSpinlockType")]
            public string sickSpinlockType { get; set; }

            [XmlAttribute("sickSpinlockTypeAfterAv")]
            public string sickSpinlockTypeAfterAv { get; set; }

            [XmlAttribute("BadPagesDetected")]
            public string badPagesDetected { get; set; }

            [XmlAttribute("BadPagesFixed")]
            public string badPagesFixed { get; set; }

            [XmlAttribute("LastBadPageAddress")]
            public string lastBadPageAddress { get; set; }

            [XmlAttribute("spinlockBackoffs")]
            public UInt32 spinlockBackoffs { get; set; }

            [XmlAttribute("latchWarnings")]
            public UInt32 latchWarnings { get; set; }

            [XmlAttribute("isAccessViolationOccurred")]
            public UInt32 isAccessViolationOccurred { get; set; }

            [XmlAttribute("writeAccessViolationCount")]
            public UInt32 writeAccessViolationCount { get; set; }

            [XmlAttribute("memoryScribblerCount")]
            public UInt32 memoryScribblerCount { get; set; }

            [XmlAttribute("totalDumpRequests")]
            public UInt32 totalDumpRequests { get; set; }

            [XmlAttribute("intervalDumpRequests")]
            public UInt32 intervalDumpRequests { get; set; }

            [XmlAttribute("nonYieldingTasksReported")]
            public UInt32 nonYieldingTasksReported { get; set; }

            [XmlAttribute("pageFaults")]
            public UInt32 pageFaults { get; set; }

            [XmlAttribute("systemCpuUtilization")]
            public UInt32 systemCpuUtilization { get; set; }

            [XmlAttribute("sqlCpuUtilization")]
            public UInt32 sqlCpuUtilization { get; set; }

        }

        [XmlRootAttribute("queryProcessing", Namespace = "",
IsNullable = false)]
        public class QPComponent: IExtensibleDataObject
        {
            public ExtensionDataObject ExtensionData { get; set; }
            [XmlAttribute("maxWorkers")]
            public UInt32 maxWorkers { get; set; }
            [XmlAttribute("workersCreated")]
            public UInt32 workersCreated { get; set; }
            [XmlAttribute("workersIdle")]
            public UInt32 workersIdle { get; set; }
            [XmlAttribute("tasksCompletedWithinInterval")]
            public UInt32 tasksCompletedWithinInterval { get; set; }
            [XmlAttribute("pendingTasks")]
            public UInt32 pendingTasks { get; set; }
            [XmlAttribute("oldestPendingTaskWaitingTime")]
            public UInt32 oldestPendingTaskWaitingTime { get; set; }

            [XmlAttribute("hasUnresolvableDeadlockOccurred")]
            public bool hasUnresolvableDeadlockOccurred { get; set; }
            [XmlAttribute("hasDeadlockedSchedulersOccurred")]
            public bool hasDeadlockedSchedulersOccurred { get; set; }

            [XmlAttribute("trackingNonYieldingScheduler")]
            public string trackingNonYieldingScheduler { get; set; }
        }

        [XmlRootAttribute("resource", Namespace = "",
IsNullable = false)]
        public class ResourceComponent : IExtensibleDataObject
        {
            public ExtensionDataObject ExtensionData { get; set; }

            [XmlAttribute("lastNotification")]
            public string lastNotification { get; set; }
            [XmlAttribute("outOfMemoryExceptions")]
            public UInt32 outOfMemoryExceptions { get; set; }
            [XmlAttribute("isAnyPoolOutOfMemory")]
            public bool isAnyPoolOutOfMemory { get; set; }
            [XmlAttribute("processOutOfMemoryPeriod")]
            public int processOutOfMemoryPeriod { get; set; }
            
        }

        [XmlRootAttribute("ioSubsystem", Namespace = "",
IsNullable = false)]
        public class IoComponent
        {
            public ExtensionDataObject ExtensionData { get; set; }

            [XmlAttribute("ioLatchTimeouts")]
            public UInt32 ioLatchTimeouts { get; set; }
            [XmlAttribute("intervalLongIos")]
            public UInt32 intervalLongIos { get; set; }
            [XmlAttribute("totalLongIos")]
            public UInt32 totalLongIos { get; set; }

        }

        public void ParseSystemComponent(string xmlString)
        {
            // copy from diagnose.cpp void SystemDiagComp::GetData()
            // XML format: 
            //  <system spinlockBackoffs="u" sickSpinlockType="s" sickSpinlockTypeAfterAv="s" latchWarnings="u"
            //      isAccessViolationOccurred="u" memoryScribblerCount="u" totalDumpRequests="u" periodDumpRequests="u"
            //      nonYieldingTasksReported="u" pageFaultRate="u" systemCpuUtilization="u" sqlCpuUtilization="u" />
            //
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlString));

            XmlSerializer serializer = new XmlSerializer(typeof(SystemComponent));
            SystemComponent systemComp = new SystemComponent();
            systemComp = (SystemComponent)serializer.Deserialize(ms);

            if (systemComp.totalDumpRequests > 100 && systemComp.intervalDumpRequests > 1)
            {
                pCurrentReport.SystemUnhealthFound = true;
                pCurrentReport.ExceedDumpThreshold = true;
            }
 
            if(systemComp.memoryScribblerCount > 3)
            {
                pCurrentReport.SystemUnhealthFound = true;
                pCurrentReport.Memorycribbler = true;
            }
            
            if (!systemComp.sickSpinlockTypeAfterAv.Equals("none"))
            {
                pCurrentReport.SickSpinLock = true;
            }

            // write useful data to report
            pCurrentReport.systemCpuUtilization = systemComp.systemCpuUtilization;
            pCurrentReport.sqlCpuUtilization = systemComp.sqlCpuUtilization;


        }

        public void ParseQpComponent(String xmlString)
        {

            // copy from diagnose.cpp void SystemDiagComp::GetData()
            // XML format: 
            // XML format: 
            //	<queryProcessing maxWorkers="u" workersCreated="u" workersIdle="u" tasksCompletedWithinInterval="u"
            //		pendingTasks="u" oldestPendingTaskWaitingTime="u" hasUnresolvableDeadlockOccurred="b"
            //		hasDeadlockedSchedulersOccurred="b" trackingNonYieldingScheduler="p">

            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlString));

            XmlSerializer serializer = new XmlSerializer(typeof(QPComponent));
            QPComponent qPComponent = new QPComponent();
            qPComponent = (QPComponent)serializer.Deserialize(ms);


            if (qPComponent.hasDeadlockedSchedulersOccurred || qPComponent.hasUnresolvableDeadlockOccurred)
            {
                pCurrentReport.UnresolvedDeadlock = true;
                pCurrentReport.SystemUnhealthFound = true;
            }
            pCurrentReport.pendingTasksCount = qPComponent.pendingTasks;
        }

        public void ParseResource(String xmlString)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlString));

            XmlSerializer serializer = new XmlSerializer(typeof(ResourceComponent));
            ResourceComponent resourceComponent = new ResourceComponent();
            resourceComponent = (ResourceComponent)serializer.Deserialize(ms);

            if (resourceComponent.processOutOfMemoryPeriod > 12000)
            {
                pCurrentReport.SystemUnhealthFound = true;
                pCurrentReport.SqlOOM = true;
            }
            if (resourceComponent.Equals("RESOURCE_MEMPHYSICAL_LOW") || resourceComponent.Equals("RESOURCE_MEMVIRTUAL_LOW"))
            {
                pCurrentReport.SqlLowMemory = true;
            }

        }
        public void ParseIoSubsytem(String xmlString)
        {
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlString));

            XmlSerializer serializer = new XmlSerializer(typeof(IoComponent));
            IoComponent ioComponent = new IoComponent();
            ioComponent = (IoComponent)serializer.Deserialize(ms);
            if (ioComponent.intervalLongIos > 0)
            {
                pCurrentReport.LongIO = true;
            }
            pCurrentReport.intervalLongIos = ioComponent.intervalLongIos;
        }



    }
}
