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

        enum PatternIndex : int
        {
            Timstamp = 0,
            Spid,
            Message
        };
        public void Init()
        {

        }
        public void Exit()
        {
        }


        private Regex rxTimeStamp = new Regex(@"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}.\d{2}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxSpid = new Regex(@"spid[0-9]{1,5}|LOGON", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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

        public void testRexParser()
        {
            using (FileStream stream = File.OpenRead(testLogPath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                        Regex rgx = new Regex(sPattern);
                        MatchCollection mc = rgx.Matches(line);
                        foreach (Match match in mc)
                        {
                            Console.WriteLine(match);
                        }
                    }
                }
            }

        }
    }
    
}
