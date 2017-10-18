using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FailoverDetector.Tests
{
    [TestClass()]
    public class ApplicationLogParserTests
    {
        string testString = @"9/19/2017 10:30:08 AM   Information MSSQL$SQL16RTM01       The state of the local availability replica in availability group 'ag8102017' has changed from   'RESOLVING_NORMAL' to 'PRIMARY_PENDING'.  The state changed because the availability group is coming online.  For more information, see the SQL Server error log, Windows Server Failover Clustering (WSFC) management console, or WSFC log.";
        ApplicationLogParser appLogParser;
        [TestInitialize]
        public void Setup()
        {
            appLogParser = new ApplicationLogParser();

        }

        [TestMethod()]
        public void TokenizeTimestampTest()
        {
            string retTime = appLogParser.TokenizeTimestamp(testString);
            Console.WriteLine("Test Time stamp token: {0}", retTime);
            Assert.AreEqual(retTime, "9/19/2017 10:30:08 AM");
        }

        [TestMethod()]
        public void TokenizeEntryTypeTest()
        {
            string retType = appLogParser.TokenizeEntryType(testString);
            Console.WriteLine("Test Message Type: {0}", retType);
            Assert.AreEqual(retType, "Information");
        }

        [TestMethod()]
        public void TokenizeSQLSourceTest()
        {
            string retSource = appLogParser.TokenizeSQLSource(testString);
            Console.WriteLine("Test Application Log Source: {0}", retSource);
            Assert.AreEqual(retSource, "MSSQL$SQL16RTM01");
        }

        [TestMethod()]
        public void ParseLogEntryTest()
        {
            ErrorLogEntry ret = appLogParser.ParseLogEntry(testString);
            ErrorLogEntry test = new ErrorLogEntry(@"9/19/2017 10:30:08 AM", @"", @"The state of the local availability replica in availability group 'ag8102017' has changed from   'RESOLVING_NORMAL' to 'PRIMARY_PENDING'.  The state changed because the availability group is coming online.  For more information, see the SQL Server error log, Windows Server Failover Clustering (WSFC) management console, or WSFC log.");
            Assert.IsTrue(ret.Equals(test));
        }
    }
}