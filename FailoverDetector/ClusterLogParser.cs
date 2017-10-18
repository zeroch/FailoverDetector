﻿using System;
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
        }

        List<ErrorLogEntry> entryList;
        private Regex rxTimeStamp = new Regex(@"\d{4}/\d{2}/\d{2}-\d{2}:\d{2}:\d{2}.\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxPid = new Regex(@"[0-9a-f]{8}.[0-9a-f]{8}::", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxEntryType = new Regex(@"ERR|INFO|warn");
        private Regex rxBrackets = new Regex(@"\[.*?\]");

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
            line = line.Substring(tmpTimestamp.Length).Trim();

            string tmpType = TokenizeEntryType(line);
            line = line.Substring(tmpType.Length).Trim();

            string tmpChannel = TokenizeChannel(line);
            line = line.Substring(tmpChannel.Length ).Trim();

            ErrorLogEntry entry = new ErrorLogEntry(tmpTimestamp, tmpPid, line);
            return entry;

        }

    }
}
