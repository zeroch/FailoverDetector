using System;
using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace FailoverDetectorTests
{
    [TestClass()]
    public class SystemLogParserTests
    {
        readonly string _testString = @"The SQL Server (SQL17RTM01) service terminated unexpectedly.  It has done this 2 time(s).";


        SystemLogParser _appLogParser;
        private readonly string baseTestPath =
    @"SystemLog\";
        [TestInitialize]
        public void Setup()
        {
            _appLogParser = new SystemLogParser();

        }

        [TestMethod()]
        public void ParseTimeStampTest()
        {
            DateTimeOffset expected = new DateTimeOffset(2017, 12, 10, 2, 58, 54, new TimeSpan(0, 0, 0));

            string retTime = "12/10/2017 2:58:54 AM";
            DateTimeOffset parsedTime = _appLogParser.ParseTimeStamp(retTime);
            Console.WriteLine("Parse string timestamp: {0} to DateTimeOffset: {1}", retTime, parsedTime.ToString());
            DateTimeOffset utcTime = expected.ToUniversalTime();
            Assert.AreEqual(utcTime, parsedTime);
        }

        [TestMethod()]
        public void ParseLogEntryTest()
        {
            string[] testString = {
                "7034","ze-2016-v1.redmond.corp.microsoft.com","System.Byte[]","71060","(0)","0","Error","The SQL Server (SQL17RTM01) service terminated unexpectedly.  It has done this 2 time(s).","Service Control Manager","System.String[]","3221232506","12/10/2017 2:58:54 AM","12/10/2017 2:58:54 AM","","",
            };
            DateTimeOffset tTime = new DateTimeOffset(2017, 12, 10, 2, 58, 54, new TimeSpan(0, 0, 0));
            ErrorLogEntry expected = new ErrorLogEntry()
            {
                Timestamp = tTime,
                Message = "The SQL Server (SQL17RTM01) service terminated unexpectedly.  It has done this 2 time(s).",
                Spid = "7034",
                RawMessage = "12/10/2017 2:58:54 AM\t7034\tThe SQL Server (SQL17RTM01) service terminated unexpectedly.  It has done this 2 time(s)."
            };
            ErrorLogEntry actual = _appLogParser.ParseLogEntry(testString);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\SystemLog", "SystemLog")]
        public void ParseSystemLogCrashTest()
        {
            string testLogPath = baseTestPath + "TestCase_0.csv";
            // prepare environment report object
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            // Create a fake report
            pReportMgr.AddNewAgReport("Dummy", "ze-vm001");
            DateTimeOffset testTimeOffset = new DateTimeOffset(2017, 12, 10, 2, 58, 54, TimeSpan.Zero);
            PartialReport pReport = pReportMgr.GetAgReports("Dummy").FGetReport(testTimeOffset);
            PartialReport expected = new PartialReport()
            {
                StartTime = testTimeOffset,
                EndTime = testTimeOffset,
                MessageSet = new HashSet<string>() { "Crash" }
            };

            _appLogParser.ParseLog(testLogPath, "ze-vm001");

            Assert.IsTrue(expected.Equals(pReport));

        }
    }
}