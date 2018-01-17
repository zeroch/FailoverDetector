using System;
using System.Collections.Generic;
using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetectorTests
{
    [TestClass]
    public class TestClusterLogParser
    {
        ClusterLogParser _clusterLogParser;
        private string baseTestPath = @"ClusterLog\";
        [TestInitialize]
        public void Setup()
        {
            _clusterLogParser = new ClusterLogParser();

        }
        [TestMethod]
        public void TestTokenizeTimestamp()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retTime = _clusterLogParser.TokenizeTimestamp(testString);
            Console.WriteLine("Test Time stamp token: {0}", retTime);
            Assert.AreEqual(retTime, "2017/09/14-18:16:27.575");
        }
        [TestMethod]
        public void TestParseTimeStamp()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 14, 18, 16, 27, new TimeSpan(0, 0, 0));

            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retTime = _clusterLogParser.TokenizeTimestamp(testString);
            DateTimeOffset parsedTime = _clusterLogParser.ParseTimeStamp(retTime);
            Console.WriteLine("Parse string timestamp: {0} to DateTimeOffset: {1}", retTime, parsedTime.ToString());
            DateTimeOffset utcTime = cmp.ToUniversalTime();
            Assert.AreEqual(utcTime, parsedTime);
        }
        [TestMethod]
        public void TestTokenizePid()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retPid = _clusterLogParser.TokenizePidTid(testString);
            Console.WriteLine("Test PID and thread ID: {0}", retPid);
            Assert.AreEqual(retPid, "0000126c.00001cec::");
        }

        [TestMethod]
        public void TestTokenizeEntryType()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retType = _clusterLogParser.TokenizeEntryType(testString);
            Console.WriteLine("Test Entry Type: {0}", retType);
            Assert.AreEqual(retType, "ERR");

        }
        [TestMethod]
        public void TestTokenizeChannel()
        {
            string testString = @"[RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retChannel = _clusterLogParser.TokenizeChannel(testString);
            Console.WriteLine("Test Channel: {0}", retChannel);
            Assert.AreEqual(retChannel, "[RES]");
        }
        [TestMethod]
        public void TestClusterParseLog()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            ErrorLogEntry ret = _clusterLogParser.ParseLogEntry(testString);
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 14, 18, 16, 27, new TimeSpan(0, 0, 0));
            ErrorLogEntry test = new ErrorLogEntry(cmp, @"0000126c.00001cec::", @"SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive");
            test.RawMessage = testString;
            Assert.IsTrue(ret.Equals(test));
        }


        [TestMethod]
        [DeploymentItem("Data\\UnitTest\\ClusterLog", "ClusterLog")]
        // this test case includes "1135", "1177", "1146"
        public void ParseCLusterLogTest_0()
        {
            string testLogPath = baseTestPath + "TestCase_0.txt";
            // prepare environment report object
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            // Create a fake report
            pReportMgr.AddNewAgReport("Dummy", "ze-vm001");
            DateTimeOffset testTimeOffset = new DateTimeOffset(2017, 10, 23, 21, 20, 31, TimeSpan.Zero);
            PartialReport pReport = pReportMgr.GetAgReports("Dummy").FGetReport(testTimeOffset);
            PartialReport expected = new PartialReport()
            {
                StartTime = testTimeOffset,
                EndTime = testTimeOffset,
                MessageSet = new HashSet<string>() { "1135", "1177", "1146" }
            };

            _clusterLogParser.ParseLog(testLogPath, "ze-vm001");

            Assert.IsTrue(expected.Equals(pReport));
        }

    }
}
