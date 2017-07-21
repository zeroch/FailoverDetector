using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;


namespace FailoverDetector
{
    class SystemHealthParser
    {
        public void ParseSystemComponent(String xmlString)
        {
            StringBuilder output = new StringBuilder();
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
            {
                while(reader.Read())
                {
                    // copy from diagnose.cpp void SystemDiagComp::GetData()
                    // XML format: 
                    //  <system spinlockBackoffs="u" sickSpinlockType="s" sickSpinlockTypeAfterAv="s" latchWarnings="u"
                    //      isAccessViolationOccurred="u" memoryScribblerCount="u" totalDumpRequests="u" periodDumpRequests="u"
                    //      nonYieldingTasksReported="u" pageFaultRate="u" systemCpuUtilization="u" sqlCpuUtilization="u" />
                    //
                    if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("system"))
                    {

                        string sickSpinlockType = reader.GetAttribute("sickSpinlockType");
                        string sickSpinlockTypeAfterAv = reader.GetAttribute("sickSpinlockTypeAfterAv");
                        string BadPagesDetected = reader.GetAttribute("BadPagesDetected");
                        string BadPagesFixed = reader.GetAttribute("BadPagesFixed");
                        string LastBadPageAddress = reader.GetAttribute("LastBadPageAddress");
                        UInt32 temp = 0;
                        UInt32 spinlockBackoffs = UInt32.TryParse(reader.GetAttribute("spinlockBackoffs"), out temp) ? temp : 0;
                        UInt32 latchWarnings = UInt32.TryParse(reader.GetAttribute("latchWarnings"), out temp) ? temp : 0;
                        UInt32 isAccessViolationOccurred = UInt32.TryParse(reader.GetAttribute("isAccessViolationOccurred"), out temp) ? temp : 0;
                        UInt32 writeAccessViolationCount = UInt32.TryParse(reader.GetAttribute("writeAccessViolationCount"), out temp) ? temp : 0;
                        UInt32 totalDumpRequests = UInt32.TryParse(reader.GetAttribute("totalDumpRequests"), out temp) ? temp : 0;
                        UInt32 intervalDumpRequests = UInt32.TryParse(reader.GetAttribute("intervalDumpRequests"), out temp) ? temp : 0;
                        UInt32 nonYieldingTasksReported = UInt32.TryParse(reader.GetAttribute("nonYieldingTasksReported"), out temp) ? temp : 0;
                        UInt32 pageFaults = UInt32.TryParse(reader.GetAttribute("pageFaults"), out temp) ? temp : 0;
                        UInt32 systemCpuUtilization = UInt32.TryParse(reader.GetAttribute("systemCpuUtilization"), out temp) ? temp : 0;
                        UInt32 sqlCpuUtilization = UInt32.TryParse(reader.GetAttribute("sqlCpuUtilization"), out temp) ? temp : 0;

                        //Console.WriteLine("systemCpuUtilization: {0}", systemCpuUtilization);
                        //Console.WriteLine("Detail: spinlockBackoffs: {0} \t totalDumpRequests: {1}", spinlockBackoffs, totalDumpRequests);
                    }
                }
            }
        }

        public bool ParseQPComponent(String xmlString)
        {
            bool ret = true;
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
            {
                while (reader.Read())
                {
                    // copy from diagnose.cpp void SystemDiagComp::GetData()
                    // XML format: 
                    // XML format: 
                    //	<queryProcessing maxWorkers="u" workersCreated="u" workersIdle="u" tasksCompletedWithinInterval="u"
                    //		pendingTasks="u" oldestPendingTaskWaitingTime="u" hasUnresolvableDeadlockOccurred="b"
                    //		hasDeadlockedSchedulersOccurred="b" trackingNonYieldingScheduler="p">

                    if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("queryProcessing"))
                    {
                        UInt32 temp = 0;
                        UInt32 maxWorkers = UInt32.TryParse(reader.GetAttribute("maxWorkers"), out temp) ? temp : 0;
                        UInt32 workersCreated = UInt32.TryParse(reader.GetAttribute("workersCreated"), out temp) ? temp : 0;
                        UInt32 workersIdle = UInt32.TryParse(reader.GetAttribute("workersIdle"), out temp) ? temp : 0;
                        UInt32 tasksCompletedWithinInterval = UInt32.TryParse(reader.GetAttribute("tasksCompletedWithinInterval"), out temp) ? temp : 0;
                        UInt32 pendingTasks = UInt32.TryParse(reader.GetAttribute("pendingTasks"), out temp) ? temp : 0;
                        UInt32 oldestPendingTaskWaitingTime = UInt32.TryParse(reader.GetAttribute("oldestPendingTaskWaitingTime"), out temp) ? temp : 0;

                        //UInt32 trackingNonYieldingScheduler = UInt32.TryParse(reader.GetAttribute("trackingNonYieldingScheduler"), out temp) ? temp : 0;
                        UInt32 hasUnresolvableDeadlockOccurred = UInt32.TryParse(reader.GetAttribute("hasUnresolvableDeadlockOccurred"), out temp) ? temp : 0;
                        UInt32 hasDeadlockedSchedulersOccurred = UInt32.TryParse(reader.GetAttribute("hasDeadlockedSchedulersOccurred"), out temp) ? temp : 0;
                        bool b_hasUnresolvableDeadlockOccurred = (hasUnresolvableDeadlockOccurred != 0);
                        bool b_hasDeadlockedSchedulersOccurred = (hasDeadlockedSchedulersOccurred != 0);
                        if (b_hasDeadlockedSchedulersOccurred)
                        {
                            Console.WriteLine("Query Processing Error: Deadlocked Scheduler Occurred");
                            ret = false;
                        }
                        if (b_hasUnresolvableDeadlockOccurred)
                        {
                            Console.WriteLine("Query Processing Error: unresolvable Deadlock Occurred");
                            ret = false;
                        }

                        if (pendingTasks > 0)
                        {
                            Console.WriteLine("Query Processing Warning: Pending Task more than 0.");
                            ret = false;
                        }
                    }
                }
            }
            return ret;
        }

        public void ParseResource(String xmlString) { }
        public void ParseIOSubsytem(String xmlString) { }
    }
}
