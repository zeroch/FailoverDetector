using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;
using System.IO;


namespace FailoverDetectorTests
{
    [TestClass]
    public class TestLogParser
    {
        string testLogPath = @"C:\\Users\zeche\Documents\WorkItems\POC\Data\TestLog.log";
        [TestMethod]

        [TestMethod]
        public void TestTokenizeTimestamp()
        {
            LogParser logParser = new LogParser();
            using (FileStream stream = File.OpenRead(testLogPath))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                        string tmpTimeStamp = logParser.TokenizeTimestamp(line);
                        Console.WriteLine(tmpTimeStamp);
                        if(!String.IsNullOrWhiteSpace(tmpTimeStamp))
                        {
                            string subline = line.Substring(22).Trim();
                            string tmpSpid = logParser.TokenizeSpid(subline);
                            Console.WriteLine(tmpSpid);
                            string tmpMessage = subline.Substring(tmpSpid.Length).Trim();
                            Console.WriteLine(tmpMessage);
                        }
                    }

                }
            }
        }

        [TestMethod]
        public void TestParseLogEntry()
        {
            string testString = @"2017-09-10 22:00:00.12 spid191     UTC adjustment: -4:00";
            LogParser parser = new LogParser();
            LogParser.ErrorLogEntry entry = new LogParser.ErrorLogEntry();
            entry = parser.ParseLogEntry(testString);
            Console.WriteLine("Timestamp: {0}, spid: {1}, and message: {2}", entry.Timestamp, entry.Spid, entry.Message);

        }
        [TestMethod]
        public void TestErrorLogEntryEquals()
        {
            LogParser.ErrorLogEntry pEntry = new LogParser.ErrorLogEntry("2017-09-10 22:00:00.19", "spid191", "UTC adjustment: -4:00");
            string testString = @"2017-09-10 22:00:00.19 spid191     UTC adjustment: -4:00";
            LogParser.ErrorLogEntry entry = new LogParser.ErrorLogEntry();
            LogParser parser = new LogParser();
            entry = parser.ParseLogEntry(testString);
            Assert.IsTrue(entry.Equals(pEntry));

        }
    }
}
