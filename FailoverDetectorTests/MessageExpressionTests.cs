using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector.Utils;
namespace FailoverDetector.Tests
{
    [TestClass()]
    public class MessageExpressionTest
    {
        // focus on test IsMatch function.
        private List<MessageExpression> regexList;

        private List<string> TestStringList;
        [TestInitialize]
        public void Setup()
        {
            regexList = new List<MessageExpression>()
            {
                new StopSqlServiceExpression(),
                new ShutdownServerExpression(),
                new StateTransitionExpression(),
                new LeaseExpiredExpression(),
                new LeaseTimeoutExpression(),
                new LeaseRenewFailedExpression(),
                new LeaseFailedToSleepExpression(),
                new ClusterHaltExpression(),
                new ResourceFailedExpression(),
                new NodeOfflineExpression(),
                new LostQuorumExpression(),
                new ClusterOfflineExpression(),
                new FailoverExpression(),
                new RhsTerminatedExpression()
            };
            TestStringList = new List<string>()
            {
                @"SQL Server is terminating in response to a 'stop' request from Service Control Manager. This is an informational message only. No user action is required. SQL Server is terminating because of a system shutdown. This is an informational message only. No user action is required. The state of the local availability replica in availability group 'ag1023' has changed from 'RESOLVING_NORMAL' to 'SECONDARY_NORMAL'. The state changed because the availability group state has changed in Windows Server Filover Clustering (WSFC).  For more information, see the SQL Server error log or cluster log.  If this is a Windows Server Failover Clustering (WSFC) availability group, you can also see the WSFC management console.",
                @"SQL Server is terminating because of a system shutdown. This is an informational message only. No user action is required.",
                @"The state of the local availability replica in availability group 'ag1023' has changed from 'RESOLVING_NORMAL' to 'SECONDARY_NORMAL'.  The state changed because the availability group state has changed in Windows Server Failover Clustering (WSFC).  For more information, see the SQL Server error log or cluster log. If this is a Windows Server Failover Clustering (WSFC) availability group, you can also see the WSFC management console.",
                @"The lease between availability group 'ag1023' and the Windows Server Failover Cluster has expired. A connectivity issue occurred between the instance of SQL Server and the Windows Server Failover Cluster. To determine whether the availability group is failing over correctly, check the corresponding availability group resource in the Windows Server Failover Cluster.",
                @"Windows Server Failover Cluster did not receive a process event signal from SQL Server hosting availability group 'ag1023' within the lease timeout period.",
                @"The renewal of the lease between availability group 'ag1023' and the Windows Server Failover Cluster failed because SQL Server encountered Windows error with error code ('%d').",
                @"The lease of availability group 'ag1023' lease is no longer valid to start the lease renewal process.",
                @"Cluster service was halted due to incomplete connectivity with other cluster nodes.",
                @"Cluster resource 'IPv4 Static Address 1 (Cluster Group)' in clustered service or application 'Cluster Group' failed.",
                @"Cluster node 'ze-2016-v2' was removed from the active failover cluster membership. The Cluster service on this node may have stopped. This could also be due to the node having lost communication with other active nodes in the failover cluster. Run the Validate a Configuration wizard to check your network configuration. If the condition persists, check for hardware or software errors related to the network adapters on this node. Also check for failures in any other network components to which the node is connected such as hubs, switches, or bridges.",
                @"The Cluster service is shutting down because quorum was lost. This could be due to the loss of network connectivity between some or all nodes in the cluster, or a failover of the witness disk. 156 Run the Validate a Configuration wizard to check your network configuration. If the condition persists, check for hardware or software errors related to the network adapter. Also check for failures in any other network components to which the node is connected such as hubs, switches, or bridges.",
                @"The Cluster service failed to bring clustered role 'ag1023' completely online or offline. One or more resources may be in a failed state. This may impact the availability of the clustered role.",
                @"The Cluster service is attempting to fail over the clustered role 'ag1023' from node 'ze-2016-v1' to node 'ze-2016-v2'.",
                @"The cluster Resource Hosting Subsystem (RHS) process was terminated and will be restarted. This is typically associated with cluster health detection and recovery of a resource. Refer to the System event log to determine which resource and resource DLL is causing the issue."
            };

        }

        [TestMethod]
        public void ExpressionMatchTest()
        {
            for (int i = 0; i < regexList.Count; i++)
            {
                Assert.IsTrue(regexList[i].IsMatch(TestStringList[i]));
            }
        }
    }

}