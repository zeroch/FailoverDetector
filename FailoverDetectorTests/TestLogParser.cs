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
        public void TestRegexParser()
        {
            LogParser logParser = new LogParser();
            logParser.testRexParser();

        }

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
                        }
                    }

                }
            }
        }
    }
}
