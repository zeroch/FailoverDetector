using System;
using System.Text.RegularExpressions;

namespace FailoverDetector
{
    public class ApplicationLogParser : LogParser
    {
        public ApplicationLogParser()
        {
            _utCcorrection = new TimeSpan(4, 0, 0);
        }
        public override void SetupRegexList()
        {
            throw new NotImplementedException();
        }

        readonly TimeSpan _utCcorrection;
        private readonly Regex _rxTimeStamp = new Regex(@"\d{1,2}\/\d{1,2}\/\d{4}\s\d{2}:\d{2}:\d{2}\s+[AM|PM]{2}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _rxEntryType = new Regex(@"Information|Error");
        private Regex _rxBrackets = new Regex(@"\[.*?\]");
        private readonly Regex _rxMssqlSource = new Regex(@"MSSQL\$\w+");

        public override string TokenizeTimestamp(string line)
        {
            string tmp = string.Empty;
            if (_rxTimeStamp.IsMatch(line))
            {
                tmp = _rxTimeStamp.Match(line).Value;
            }
            return tmp;
        }
        public override DateTimeOffset ParseTimeStamp(string timestamp)
        {
            DateTimeOffset.TryParse(timestamp, null as IFormatProvider,
                                    System.Globalization.DateTimeStyles.AssumeUniversal,
                                    out var parsedTime);
            parsedTime += _utCcorrection;
            return parsedTime;
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

        public string TokenizeSqlSource(string line)
        {
            string tmp = string.Empty;
            if (_rxMssqlSource.IsMatch(line))
            {
                tmp = _rxMssqlSource.Match(line).Value;
            }
            return tmp;
        }

        public override ErrorLogEntry ParseLogEntry(string line)
        {
            string tmpTimestamp = TokenizeTimestamp(line);
            DateTimeOffset tmpParsedTime = ParseTimeStamp(tmpTimestamp);
            line = line.Substring(tmpTimestamp.Length).Trim();

            string tmpType = TokenizeEntryType(line);
            line = line.Substring(tmpType.Length).Trim();

            string tmpSqlSource = TokenizeSqlSource(line);
            line = line.Substring(tmpSqlSource.Length ).Trim();

            ErrorLogEntry entry = new ErrorLogEntry(tmpParsedTime, "", line);
            return entry;

        }


    }
}
