using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;


namespace FailoverDetector
{

    // Cluster Log has following structure
    // [PID]{.}[threadID]{::}[timestamp]{/space}[entry type]{/double space}[component]{/space}[message]
    public class ClusterLogParser : LogParser
    {
        public ClusterLogParser()
        {
            // TODO: come back later
            entryList = new List<ErrorLogEntry>();
            UTCcorrection = new TimeSpan(4, 0, 0);
        }

        public override void SetupRegexList()
        {
            throw new NotImplementedException();
        }

        TimeSpan UTCcorrection;
        List<ErrorLogEntry> entryList;
        private Regex rxTimeStamp = new Regex(@"\d{4}/\d{2}/\d{2}-\d{2}:\d{2}:\d{2}.\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxPid = new Regex(@"[0-9a-f]{8}.[0-9a-f]{8}::", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxEntryType = new Regex(@"ERR|INFO|warn");
        private Regex rxBrackets = new Regex(@"\[.*?\]");

        // cluster log 1006
        private Regex rxClusterHalt = new Regex(@"Cluster service was halted due to incomplete connectivity with other cluster nodes");
        // cluster log 1069
        private Regex rxResourceFailed = new Regex(@"Cluster resource(.*)in clustered service or application(.*)failed");

        // Cluster log Node Offline, 1135
        private Regex rxNodeOffline = new Regex(@"(Cluster node)(.*)(was removed from the active failover cluster membership)");

        // cluster log 1177
        private Regex rxLossQuorum = new Regex(@"The Cluster service is shutting down because quorum was lost");

        // cluster log 1205
        private Regex rxClusterOffline = new Regex(@"The Cluster service failed to bring clustered role(.*)completely online or offline");

        // failover
        private Regex rxFailover = new Regex(@"The Cluster service is attempting to fail over the clustered role(.*)from node(.*)to node (.*)");

        // RHS terminated
        private Regex rxRHSTerminated = new Regex(@"The cluster Resource Hosting Subsystem \(RHS\) process was terminated and will be restarted");

        // methods
        public override string TokenizeTimestamp(string line)
        {
            // 2017-10-11 11:04:12.32
            string tmp = string.Empty;
            if (this.rxTimeStamp.IsMatch(line))
            {
                string timeStamp = this.rxTimeStamp.Match(line).Value;
                // based on regex rules, we crop timestamp for 22 chars
                // TODO: don't remove timestamp at here. we need to return timestamp first.
                //tmp = line.Substring(22).Trim();
                tmp = timeStamp;
            }
            return tmp;
        }
        public override DateTimeOffset ParseTimeStamp(string timestamp)
        {
            // timestamp have a special 23.5    we need to remove .x value from string
            // FIXME: a hack to format timestamp string
            timestamp = timestamp.Split('.')[0].Replace('-', ' ');
            DateTimeOffset parsedTime;
            DateTimeOffset.TryParse(timestamp, null as IFormatProvider,
                                System.Globalization.DateTimeStyles.AssumeUniversal, out parsedTime);
            parsedTime += UTCcorrection;
            return parsedTime;
        }

        // in the comming analysis, PID and Thread ID really doesn't matter
        // so we tokenize them together here, if there is no PID and TID,
        // this message must trucated
        public string TokenizePidTid(string line)
        {
            string tmp = string.Empty;
            if (this.rxPid.IsMatch(line))
            {
                tmp = this.rxPid.Match(line).Value;
            }
            return tmp;
        }

        public string TokenizeEntryType(string line)
        {
            string tmp = string.Empty;
            if (this.rxEntryType.IsMatch(line))
            {
                tmp = this.rxEntryType.Match(line).Value;
            }
            return tmp;
        }

        public string TokenizeChannel(string line)
        {
            string tmp = string.Empty;
            if (this.rxBrackets.IsMatch(line))
            {
                //  this is not exactly right. we need to match first string inside the brackets
                tmp = this.rxBrackets.Match(line).Value;
            }
            return tmp;
        }

        public override ErrorLogEntry ParseLogEntry(string line)
        {
            string tmpPid = TokenizePidTid(line);
            line = line.Substring(tmpPid.Length).Trim();

            string tmpTimestamp = TokenizeTimestamp(line);
            DateTimeOffset tmpParsedTime = ParseTimeStamp(tmpTimestamp);
            line = line.Substring(tmpTimestamp.Length).Trim();

            string tmpType = TokenizeEntryType(line);
            line = line.Substring(tmpType.Length).Trim();

            string tmpChannel = TokenizeChannel(line);
            line = line.Substring(tmpChannel.Length ).Trim();

            ErrorLogEntry entry = new ErrorLogEntry(tmpParsedTime, tmpPid, line);
            return entry;

        }

        public bool MatchClusterHalt(string msg)
        {
            return this.rxClusterHalt.IsMatch(msg);
        }

        public bool MatchResourceFailed(string msg)
        {
            return this.rxResourceFailed.IsMatch(msg);
        }

        public bool MatchNodeOffline(string msg)
        {
            return this.rxNodeOffline.IsMatch(msg);
        }

        public bool MatchLossQuorum(string msg)
        {
            return this.rxLossQuorum.IsMatch(msg);
        }

        public bool MatchClusterOffline(string msg)
        {
            return this.rxClusterOffline.IsMatch(msg);
        }

        public bool MatchFailover(string msg)
        {
            return this.rxFailover.IsMatch(msg);
        }

        public bool MatchRHSTerminated(string msg)
        {
            return this.rxRHSTerminated.IsMatch(msg);
        }
    }
}
