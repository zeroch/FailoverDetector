using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FailoverDetector.Utils;

namespace FailoverDetector
{

    public abstract class LogParser
    {
        private DateTimeOffset _failoverTimeStart;
        private DateTimeOffset _failoverTimeEnd;
        protected List<MessageExpression> _logParserList;
        protected Constants.SourceType sourceType;
        protected UTCCorrectionExpression pUTCCorrection;

        public TimeSpan FGetUTCTimeZone()
        {
            if (utcCorrectionFound)
            {
                return _utCcorrection;
            }else
            {
                return new TimeSpan(0, 0, 0);
            }
        }
        // UTC correction should only used at system log and error log
        protected TimeSpan _utCcorrection { get; set; }
        protected bool utcCorrectionFound { get; set; }

        protected LogParser()
        {
            _utCcorrection = new TimeSpan(0, 0, 0);
            pUTCCorrection = new UTCCorrectionExpression();
        }

        public void SetTargetFailoverTime(DateTimeOffset start, DateTimeOffset end)
        {
            _failoverTimeStart = start;
            _failoverTimeEnd = end;
        }

        public abstract string TokenizeTimestamp(string line);
        public abstract DateTimeOffset ParseTimeStamp(string timestamp);
        public abstract ErrorLogEntry ParseLogEntry(string entry);
        public abstract void SetupRegexList();
        public void ParseLog(string logFilePath, string instanceName)
        {
            try
            {
                using (FileStream stream = File.OpenRead(logFilePath))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line = reader.ReadLine();
                        ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;

                        // I need an interator that I don't care which AG it is
                        // only iterate through by time.
                        //pReportMgr.
                        IEnumerator ReportIterator = pReportMgr.ReportVisitor();
                        if (!ReportIterator.MoveNext())
                            return;
                        while (line != null && ReportIterator.Current != null)
                        {

                            var pEntry = ParseLogEntry(line);
                            if (pEntry.IsEmpty())
                            {
                                line = reader.ReadLine();
                                continue;

                            }
                            if (pEntry.IsTrancated())
                            {
                                line = reader.ReadLine();
                                continue;
                                // dont use and access date
                                // append message with last one I think. 
                                // trancated is a special case in our problem.
                            }
                            // fix UTC correction issue is not relevent with timestamp
                            if (!utcCorrectionFound)
                            {
                                if (pUTCCorrection.IsMatch(pEntry.Message))
                                {
                                    utcCorrectionFound = true;
                                    // parse time 
                                    _utCcorrection = pUTCCorrection.HandleOnceMatch(pEntry);
                                }
                            }


                            DateTimeOffset messageTime = pEntry.Timestamp;
                            // compare time
                            PartialReport reportInstance = (PartialReport)ReportIterator.Current;

                            if (messageTime < (reportInstance.StartTime.AddMinutes(-1 * Constants.DefaultInterval)))
                            {
                                line = reader.ReadLine();
                                continue;
                            }
                            else if (messageTime > reportInstance.EndTime.AddMinutes(Constants.DefaultInterval))
                            {
                                if (!ReportIterator.MoveNext())
                                    break;
                                else
                                    continue;
                            }
                            else
                            {
                                // now                         
                                //if (pEntry.Timestamp < sometime upper bound
                                //    && pEntry.Timestamp > sometime lower bound)
                                // parse message, search the pattern we will use. 
                                foreach (var regexParser in _logParserList)
                                {
                                    if (regexParser.IsMatch(pEntry.Message))
                                    {
                                        regexParser.HandleOnceMatch(instanceName, pEntry, reportInstance);

                                    }
                                }
                                line = reader.ReadLine();
                            }

                        }
                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {

                Console.WriteLine(e.Message);
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }
    }

    public class ErrorLogEntry
    {
        public string Message { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string Spid { get; set; }

        public string RawMessage { get; set; }

        public ErrorLogEntry()
        {
            Message = String.Empty;
            RawMessage = String.Empty;
            Spid = String.Empty;
            Timestamp = DateTimeOffset.MinValue;
        }

        public ErrorLogEntry(DateTimeOffset pTimestamp, string pSpid, string pMessage)
        {
            Timestamp = pTimestamp;
            Spid = pSpid;
            Message = pMessage;
            RawMessage = String.Empty;

        }

        public override bool Equals(object obj)
        {
            if (obj is ErrorLogEntry)
                return Equals(obj as ErrorLogEntry);

            return base.Equals(obj);
        }

        public bool Equals(ErrorLogEntry logEntry)
        {
            return String.Equals(Timestamp, logEntry.Timestamp)
                   && String.Equals(Spid, logEntry.Spid)
                   && String.Equals(Message, logEntry.Message)
                   && String.Equals(RawMessage, logEntry.RawMessage);
        }

        // if a log entry doesn't has timestamp, then it is a tracated message from last message
        public bool IsTrancated()

        {
            return Timestamp == DateTimeOffset.MinValue;
        }

        public bool IsEmpty()
        {
            return (Timestamp == DateTimeOffset.MinValue)
                   && String.IsNullOrWhiteSpace(Spid)
                   && String.IsNullOrWhiteSpace(Message);
        }



    }

    public class ErrorLogParser : LogParser
    {

        public ErrorLogParser()
        {
            sourceType = Constants.SourceType.ErrorLog;
            utcCorrectionFound = false;
            SetupRegexList();
        }

        public override void SetupRegexList()
        {
            _logParserList = new List<MessageExpression>()
            {
                new StopSqlServiceExpression(),
                new ShutdownServerExpression(),
                new StateTransitionExpression(),
                new LeaseExpiredExpression(),
                new LeaseTimeoutExpression(),
                new LeaseRenewFailedExpression(),
                new LeaseFailedToSleepExpression(),
                new GenerateDumpExpression()
            };
            // match UTC adjustment so we can convert all time to UTC time

    }

        private readonly Regex _rxTimeStamp = new Regex(@"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{2}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Regex _rxSpid =
            new Regex(@"spid[0-9]{1,5}[a-z]{0,2}|LOGON|Backup", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // if we match current message is 19406, we tokenize value from '%ls'
        private readonly Regex _rxStringInQuote = new Regex(@"\'\w+\'");

        // match any character that beginning of a string and end with a dot '.'
        private readonly Regex _rxFirstSentence = new Regex(@"^([^.]*)\.");



        // methods
        public override string TokenizeTimestamp(string line)
        {
            // 2017-10-11 11:04:12.32
            var tmp = string.Empty;
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


        // parse a string timestamp to a dateTimeOffset, so we can compare
        // it with failover time

        public override DateTimeOffset ParseTimeStamp(string timestamp)
        {
            // timestamp have a special 23.5    we need to remove .x value from string
            string[] substr = timestamp.Split('.');
            DateTimeOffset.TryParse(substr[0], null as IFormatProvider,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedTime);
            parsedTime += _utCcorrection;
            return parsedTime;
        }

        // we expected input string is after tokenize timestamp
        // typical result is spid* or LOGON
        // TODO: not sure are we expecting a empty result. 
        public string TokenizeSpid(string line)
        {
            string tmp = string.Empty;
            if (_rxSpid.IsMatch(line))
            {
                tmp = _rxSpid.Match(line).Value;
            }
            return tmp;
        }

        // change a text log entry into a struct format
        public override ErrorLogEntry ParseLogEntry(string line)
        {
            ErrorLogEntry entry = new ErrorLogEntry();

            entry.RawMessage = line;

            string tmpTimeStamp = TokenizeTimestamp(line);

            line = line.Substring(tmpTimeStamp.Length).Trim();
            string tmpSpid = TokenizeSpid(line);

            string tmpMessage = line.Substring(tmpSpid.Length).Trim();

            if (!String.IsNullOrWhiteSpace(tmpTimeStamp))
            {
                entry.Timestamp = ParseTimeStamp(tmpTimeStamp);
            }
            if (!String.IsNullOrWhiteSpace(tmpSpid))
            {
                entry.Spid = tmpSpid;
            }
            if (!String.IsNullOrWhiteSpace(tmpMessage))
            {
                entry.Message = tmpMessage;
            }

            return entry;
        }
        public new void ParseLog(string logFilePath, string instanceName)
        {
            try
            {
                using (FileStream stream = File.OpenRead(logFilePath))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line = reader.ReadLine();
                        ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;

                        while (line != null)
                        {

                            var pEntry = ParseLogEntry(line);
                            if (pEntry.IsEmpty())
                            {
                                line = reader.ReadLine();
                                continue;

                            }
                            if (pEntry.IsTrancated())
                            {
                                line = reader.ReadLine();
                                continue;
                                // dont use and access date
                                // append message with last one I think. 
                                // trancated is a special case in our problem.
                            }

                            // this line is special for system
                            // fix UTC correction issue is not relevent with timestamp
                            if (!utcCorrectionFound)
                            {
                                if (pUTCCorrection.IsMatch(pEntry.Message))
                                {
                                    utcCorrectionFound = true;
                                    // parse time 
                                    _utCcorrection = pUTCCorrection.HandleOnceMatch(pEntry);
                                }
                            }
                            foreach (var regexParser in _logParserList)
                            {
                                if (regexParser.IsMatch(pEntry.Message))
                                {
                                    // scanning all reports to find a most match report
                                    regexParser.HandleOnceMatch(instanceName, pEntry);
                                }
                            }
                            line = reader.ReadLine();
                        }

                    }
                }
            }
            catch (DirectoryNotFoundException e)
            {

                Console.WriteLine(e.Message);
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }
    }


}
