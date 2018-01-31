using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FailoverDetector.Utils;


namespace FailoverDetector
{

    // Cluster Log has following structure
    // [PID]{.}[threadID]{::}[timestamp]{/space}[entry type]{/double space}[component]{/space}[message]
    public class ClusterLogParser : LogParser
    {
        public ClusterLogParser()
        {
            // Cluster log always read as UTC time
            _utCcorrection = new TimeSpan(0, 0, 0);
            sourceType = Constants.SourceType.ClusterLog;
            SetupRegexList();
            startToReadSystem = false;
        }

        public override void SetupRegexList()
        {
            _logParserList = new List<MessageExpression>()
            {
                new ClusterHaltExpression(),        // 1006
                new ResourceFailedExpression(),     // 1069
                new NodeOfflineExpression(),        // 1135
                new LostQuorumExpression(),         // 1177
                new ClusterOfflineExpression(),     // 1205
                new FailoverExpression(),           // Failover
                new RhsTerminatedExpression()       // 1146
            };
        }

        readonly TimeSpan _utCcorrection;
        private readonly Regex _rxTimeStamp = new Regex(@"\d{4}/\d{2}/\d{2}-\d{2}:\d{2}:\d{2}.\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _rxPid = new Regex(@"[0-9a-f]{8}.[0-9a-f]{8}::", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _rxEntryType = new Regex(@"ERR|INFO|warn|DBG");
        private readonly Regex _rxBrackets = new Regex(@"\[.*?\]");


        // methods
        public override string TokenizeTimestamp(string line)
        {
            // 2017-10-11 11:04:12.32
            string tmp = string.Empty;
            if (_rxTimeStamp.IsMatch(line))
            {
                string timeStamp = _rxTimeStamp.Match(line).Value;
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
            DateTimeOffset.TryParse(timestamp, null as IFormatProvider,
                                System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedTime);
            parsedTime += _utCcorrection;
            return parsedTime;
        }

        // in the comming analysis, PID and Thread ID really doesn't matter
        // so we tokenize them together here, if there is no PID and TID,
        // this message must trucated
        public string TokenizePidTid(string line)
        {
            string tmp = string.Empty;
            if (_rxPid.IsMatch(line))
            {
                tmp = _rxPid.Match(line).Value;
            }
            return tmp;
        }

        public string TokenizeEntryType(string line)
        {
            string tmp = string.Empty;
            if (_rxEntryType.IsMatch(line))
            {
                tmp = _rxEntryType.Match(line).Value;
            }
            return tmp;
        }

        public string TokenizeChannel(string line)
        {
            string tmp = string.Empty;
            if (_rxBrackets.IsMatch(line) && _rxBrackets.Match(line).Index == 0)
            {
                //  this is not exactly right. we need to match first string inside the brackets
                tmp = _rxBrackets.Match(line).Value;
            }
            return tmp;
        }

        public override ErrorLogEntry ParseLogEntry(string line)
        {
            string rawLine = line;
            // channel could be at the beginning or after timestamp
            string tmpChannel = TokenizeChannel(line);
            line = line.Substring(tmpChannel.Length).Trim();

            string tmpPid = TokenizePidTid(line);
            line = line.Substring(tmpPid.Length).Trim();

            string tmpTimestamp = TokenizeTimestamp(line);
            DateTimeOffset tmpParsedTime = ParseTimeStamp(tmpTimestamp);
            line = line.Substring(tmpTimestamp.Length).Trim();

            string tmpType = TokenizeEntryType(line);
            line = line.Substring(tmpType.Length).Trim();

            if (tmpChannel == string.Empty)
            {
                tmpChannel = TokenizeChannel(line);
                line = line.Substring(tmpChannel.Length).Trim();
            }

            ErrorLogEntry entry = new ErrorLogEntry(tmpParsedTime, tmpPid, line);
            entry.RawMessage = rawLine;
            return entry;

        }

    }
}
