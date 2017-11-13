using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace FailoverDetector
{
    namespace Utils
    {

        public abstract class MessageExpression
        {
            protected Regex _Regex;
            // static pReport
            // TODO: replace with singlton GetReport() later
            protected static PartialReport PReport;
            protected readonly Regex RxStringInQuote = new Regex(@"\'\w+\'");
            protected readonly Regex RxFirstSentence = new Regex(@"^([^.]*)\.");

            protected MessageExpression(string pRegexPattern)
            {
                _Regex = new Regex(pRegexPattern);
            }
            protected MessageExpression() { }

            public bool IsMatch(string msg)
            {
                return _Regex.IsMatch(msg);
            }
            public abstract void HandleOnceMatch(string msg);

        }

        // match stop sqlservice and handle method
        public class StopSqlServiceExpression : MessageExpression
        {

            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("stopService");
            }

            public StopSqlServiceExpression()
            {
                _Regex = new Regex(
                    @"SQL Server is terminating in response to a 'stop' request from Service Control Manager");

            }
        }

        // match shutdown server and handle method
        public class ShutdownServerExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("ShutdownServer");
            }

            public ShutdownServerExpression()
            {
                _Regex = new Regex(@"SQL Server is terminating because of a system shutdown");
            }
        }

        // match State transition and handle
        public class StateTransitionExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report

                // capture 'ag_name', 'prev_state'  and 'current_state'
                if(RxStringInQuote.IsMatch(msg))
                {
                    // in this case, matches must equels to 3
                    MatchCollection mc = RxStringInQuote.Matches((msg));
                    if (mc.Count != 3)
                        return;
                    PReport.AgName = mc[0].Value;
                    PReport.AddRoleTransition(mc[1].Value);
                    PReport.AddRoleTransition(mc[2].Value);
                }
                
            }

            public StateTransitionExpression()
            {
                _Regex = new Regex(@"The state of the local availability replica in availability group");
            }
        }

        // match Lease Expired and handle
        public class LeaseExpiredExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("leaseExpire");
            }

            public LeaseExpiredExpression()
            {
                _Regex = new Regex(
                    @"(The lease between availability group)(.*)(and the Windows Server Failover Cluster has expired)");
            }
        }

        // Match Lease Timeout
        public class LeaseTimeoutExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("leaseTimeout");
            }

            public LeaseTimeoutExpression()
            {
                _Regex = new Regex(
                    @"(Windows Server Failover Cluster did not receive a process event signal from SQL Server hosting availability group)(.*)(within the lease timeout period.)");
            }
        }
        // Match Lease Renew Failed.
        public class LeaseRenewFailedExpression : MessageExpression
        {


            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("lease renew failed");
            }

            public LeaseRenewFailedExpression()
            {
                _Regex = new Regex(
                    @"(The renewal of the lease between availability group)(.*)(and the Windows Server Failover Cluster failed)");
            }
        }
        // Match LeaseFailedToSleep
        public class LeaseFailedToSleepExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("lease failed to sleep");
            }

            public LeaseFailedToSleepExpression()
            {
                _Regex = new Regex(
                    @"(The lease of availability group)(.*)(lease is no longer valid to start the lease renewal process)");
            }
        }

        //  cluster log 1006
        public class ClusterHaltExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("1006");
            }

            public ClusterHaltExpression()
            {
                _Regex = new Regex(@"Cluster service was halted due to incomplete connectivity with other cluster nodes");
            }
        }
        // cluster log 1069
        public class ResourceFailedExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("1069");
            }

            public ResourceFailedExpression()
            {
                _Regex = new Regex(@"Cluster resource(.*)in clustered service or application(.*)failed");
            }
        }
        // Cluster log Node Offline, 1135
        public class NodeOfflineExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("1135");
            }

            public NodeOfflineExpression()
            {
                _Regex = new Regex(@"(Cluster node)(.*)(was removed from the active failover cluster membership)");
            }
        }

        // cluster log 1177
        public class LostQuorumExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("1177");
            }

            public LostQuorumExpression()
            {
                _Regex = new Regex(@"The Cluster service is shutting down because quorum was lost");
            }
        }

        // cluster log 1205
        public class ClusterOfflineExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("1205");
            }

            public ClusterOfflineExpression()
            {
                _Regex = new Regex(@"The Cluster service failed to bring clustered role(.*)completely online or offline");
            }
        }
        public class FailoverExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("Failover");
            }

            public FailoverExpression()
            {
                _Regex = new Regex(@"The Cluster service is attempting to fail over the clustered role(.*)from node(.*)to node (.*)");
            }
        }


        // RHS terminated
        public class RhsTerminatedExpression : MessageExpression
        {
            public override void HandleOnceMatch(string msg)
            {
                // TODO
                // get current Partial Report
                // fill data into partial report
                PReport.MessageSet.Add("RHS Failed");
            }

            public RhsTerminatedExpression()
            {
                _Regex = new Regex(@"The cluster Resource Hosting Subsystem \(RHS\) process was terminated and will be restarted");
            }
        }
    }
}
