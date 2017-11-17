using System;
using System.Collections;
using System.Collections.Generic;
using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetectorTests
{
    [TestClass()]
    public class AlwaysOnDataTests
    {
        [TestMethod()]
        public void ParseStatementTest()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = "ALTER AVAILABILITY GROUP [ag_name] failover";
            Assert.AreEqual(true, mData.ParseStatement(testStr));
        }

        [TestMethod()]
        public void ParseStatementForceFailoverTest()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = "   ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss";
            Assert.AreEqual(true, mData.ParseStatement(testStr));

        }
        [TestMethod()]
        public void ParseStatementCreateAgTest()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = @"CREATE AVAILABILITY GROUP MyAg  WITH( 
                                AUTOMATED_BACKUP_PREFERENCE = SECONDARY, 
                                FAILURE_CONDITION_LEVEL = 3,    
                                HEALTH_CHECK_TIMEOUT = 600000
                                )   ";
            Assert.AreEqual(false, mData.ParseStatement(testStr));

        }

        [TestMethod()]
        public void ParseStatementWithEmptyString()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = @"   ";
            Assert.AreEqual(false, mData.ParseStatement(testStr));

        }

        // naive test that if ag name is same
        [TestMethod()]
        public void AgReportIteratorTest()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            pReportMgr.AddNewAgReport("testAG", "ze001");
            pReportMgr.AddNewAgReport("ag002", "ze002");
            List<string> actual = new List<string>();
            foreach (var agReport in pReportMgr.AgReportIterator())
            {
                actual.Add(agReport.AgName);
            }
            List<string> expected = new List<string>() { "testAG", "ag002" };
            CollectionAssert.AreEqual(actual, expected);
        }

        [TestMethod()]
        public void ReportIteratorForEachTest()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            pReportMgr.AddNewAgReport("testAG", "ze001");
            pReportMgr.AddNewAgReport("ag002", "ze002");

            DateTimeOffset baseTimeOffset = new DateTimeOffset(2017, 11, 15, 08, 00, 00, TimeSpan.Zero);
            DateTimeOffset ActualTimeoffset = baseTimeOffset;
            PartialReport tmpPartialReport;
            string agname = String.Empty;
            List<PartialReport> expected = new List<PartialReport>();
            int i;
            for (i = 0; i < 4; i++)
            {
                if (i / 2 == 0)
                {
                    agname = "testAG";
                }
                else
                {
                    agname = "ag002";
                }
                tmpPartialReport = pReportMgr.GetAgReports(agname).FGetReport(ActualTimeoffset);
                tmpPartialReport.ForceFailoverFound = true;
                ActualTimeoffset = baseTimeOffset.AddMinutes(15 *(i +1));

                expected.Add(tmpPartialReport);
            }
            i = 0;

            foreach (PartialReport report in pReportMgr.ReportIterator())
            {
                Assert.IsTrue(report.Equals(expected[i]));
                i++;
            }
            
        }
        [TestMethod]
        public void ReportIteratorTest()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            pReportMgr.AddNewAgReport("testAG", "ze001");
            pReportMgr.AddNewAgReport("ag002", "ze002");

            DateTimeOffset baseTimeOffset = new DateTimeOffset(2017, 11, 15, 08, 00, 00, TimeSpan.Zero);
            DateTimeOffset ActualTimeoffset = baseTimeOffset;
            PartialReport tmpPartialReport;
            string agname = String.Empty;
            List<PartialReport> expected = new List<PartialReport>();
            int i;
            for (i = 0; i < 4; i++)
            {
                if (i / 2 == 0)
                {
                    agname = "testAG";
                }
                else
                {
                    agname = "ag002";
                }
                tmpPartialReport = pReportMgr.GetAgReports(agname).FGetReport(ActualTimeoffset);
                tmpPartialReport.ForceFailoverFound = true;
                ActualTimeoffset = baseTimeOffset.AddMinutes(15 * (i + 1));

                expected.Add(tmpPartialReport);
            }
            i = 0;


            IEnumerator visitor = pReportMgr.ReportVisitor();
            while (visitor.MoveNext())
            {
                Assert.IsTrue(visitor.Current.Equals(expected[i]));
                i++;
            }

        }
        [TestMethod()]
        public void PartialReportEqualsTest()
        {
            PartialReport expected = new PartialReport();
            DateTimeOffset baseTimeOffset = new DateTimeOffset(2017, 11, 15, 08, 00, 00, TimeSpan.Zero);
            expected.StartTime = baseTimeOffset;
            expected.EndTime = baseTimeOffset;
            expected.AgName = "ag001";
            expected.AgId = "90001";
            expected.ForceFailoverFound = true;
            expected.AddRoleTransition("ze-vm001", "RESOLVING_NORMAL");
            expected.AddRoleTransition("ze-vm001", "RESOLVING_PENDING_FAILOVER");

            PartialReport actual = new PartialReport
            {
                StartTime = baseTimeOffset,
                EndTime = baseTimeOffset,
                AgName = "ag001",
                AgId = "90001",
                ForceFailoverFound = true
            };
            actual.AddRoleTransition("ze-vm001", "RESOLVING_NORMAL");
            actual.AddRoleTransition("ze-vm001", "RESOLVING_PENDING_FAILOVER");

            Assert.IsTrue(expected.Equals(actual));


        }
    }
}

