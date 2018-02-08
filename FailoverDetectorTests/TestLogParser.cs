using System;
using System.Collections.Generic;
using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetectorTests
{
    [TestClass]
    public class TestLogParser
    {
        readonly string _testString = @"2017-09-14 16:19:57.05 spid24s     Always On: The availability replica manager is going offline because the local Windows Server Failover Clustering (WSFC) node has lost quorum. This is an informational message only. No user action is required.";
        ErrorLogParser _logParser;

        private readonly string baseTestPath =
            @"ErrorLog\";
        [TestInitialize]
        public void Setup()
        {
            _logParser = new ErrorLogParser();
        }
        [TestMethod]
        public void TestTokenizeTimestamp()
        {
            string tmpTimeStamp = _logParser.TokenizeTimestamp(_testString);
            Console.WriteLine(tmpTimeStamp);
            Assert.AreEqual(tmpTimeStamp, "2017-09-14 16:19:57.05");
        }

        [TestMethod]
        public void TestParseTimeStamp()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 14, 16, 19, 57, new TimeSpan(0, 0, 0));

            string retTime = _logParser.TokenizeTimestamp(_testString);
            DateTimeOffset parsedTime = _logParser.ParseTimeStamp(retTime);
            Console.WriteLine("Parse string timestamp: {0} to DateTimeOffset: {1}", retTime, parsedTime.ToString());
            DateTimeOffset utcTime = cmp.ToUniversalTime();
            Assert.AreEqual(utcTime, parsedTime);
        }

        [TestMethod]
        public void TestParseLogEntry()
        {
            string testString = @"2018-01-24 09:29:49.40 spid45s     The state of the local availability replica in availability group 'FirstHadron' has changed from 'SECONDARY_NORMAL' to 'RESOLVING_NORMAL'.  The state changed because the availability group state has changed in Windows Server Failover Clustering (WSFC).  For more information, see the SQL Server error log or cluster log.  If this is a Windows Server Failover Clustering (WSFC) availability group, you can also see the WSFC management console.";
            ErrorLogParser parser = new ErrorLogParser();
            ErrorLogEntry actual = parser.ParseLogEntry(testString);
            ErrorLogEntry expected = new ErrorLogEntry()
            {
                Timestamp = new DateTimeOffset(2018, 01, 24, 09, 29, 49, TimeSpan.Zero),
                Spid = "spid45s",
                Message = "The state of the local availability replica in availability group 'FirstHadron' has changed from 'SECONDARY_NORMAL' to 'RESOLVING_NORMAL'.  The state changed because the availability group state has changed in Windows Server Failover Clustering (WSFC).  For more information, see the SQL Server error log or cluster log.  If this is a Windows Server Failover Clustering (WSFC) availability group, you can also see the WSFC management console.",
                RawMessage = testString
            };

            Assert.AreEqual(expected, actual);

        }

        [TestMethod]
        public void TestErrorLogEntryEquals()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 10, 22, 00, 00, new TimeSpan(0, 0, 0));
            ErrorLogEntry pEntry = new ErrorLogEntry(cmp, "spid191", "", "UTC adjustment: -4:00");
            string testString = @"2017-09-10 22:00:00.19 spid191     UTC adjustment: -4:00";
            ErrorLogEntry entry = new ErrorLogEntry();
            ErrorLogParser parser = new ErrorLogParser();
            entry = parser.ParseLogEntry(testString);
            pEntry.RawMessage = testString;
            Assert.IsTrue(entry.Equals(pEntry));

        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\ErrorLog", "ErrorLog")]
        public void ParseLogStopServiceTest()
        {
            string testLogPath = baseTestPath + "TestCase_0.txt";
            // prepare environment report object
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            // Create a fake report
            pReportMgr.AddNewAgReport("Dummy", "ze-vm001");
            DateTimeOffset testTimeOffset = new DateTimeOffset(2017, 10, 23, 18, 42, 31, TimeSpan.Zero);
            PartialReport pReport = pReportMgr.GetAgReports("Dummy").FGetReport(testTimeOffset);
            PartialReport expected = new PartialReport()
            {
                StartTime = testTimeOffset,
                EndTime = testTimeOffset,
                MessageSet = new HashSet<string>() { "17148" }
            };

            _logParser.ParseLog(testLogPath, "ze-vm001");

            Assert.IsTrue(expected.Equals(pReport));

        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\ErrorLog", "ErrorLog")]
        public void ParseLogShutdownServerTest()
        {
            string testLogPath = baseTestPath + "TestCase_1.txt";
            // prepare environment report object
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            // Create a fake report
            pReportMgr.AddNewAgReport("Dummy", "ze-vm001");
            DateTimeOffset testTimeOffset = new DateTimeOffset(2017, 10, 23, 18, 32, 31, TimeSpan.Zero);
            PartialReport pReport = pReportMgr.GetAgReports("Dummy").FGetReport(testTimeOffset);
            PartialReport expected = new PartialReport()
            {
                StartTime = testTimeOffset,
                EndTime = testTimeOffset,
                MessageSet = new HashSet<string>() { "17147" }
            };

            _logParser.ParseLog(testLogPath, "ze-vm001");

            Assert.IsTrue(expected.Equals(pReport));

        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\ErrorLog", "ErrorLog")]
        public void ParseRoleTransitionTest()
        {
            string testLogPath = baseTestPath + "TestCase_2.txt";
            // Create a fake report
            PartialReport expected = new PartialReport()
            {
                StartTime = new DateTimeOffset(2018, 01, 24, 09, 23, 32, new TimeSpan(-8, 0, 0)),
                EndTime = new DateTimeOffset(2018, 01, 24, 09, 29, 53, new TimeSpan(-8, 0, 0)),
                AgName = "FirstHadron",
            };

            expected.AddRoleTransition("LZHANG1S", "SECONDARY_NORMAL", "RESOLVING_NORMAL");
            expected.AddRoleTransition("LZHANG1S", "RESOLVING_NORMAL", "PRIMARY_PENDING");
            expected.AddRoleTransition("LZHANG1S", "PRIMARY_PENDING", "PRIMARY_NORMAL");



            // run TestJob job to parse a testcase which generate report from errorlog without xevent
            _logParser.ParseLog(testLogPath, "LZHANG1S");

            // pull report from list
            DateTimeOffset testTimeOffset = new DateTimeOffset(2018, 01, 24, 09, 29, 00, new TimeSpan(-8, 0, 0));
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            PartialReport pReport = pReportMgr.GetAgReports("FirstHadron").FGetReport(testTimeOffset);


            Assert.IsTrue(expected.Equals(pReport));
        }
    }
}
