using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FailoverDetectorTests
{
    [TestClass()]
    public class AlwaysOnXeventParserTests
    {
        [TestMethod()]
        public void ParseStatementTest()
        {
            AlwaysOnXeventParser mData = new AlwaysOnXeventParser();
            string testStr = "ALTER AVAILABILITY GROUP [ag_name] failover";
            Assert.AreEqual(true, mData.ParseStatement(testStr));
        }

        [TestMethod()]
        public void ParseStatementForceFailoverTest()
        {
            AlwaysOnXeventParser mData = new AlwaysOnXeventParser();
            string testStr = "   ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss";
            Assert.AreEqual(true, mData.ParseStatement(testStr));

        }
        [TestMethod()]
        public void ParseStatementCreateAgTest()
        {
            AlwaysOnXeventParser mData = new AlwaysOnXeventParser();
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
            AlwaysOnXeventParser mData = new AlwaysOnXeventParser();
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
                ActualTimeoffset = baseTimeOffset.AddMinutes(15 * (i + 1));

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
                PartialReport reportInstance = (PartialReport)visitor.Current;
                reportInstance.ForceFailoverFound = false;

                Assert.IsTrue(visitor.Current.Equals(expected[i]));
                i++;
            }

        }

        [TestMethod]
        public void ReportIteratorUpdateTest()
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
                PartialReport reportInstance = (PartialReport)visitor.Current;
                reportInstance.ForceFailoverFound = false;
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

        // Test function to serialize Partial report to Json Format 
        [TestMethod]
        public void SerializePartialReport()
        {
            DateTimeOffset baseTimeOffset = new DateTimeOffset(2017, 11, 15, 08, 00, 00, TimeSpan.Zero);

            PartialReport pReport = new PartialReport()
            {
                OldPrimary = "ze-vm001",
                NewPrimary = "ze-vm002",
                AgId = "1234567",
                AgName = "Happy Ending",
                RootCause = "Newton's Apple",
                EstimateResult = true,
                RootCauseDescription = "It is just a joke",
                StartTime = baseTimeOffset,
                EndTime = baseTimeOffset
            };
            pReport.AddRoleTransition("ze-vm001", "RESOLVING_NORMAL");
            pReport.AddRoleTransition("ze-vm001", "RESOLVING_PENDING_FAILOVER");
            pReport.AddRoleTransition("ze-vm002", "RESOLVING_PENDING_FAILOVER");
            pReport.AddRoleTransition("ze-vm002", "RESOLVING_NORMAL");
            string _testString = @"2017-09-14 16:19:57.05 spid24s     Always On: The availability replica manager is going offline because the local Windows Server Failover Clustering (WSFC) node has lost quorum. This is an informational message only. No user action is required.";

            pReport.AddNewMessage(Constants.SourceType.ErrorLog, "ze-vm001", baseTimeOffset, _testString);

            // serialize to json stream
            string output = JsonConvert.SerializeObject(pReport);
            Console.WriteLine(output);


            using (var stringReader = new StringReader(output))
            using (StreamWriter sw = new StreamWriter(@"C:\temp\json.txt"))
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(sw) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);

            }
        }


        [TestMethod()]
        public void MergeReportTest()
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
            expected.AddRoleTransition("ze-vm002", "RESOLVING_NORMAL");
            expected.AddRoleTransition("ze-vm002", "RESOLVING_PENDING_FAILOVER");

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

            PartialReport other = new PartialReport
            {
                StartTime = baseTimeOffset,
                EndTime = baseTimeOffset,
                AgName = "ag001",
                AgId = "90001",
                ForceFailoverFound = true
            };
            actual.AddRoleTransition("ze-vm002", "RESOLVING_NORMAL");
            actual.AddRoleTransition("ze-vm002", "RESOLVING_PENDING_FAILOVER");
            actual.MergeReport(other);

            Assert.IsTrue(expected.Equals(actual));

        }

        [TestMethod()]
        public void MergeReportsTest()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            pReportMgr.AddNewAgReport("ag001", "ze001");


            PartialReport expected = new PartialReport();
            DateTimeOffset baseTimeOffset = new DateTimeOffset(2017, 11, 15, 08, 00, 00, TimeSpan.Zero);
            DateTimeOffset endTimeOffset = new DateTimeOffset(2017, 11, 15, 08, 10, 00, TimeSpan.Zero);
            expected.StartTime = baseTimeOffset;
            expected.EndTime = endTimeOffset;
            expected.AgName = "ag001";
            expected.AgId = "90001";
            expected.ForceFailoverFound = true;
            expected.AddRoleTransition("ze-vm001", "RESOLVING_NORMAL");
            expected.AddRoleTransition("ze-vm001", "RESOLVING_PENDING_FAILOVER");
            expected.AddRoleTransition("ze-vm002", "RESOLVING_NORMAL");
            expected.AddRoleTransition("ze-vm002", "RESOLVING_PENDING_FAILOVER");


            AgReport agReport = pReportMgr.GetAgReports("ag001");
            PartialReport actual = agReport.FGetReport(baseTimeOffset);

            actual.StartTime = baseTimeOffset;
            actual.EndTime = baseTimeOffset;
            actual.AgName = "ag001";
            actual.AgId = "90001";
            actual.ForceFailoverFound = true;
            actual.AddRoleTransition("ze-vm001", "RESOLVING_NORMAL");
            actual.AddRoleTransition("ze-vm001", "RESOLVING_PENDING_FAILOVER");

            PartialReport other = agReport.FGetReport(endTimeOffset);
            other.StartTime = endTimeOffset;
            other.EndTime = endTimeOffset;
            other.AgName = "ag001";
            other.AgId = "90001";
            other.ForceFailoverFound = true;
            other.AddRoleTransition("ze-vm002", "RESOLVING_NORMAL");
            other.AddRoleTransition("ze-vm002", "RESOLVING_PENDING_FAILOVER");

            agReport.MergeReports();

            Assert.IsTrue(agReport.Reports.Count == 1);
            Assert.IsTrue(agReport.Reports[0].Equals(expected));

        }
    }
}

