using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;


namespace FailoverDetectorTests
{
    [TestClass]
    public class TestClusterLogParser
    {
        string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
        ClusterLogParser clusterLogParser;
        [TestInitialize]
        public void Setup()
        {
            clusterLogParser = new ClusterLogParser();

        }
        [TestMethod]
        public void TestTokenizeTimestamp()
        {
            string retTime = clusterLogParser.TokenizeTimestamp(testString);
            Console.WriteLine("Test Time stamp token: {0}", retTime);
            Assert.AreEqual(retTime, "2017/09/14-18:16:27.575");
        }
        [TestMethod]
        public void TestTokenizePid()
        {
            string retPid = clusterLogParser.TokenizePidTid(testString);
            Console.WriteLine("Test PID and thread ID: {0}", retPid);
            Assert.AreEqual(retPid, "0000126c.00001cec::");
        }

        [TestMethod]
        public void TestTokenizeEntryType()
        {
            string retType = clusterLogParser.TokenizeEntryType(testString);
            Console.WriteLine("Test Entry Type: {0}", retType);
            Assert.AreEqual(retType, "ERR");

        }
        [TestMethod]
        public void TestTokenizeChannel()
        {
            string retChannel = clusterLogParser.TokenizeChannel(testString);
            Console.WriteLine("Test Channel: {0}", retChannel);
            Assert.AreEqual(retChannel, "[RES]");
        }
        [TestMethod]
        public void TestClusterParseLog()
        {
            ErrorLogEntry ret = clusterLogParser.ParseLogEntry(testString);
            ErrorLogEntry test = new ErrorLogEntry(@"2017/09/14-18:16:27.575", @"0000126c.00001cec::", @"SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive");
            Assert.IsTrue(ret.Equals(test));
        }
    }
}
