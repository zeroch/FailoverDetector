using System;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using FailoverDetector.Utils;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace FailoverDetector
{

    // This is System Log. System log is CSV format. so we will parse it not rely on regular express. 
    // we will parse line rely on TextFieldParser so we don't have to reinvent wheel
    public class SystemLogParser : LogParser
    {
        public SystemLogParser()
        {
            sourceType = Constants.SourceType.SystemLog;
            _utCcorrection = new TimeSpan(0, 0, 0);
            SetupRegexList();
            startToReadSystem = true;
        }
        public override void SetupRegexList()
        {
            _logParserList = new List<MessageExpression>()
            {
                new ServiceCrashedExpression()
            };
        }

        public void SetUTCCorrection(TimeSpan timezone)
        {
            _utCcorrection = timezone;
        }



        public new void ParseLog(string logFilePath, string instanceName)
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;

            try
            {
                using (TextFieldParser parser = new TextFieldParser(logFilePath))
                {


                    // we want it default to be "false" data from old to new
                    // any exit we should sort it back, in case effect other method
                    pReportMgr.SortReports(true);

                    IEnumerator ReportIterator = pReportMgr.ReportVisitor();
                    if (!ReportIterator.MoveNext())
                        return;

                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    string line = parser.ReadLine(); 
                    line = parser.ReadLine();// skip first 2 line

                    while (!parser.EndOfData && ReportIterator.Current != null)
                    {
                        string[] fields = parser.ReadFields();
                        var pEntry = ParseLogEntry(fields);
                        if (pEntry.IsEmpty() || pEntry.IsTrancated())
                        {
                            fields = parser.ReadFields();
                            continue;
                        }

                        DateTimeOffset messageTime = pEntry.Timestamp;
                        // compare time
                        PartialReport reportInstance = (PartialReport)ReportIterator.Current;

                        if (messageTime < (reportInstance.StartTime.AddMinutes(-1 * Constants.DefaultInterval)))
                        {
                            if (!ReportIterator.MoveNext())
                                break;
                            else
                                continue;

                        }
                        else if (messageTime > reportInstance.EndTime.AddMinutes(Constants.DefaultInterval))
                        {
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
                        }
                    }

                    pReportMgr.SortReports(false);

                }

            }
            catch (DirectoryNotFoundException e)
            {
                pReportMgr.SortReports(false);
                Console.WriteLine(e.Message);
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                pReportMgr.SortReports(false);
                Console.WriteLine(e.Message);
                return;
            }catch(Exception e)
            {
                pReportMgr.SortReports(false);
                Console.WriteLine(e.Message);
                return;
            }
        }


        // When windows system log export as CSV file, it follows schema below:
        // EventID,MachineName,Data,Index,Category,CategoryNumber,EntryType,Message,
        // Source,ReplacementStrings,InstanceId,TimeGenerated,TimeWritten,UserName,
        // Site,Container
        //  EventID : 0
        //  Message : 7
        //  TimeGenerated: 11

        public ErrorLogEntry ParseLogEntry(string[] fields)
        {
            ErrorLogEntry entry = new ErrorLogEntry();
            if (fields == null || fields.Length < 12)
            {
                return entry; 
            }
            else
            {
                string tmpEventID = fields[0];
                string tmpMessage = fields[7];
                string tmpTimestamp = fields[11];

                if (!String.IsNullOrWhiteSpace(tmpTimestamp))
                {
                    entry.Timestamp = ParseTimeStamp(tmpTimestamp);
                }
                if (!String.IsNullOrWhiteSpace(tmpEventID))
                {
                    entry.Spid = tmpEventID;
                }
                if (!String.IsNullOrWhiteSpace(tmpMessage))
                {
                    entry.Message = tmpMessage;
                    string rawMessage = tmpTimestamp + "\t" + tmpEventID + "\t" + tmpMessage;
                    entry.RawMessage = rawMessage;
                }

            }

            return entry;
        }
        public override ErrorLogEntry ParseLogEntry(string line)
        {
        
            throw new NotImplementedException();

        }

        public override string TokenizeTimestamp(string line)
        {
            throw new NotImplementedException();
        }

        public override DateTimeOffset ParseTimeStamp(string timestamp)
        {
            // timestamp have a special 23.5    we need to remove .x value from string
            string[] substr = timestamp.Split('.');
            DateTimeOffset.TryParse(substr[0], null as IFormatProvider,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var parsedTime);
            parsedTime += _utCcorrection;
            return parsedTime;
        }
    }
}
