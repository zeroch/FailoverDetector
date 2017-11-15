using System;
using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetectorTests
{
    [TestClass()]
    public class ApplicationLogParserTests
    {
        readonly string _testString = @"9/19/2017 10:30:08 AM   Information MSSQL$SQL16RTM01       The state of the local availability replica in availability group 'ag8102017' has changed from   'RESOLVING_NORMAL' to 'PRIMARY_PENDING'.  The state changed because the availability group is coming online.  For more information, see the SQL Server error log, Windows Server Failover Clustering (WSFC) management console, or WSFC log.";
        ApplicationLogParser _appLogParser;
        [TestInitialize]
        public void Setup()
        {
            _appLogParser = new ApplicationLogParser();

        }

        [TestMethod()]
        public void TokenizeTimestampTest()
        {
            string retTime = _appLogParser.TokenizeTimestamp(_testString);
            Console.WriteLine("Test Time stamp token: {0}", retTime);
            Assert.AreEqual(retTime, "9/19/2017 10:30:08 AM");
        }

        [TestMethod()]
        public void ParseTimeStampTest()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 19, 10, 30, 8, new TimeSpan(-4, 0, 0));

            string retTime = _appLogParser.TokenizeTimestamp(_testString);
            DateTimeOffset parsedTime = _appLogParser.ParseTimeStamp(retTime);
            Console.WriteLine("Parse string timestamp: {0} to DateTimeOffset: {1}", retTime, parsedTime.ToString());
            DateTimeOffset utcTime = cmp.ToUniversalTime();
            Assert.AreEqual(utcTime, parsedTime);
        }

        [TestMethod()]
        public void TokenizeEntryTypeTest()
        {
            string retType = _appLogParser.TokenizeEntryType(_testString);
            Console.WriteLine("Test Message Type: {0}", retType);
            Assert.AreEqual(retType, "Information");
        }

        [TestMethod()]
        public void TokenizeSqlSourceTest()
        {
            string retSource = _appLogParser.TokenizeSqlSource(_testString);
            Console.WriteLine("Test Application Log Source: {0}", retSource);
            Assert.AreEqual(retSource, "MSSQL$SQL16RTM01");
        }

        [TestMethod()]
        public void ParseLogEntryTest()
        {
            ErrorLogEntry ret = _appLogParser.ParseLogEntry(_testString);
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 19, 10, 30, 8, new TimeSpan(-4, 0, 0));
            ErrorLogEntry test = new ErrorLogEntry(cmp, @"", @"The state of the local availability replica in availability group 'ag8102017' has changed from   'RESOLVING_NORMAL' to 'PRIMARY_PENDING'.  The state changed because the availability group is coming online.  For more information, see the SQL Server error log, Windows Server Failover Clustering (WSFC) management console, or WSFC log.");
            Assert.IsTrue(ret.Equals(test));
        }


    }
}