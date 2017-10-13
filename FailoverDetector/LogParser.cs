using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace FailoverDetector
{
    public class LogParser
    {
        // get timestamp from each line.
        string sPattern = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{2}";
        string testLogPath = @"C:\\Users\zeche\Documents\WorkItems\POC\Data\TestLog.log";

    
        public LogParser()
        {
            m_entryList = new List<ErrorLogEntry>();
        }

        List<ErrorLogEntry> m_entryList;

        private Regex rxTimeStamp = new Regex(@"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{2}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxSpid = new Regex(@"spid[0-9]{1,5}|LOGON|Backup", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public string TokenizeTimestamp(string line)
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
    
        public class ErrorLogEntry
        {
            private string message;
            private string timestamp;
            private string spid;

            public string Message { get => message; set => message = value; }
            public string Timestamp { get => timestamp; set => timestamp = value; }
            public string Spid { get => spid; set => spid = value; }

            public ErrorLogEntry()
            {
                message = String.Empty;
                spid = String.Empty;
                timestamp = String.Empty;
            }
            public ErrorLogEntry(string pTimestamp, string pSpid, string pMessage)
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
                if(String.IsNullOrEmpty(Message))
                {
                    return true;
                }else
                {
                    return false;
                }
            }
            public bool IsEmpty()
            {
                if (String.IsNullOrWhiteSpace(timestamp)
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
        // change a text log entry into a struct format
        public ErrorLogEntry ParseLogEntry(string line)
        {
            ErrorLogEntry entry = new ErrorLogEntry();
                
            string tmpTimeStamp = TokenizeTimestamp(line);

            line = line.Substring(tmpTimeStamp.Length).Trim();
            string tmpSpid = TokenizeSpid(line);

            string tmpMessage = line.Substring(tmpSpid.Length).Trim();
            
            if(!String.IsNullOrWhiteSpace(tmpTimeStamp))
            {
                entry.Timestamp = tmpTimeStamp;
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
                            }
                        }
                    }
                }
            }

        }
    }
    
}
