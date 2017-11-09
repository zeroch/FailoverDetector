using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace FailoverDetector
{
    public abstract class LogParser
    {
        private DateTimeOffset FailoverTimeStart;
        private DateTimeOffset FailoverTimeEnd;

        public LogParser() { }
        public void SetTargetFailoverTime(DateTimeOffset start, DateTimeOffset end)
        {
            FailoverTimeStart = start;
            FailoverTimeEnd = end;
        }

        abstract public string TokenizeTimestamp(string line);
        abstract public DateTimeOffset ParseTimeStamp(string timestamp);
        abstract public ErrorLogEntry ParseLogEntry(string entry);
        abstract public void SetupRegexList();
    }
    public class ErrorLogEntry
    {
        private string message;
        private DateTimeOffset timestamp;
        private string spid;

        public string Message { get => message; set => message = value; }
        public DateTimeOffset Timestamp { get => timestamp; set => timestamp = value; }
        public string Spid { get => spid; set => spid = value; }

        public ErrorLogEntry()
        {
            message = String.Empty;
            spid = String.Empty;
            timestamp = DateTimeOffset.MinValue;
        }
        public ErrorLogEntry(DateTimeOffset pTimestamp, string pSpid, string pMessage)
        {
            timestamp = pTimestamp;
            spid = pSpid;
            message = pMessage;
        }
        public bool Equals(ErrorLogEntry logEntry)
        {
            if ( String.Equals(this.timestamp, logEntry.Timestamp)
                && String.Equals(this.spid, logEntry.Spid)
                && String.Equals(this.message, logEntry.Message))
            {
                return true;
            }else
            {
                return false;
            }
        }
        // if a log entry doesn't has timestamp, then it is a tracated message from last message
        public bool IsTrancated()

        {
            if (this.timestamp == DateTimeOffset.MinValue) 
            {
                return true;
            }else
            {
                return false;
            }
        }
        public bool IsEmpty()
        {
            if ( (timestamp == DateTimeOffset.MinValue)
                && String.IsNullOrWhiteSpace(spid)
                && String.IsNullOrWhiteSpace(message))
            {
                return true;
            }else
            {
                return false;
            }
        }
    }
    public class ErrorLogParser : LogParser
    {
        // get timestamp from each line.
        string sPattern = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{2}";
        string testLogPath = @"C:\\Users\zeche\Documents\WorkItems\POC\Data\TestLog.log";
        TimeSpan UTCcorrection;
        List<Regex> m_RegexList;
        public ErrorLogParser()
        {
            m_entryList = new List<ErrorLogEntry>();
            UTCcorrection = new TimeSpan(4, 0, 0);
        }
        public override void SetupRegexList()
        {
            m_RegexList = new List<Regex>();
            m_RegexList.Add(rxTimeStamp);
            m_RegexList.Add(rxSpid);
            m_RegexList.Add(rxError17148);
            m_RegexList.Add(rxErrorServerKill);
            m_RegexList.Add(rxStateTransition);
            m_RegexList.Add(rxStringInQuote);
            m_RegexList.Add(rxFirstSentence);
            m_RegexList.Add(rxUTCAdjust);
        }
        List<ErrorLogEntry> m_entryList;

        private Regex rxTimeStamp = new Regex(@"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{2}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxSpid = new Regex(@"spid[0-9]{1,5}|LOGON|Backup", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // SQLError 17148, 
        private Regex rxError17148 = new Regex(@"SQL Server is terminating in response to a 'stop' request from Service Control Manager", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // SQL Error 17147
        private Regex rxErrorServerKill = new Regex(@"SQL Server is terminating because of a system shutdown");
        // SQL Error 19406, show state transition of a replica
        private Regex rxStateTransition = new Regex(@"The state of the local availability replica in availability group");
        // if we match current message is 19406, we tokenize value from '%ls'
        private Regex rxStringInQuote = new Regex(@"\'\w+\'");
        // match any character that beginning of a string and end with a dot '.'
        private Regex rxFirstSentence = new Regex(@"^([^.]*)\.");
        // match UTC adjustment so we can convert all time to UTC time
        private Regex rxUTCAdjust = new Regex(@"(UTC adjustment:).*");
        // ag lease expired
        private Regex rxLeaeExpired = new Regex(@"(The lease between availability group)(.*)(and the Windows Server Failover Cluster has expired)");

        // HADR_AG_LEASE_RENEWAL_TIMEOUT
        private Regex rxLeaseTimeout = new Regex(@"(SQL Server hosting availability group)(.*)(did not receive a process event signal from the Windows Server Failover Cluster within the lease timeout period.)");

        // HADR_AG_LEASE_RENEWAL_FAILED_DUE_WINDOWS_ERROR
        private Regex rxLeaseRenewFailed = new Regex(@"(The renewal of the lease between availability group)(.*)(and the Windows Server Failover Cluster failed)");

        // HADR_AG_LEASE_FAILED_TO_SLEEP_FOR_EXCESS_LEASE
        private Regex rxLeaseFailedToSleep = new Regex(@"(The lease of availability group)(.*)(lease is no longer valid to start the lease renewal process)");

        // methods
        public override string TokenizeTimestamp(string line)
        {
            // 2017-10-11 11:04:12.32
            string tmp = string.Empty;
            if(this.rxTimeStamp.IsMatch(line))
            {
                string timeStamp = this.rxTimeStamp.Match(line).Value;
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
            DateTimeOffset parsedTime;
            DateTimeOffset.TryParse(substr[0], null as IFormatProvider,
                                System.Globalization.DateTimeStyles.AssumeUniversal, out parsedTime);
            parsedTime += UTCcorrection;
            return parsedTime;

        }

        // we expected input string is after tokenize timestamp
        // typical result is spid* or LOGON
        // TODO: not sure are we expecting a empty result. 
        public string TokenizeSpid(string line)
        {
            string tmp = string.Empty;
            if(this.rxSpid.IsMatch(line))
            {
                tmp = this.rxSpid.Match(line).Value;
            }
            return tmp;
        }
    
        // change a text log entry into a struct format
        public override ErrorLogEntry ParseLogEntry(string line)
        {
            ErrorLogEntry entry = new ErrorLogEntry();
                
            string tmpTimeStamp = TokenizeTimestamp(line);

            line = line.Substring(tmpTimeStamp.Length).Trim();
            string tmpSpid = TokenizeSpid(line);

            string tmpMessage = line.Substring(tmpSpid.Length).Trim();
            
            if(!String.IsNullOrWhiteSpace(tmpTimeStamp))
            {

                entry.Timestamp = ParseTimeStamp(tmpTimeStamp);
            }
            if(!String.IsNullOrWhiteSpace(tmpSpid))
            {
                entry.Spid = tmpSpid;
            }
            if(!String.IsNullOrWhiteSpace(tmpMessage))
            {
                entry.Message = tmpMessage;
            }

            return entry;

        }

        public void ParseLog(string logFilePath)
        {
            using (FileStream stream = File.OpenRead(logFilePath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    ErrorLogEntry pEntry;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        pEntry = ParseLogEntry(line);
                        if (!pEntry.IsEmpty())
                        {
                            if (pEntry.IsTrancated())
                            {
                                // dont use and access date
                                // append message with last one I think. 
                                // trancated is a special case in our problem.
                            }else
                            {
                                // now we need to check the date in our checking range
                                //if (pEntry.Timestamp < sometime upper bound
                                //    && pEntry.Timestamp > sometime lower bound)
                                // parse message, search the pattern we will use. 
                                if(this.rxError17148.IsMatch(pEntry.Message))
                                {
                                    Console.WriteLine("time:{0}, message: {1}", pEntry.Timestamp, pEntry.Message);
                                }
                            }
                        }
                    }
                }
            }

        }
        
        public bool MatchErrorStopService(string msg)
        {
            return this.rxError17148.IsMatch(msg);
        }

        public bool MatchErrorServerKill(string msg)
        {
            return this.rxErrorServerKill.IsMatch(msg);
        }

        public bool MatchStateTransition(string msg)
        {
            return this.rxStateTransition.IsMatch(msg);
        }

        public bool MatchUTCAdjust(string msg)
        {
            return this.rxUTCAdjust.IsMatch(msg);
        }


        public bool MatchLeaseExpired(string msg)
        {
            return this.rxLeaeExpired.IsMatch(msg);
        }

        public bool MatchLeaseTimeout(string msg)
        {
            return this.rxLeaseTimeout.IsMatch(msg);
        }

        public bool MatchLeaseFailedToSleep(string msg)
        {
            return this.rxLeaseFailedToSleep.IsMatch(msg);
        }

        public bool MatchLeaseRenewFailed(string msg)
        {
            return this.rxLeaseRenewFailed.IsMatch(msg);
        }
    }
    
}
