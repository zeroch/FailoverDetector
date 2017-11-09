using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;
using System.IO;


namespace FailoverDetector.Tests
{
    [TestClass]
    public class TestLogParser
    {
        string testString = @"2017-09-14 16:19:57.05 spid24s     Always On: The availability replica manager is going offline because the local Windows Server Failover Clustering (WSFC) node has lost quorum. This is an informational message only. No user action is required.";
        ErrorLogParser logParser;
        [TestInitialize]
        public void Setup()
        {
            logParser = new ErrorLogParser();
        }
        [TestMethod]
        public void TestTokenizeTimestamp()
        {
            string tmpTimeStamp = logParser.TokenizeTimestamp(testString);
            Console.WriteLine(tmpTimeStamp);
            Assert.AreEqual(tmpTimeStamp, "2017-09-14 16:19:57.05");
        }

        [TestMethod]
        public void TestParseTimeStamp()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 14, 16, 19, 57, new TimeSpan(-4,0,0));

            string retTime = logParser.TokenizeTimestamp(testString);
            DateTimeOffset parsedTime = logParser.ParseTimeStamp(retTime);
            Console.WriteLine("Parse string timestamp: {0} to DateTimeOffset: {1}", retTime, parsedTime.ToString());
            DateTimeOffset utcTime = cmp.ToUniversalTime();
            Assert.AreEqual(utcTime, parsedTime);
        }

        [TestMethod]
        public void TestParseLogEntry()
        {
            string testString = @"2017-09-10 22:00:00.12 spid191     UTC adjustment: -4:00";
            ErrorLogParser parser = new ErrorLogParser();
            ErrorLogEntry entry = new ErrorLogEntry();
            entry = parser.ParseLogEntry(testString);
            Console.WriteLine("Timestamp: {0}, spid: {1}, and message: {2}", entry.Timestamp, entry.Spid, entry.Message);

        }
        [TestMethod]
        public void TestErrorLogEntryEquals()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 10, 22, 00 ,00, new TimeSpan(-4,0,0));
            ErrorLogEntry pEntry = new ErrorLogEntry(cmp, "spid191", "UTC adjustment: -4:00");
            string testString = @"2017-09-10 22:00:00.19 spid191     UTC adjustment: -4:00";
            ErrorLogEntry entry = new ErrorLogEntry();
            ErrorLogParser parser = new ErrorLogParser();
            entry = parser.ParseLogEntry(testString);
            Assert.IsTrue(entry.Equals(pEntry));

        }

        [TestMethod]
        public void TestMatchErrorStopService()
        {
            ErrorLogParser parser = new ErrorLogParser();
            string testString = @"SQL Server is terminating in response to a 'stop' request from Service Control Manager. This is an informational message only. No user action is required";
            Assert.IsTrue(parser.MatchErrorStopService(testString));
        }

        [TestMethod]
        public void TestMatchServerKill()
        {
            string testString = @"SQL Server is terminating because of a system shutdown. This is an informational message only. No user action is required.";
            Assert.IsTrue(logParser.MatchErrorServerKill(testString));
        }
        [TestMethod]
        public void TestMatchUTCAdjust()
        {
            string testString = @"2017-09-10 22:00:00.19 spid191     UTC adjustment: -4:00";
            Assert.IsTrue(logParser.MatchUTCAdjust(testString));
        }

        [TestMethod]
        public void TestMatchStateTransition()
        {
            string testString = @"The state of the local availability replica in " +
                            "availability group 'ag1023' has changed from 'RESOLVING_NORMAL' to 'SECONDARY_NORMAL'.  " +
                            "The state changed because the availability group state has changed in Windows Server " +
                            "Failover Clustering (WSFC).  For more information, see the SQL Server error log or cluster log.  " +
                            "If this is a Windows Server Failover Clustering (WSFC) availability group, you can also see the " +
                            "WSFC management console.";
            Assert.IsTrue(logParser.MatchStateTransition(testString));
        }
       
    }
}
