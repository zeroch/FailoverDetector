using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FailoverDetector.UtilsTests
{
    [TestClass()]
    public class FileProcessorTests
    {

        [TestMethod()]
        public void RootDirectoryTest()
        {
            string testPath = @"C:\Temp\FailoverDetector\Data\Demo";
            FileProcessor pFileProcess = new FileProcessor(testPath);
            pFileProcess.RootDirectory(testPath);

            FileProcessor expectFileProcessor = new FileProcessor();
            expectFileProcessor.NodeList["ze-2016-v1"] = new FileProcessor.NodeFileInfo("ze-2016-v1");
            expectFileProcessor.NodeList["ze-2016-v1"].SetAlwaysOnFile(new List<string>
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532583880140000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532634780070000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532680183890000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532701164380000.xel"
            });
            expectFileProcessor.NodeList["ze-2016-v1"].SetErrorLogFile(new List<string>()
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.1",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.2",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.3",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.4",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.5",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.6",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\SQLDUMPER_ERRORLOG.log"
            });
            expectFileProcessor.NodeList["ze-2016-v1"].SetSystemHealthFile(new List<string>()
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532583879830000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532634779760000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532680183430000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532701164070000.xel"
            });
            expectFileProcessor.NodeList["ze-2016-v1"].ClusterLogPath = @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1_cluster.log";


            expectFileProcessor.NodeList["ze-2016-v2"] = new FileProcessor.NodeFileInfo("ze-2016-v2");
            expectFileProcessor.NodeList["ze-2016-v2"].SetAlwaysOnFile(new List<string>
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\AlwaysOn_health_0_131532571347210000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\AlwaysOn_health_0_131532578226200000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\AlwaysOn_health_0_131532586348180000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\AlwaysOn_health_0_131532725682240000.xel"
            });
            expectFileProcessor.NodeList["ze-2016-v2"].SetErrorLogFile(new List<string>()
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG.1",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG.2",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG.3",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG.4",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG.5",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ERRORLOG.6"
            });
            expectFileProcessor.NodeList["ze-2016-v2"].SetSystemHealthFile(new List<string>()
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\system_health_0_131532571346430000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\system_health_0_131532578225950000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\system_health_0_131532586347860000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\system_health_0_131532725681930000.xel"
            });
            expectFileProcessor.NodeList["ze-2016-v2"].ClusterLogPath = @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2_cluster.log";

            Assert.IsTrue(expectFileProcessor.Equals(pFileProcess));

        }

        [TestMethod()]
        public void ProcessNodeDirectoryTest()
        {
            string testPath = @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1";
            FileProcessor pFileProcess = new FileProcessor();
            pFileProcess.ProcessNodeDirectory(testPath);

            FileProcessor.NodeFileInfo expectedNode = new FileProcessor.NodeFileInfo("ze-2016-v1");
            expectedNode.SetAlwaysOnFile(new List<string>
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532583880140000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532634780070000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532680183890000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\AlwaysOn_health_0_131532701164380000.xel"
            });
            expectedNode.SetErrorLogFile(new List<string>()
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.1",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.2",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.3",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.4",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.5",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ERRORLOG.6",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\SQLDUMPER_ERRORLOG.log"
            });
            expectedNode.SetSystemHealthFile(new List<string>()
            {
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532583879830000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532634779760000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532680183430000.xel",
                @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\system_health_0_131532701164070000.xel"
            });

            Assert.IsTrue(expectedNode.Equals(pFileProcess.NodeList["ze-2016-v1"]));


        }
    }
}


