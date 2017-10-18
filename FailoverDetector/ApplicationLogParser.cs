using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace FailoverDetector
{
    public class ApplicationLogParser : LogParser
    {
        public ApplicationLogParser()
        {
        }

        private Regex rxTimeStamp = new Regex(@"\d{1,2}\/\d{1,2}\/\d{4}\s\d{2}:\d{2}:\d{2}\s+[AM|PM]{2}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex rxEntryType = new Regex(@"Information|Error");
        private Regex rxBrackets = new Regex(@"\[.*?\]");
        private Regex rxMSSQLSource = new Regex(@"MSSQL\$\w+");

        public override string TokenizeTimestamp(string line)
        {
            string tmp = string.Empty;
            if( this.rxTimeStamp.IsMatch(line))
            {
                tmp = this.rxTimeStamp.Match(line).Value;
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

        public string TokenizeSQLSource(string line)
        {
            string tmp = string.Empty;
            if (this.rxMSSQLSource.IsMatch(line))
            {
                tmp = this.rxMSSQLSource.Match(line).Value;
            }
            return tmp;
        }

        public override ErrorLogEntry ParseLogEntry(string line)
        {
            string tmpTimestamp = TokenizeTimestamp(line);
            line = line.Substring(tmpTimestamp.Length).Trim();

            string tmpType = TokenizeEntryType(line);
            line = line.Substring(tmpType.Length).Trim();

            string tmpSQLSource = TokenizeSQLSource(line);
            line = line.Substring(tmpSQLSource.Length ).Trim();

            ErrorLogEntry entry = new ErrorLogEntry(tmpTimestamp, "", line);
            return entry;

        }


    }
}
