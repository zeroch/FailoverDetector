using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;


namespace FailoverDetector.Tests
{
    [TestClass]
    public class TestClusterLogParser
    {
        ClusterLogParser clusterLogParser;
        [TestInitialize]
        public void Setup()
        {
            clusterLogParser = new ClusterLogParser();

        }
        [TestMethod]
        public void TestTokenizeTimestamp()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retTime = clusterLogParser.TokenizeTimestamp(testString);
            Console.WriteLine("Test Time stamp token: {0}", retTime);
            Assert.AreEqual(retTime, "2017/09/14-18:16:27.575");
        }
        [TestMethod]
        public void TestParseTimeStamp()
        {
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 14, 18, 16, 27, new TimeSpan(-4, 0, 0));

            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retTime = clusterLogParser.TokenizeTimestamp(testString);
            DateTimeOffset parsedTime = clusterLogParser.ParseTimeStamp(retTime);
            Console.WriteLine("Parse string timestamp: {0} to DateTimeOffset: {1}", retTime, parsedTime.ToString());
            DateTimeOffset utcTime = cmp.ToUniversalTime();
            Assert.AreEqual(utcTime, parsedTime);
        }
        [TestMethod]
        public void TestTokenizePid()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retPid = clusterLogParser.TokenizePidTid(testString);
            Console.WriteLine("Test PID and thread ID: {0}", retPid);
            Assert.AreEqual(retPid, "0000126c.00001cec::");
        }

        [TestMethod]
        public void TestTokenizeEntryType()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retType = clusterLogParser.TokenizeEntryType(testString);
            Console.WriteLine("Test Entry Type: {0}", retType);
            Assert.AreEqual(retType, "ERR");

        }
        [TestMethod]
        public void TestTokenizeChannel()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            string retChannel = clusterLogParser.TokenizeChannel(testString);
            Console.WriteLine("Test Channel: {0}", retChannel);
            Assert.AreEqual(retChannel, "[RES]");
        }
        [TestMethod]
        public void TestClusterParseLog()
        {
            string testString = @"0000126c.00001cec::2017/09/14-18:16:27.575 ERR   [RES] SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive";
            ErrorLogEntry ret = clusterLogParser.ParseLogEntry(testString);
            DateTimeOffset cmp = new DateTimeOffset(2017, 9, 14, 18, 16, 27, new TimeSpan(-4, 0, 0));
            ErrorLogEntry test = new ErrorLogEntry(cmp, @"0000126c.00001cec::", @"SQL Server Availability Group <ag8102017>: [hadrag] SQL server service is not alive");
            Assert.IsTrue(ret.Equals(test));
        }

        [TestMethod]
        public void TestClusterHalt()
        {
            string testString = @"Cluster service was halted due to incomplete connectivity with other cluster nodes.";
            Assert.IsTrue(clusterLogParser.MatchClusterHalt(testString));
        }

        [TestMethod()]
        public void MatchResourceFailedTest()
        {
            string testString =
                @"Cluster resource 'IPv4 Static Address 1 (Cluster Group)' in clustered service or application 'Cluster Group' failed.";
            Assert.IsTrue(clusterLogParser.MatchResourceFailed(testString));
        }

        [TestMethod()]
        public void MatchNodeOfflineTest()
        {
            string testString =
                @"Cluster node 'ze-2016-v2' was removed from the active failover cluster membership. The Cluster service on this node may have stopped. This could also be due to the node having lost communication with other active nodes in the failover cluster. Run the Validate a Configuration wizard to check your network configuration. If the condition persists, check for hardware or software errors related to the network adapters on this node. Also check for failures in any other network components to which the node is connected such as hubs, switches, or bridges.";
            Assert.IsTrue(clusterLogParser.MatchNodeOffline(testString));
        }

        [TestMethod()]
        public void MatchLossQuorumTest()
        {
            string testString =
                @"The Cluster service is shutting down because quorum was lost. This could be due to the loss of network connectivity between some or all nodes in the cluster, or a failover of the witness disk. 156 Run the Validate a Configuration wizard to check your network configuration. If the condition persists, check for hardware or software errors related to the network adapter. Also check for failures in any other network components to which the node is connected such as hubs, switches, or bridges.";
            Assert.IsTrue(clusterLogParser.MatchLossQuorum(testString));
        }

        [TestMethod()]
        public void MatchClusterOfflineTest()
        {
            string testString =
                @"The Cluster service failed to bring clustered role 'ag1023' completely online or offline. One or more resources may be in a failed state. This may impact the availability of the clustered role.";
            Assert.IsTrue(clusterLogParser.MatchClusterOffline(testString));
        }

        [TestMethod()]
        public void MatchFailoverTest()
        {
            string testString =
                @"The Cluster service is attempting to fail over the clustered role 'ag1023' from node 'ze-2016-v1' to node 'ze-2016-v2'.";
            Assert.IsTrue(clusterLogParser.MatchFailover(testString));
        }

        [TestMethod()]
        public void MatchRhsTerminatedTest()
        {
            string testString =
                @"The cluster Resource Hosting Subsystem (RHS) process was terminated and will be restarted. This is typically associated with cluster health detection and recovery of a resource. Refer to the System event log to determine which resource and resource DLL is causing the issue.";
            Assert.IsTrue(clusterLogParser.MatchRHSTerminated(testString));
        }
    }
}
