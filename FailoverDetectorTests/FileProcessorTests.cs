using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;

namespace FailoverDetector.Utils.FileProcessor.ConfigurationTests
{
    [TestClass()]
    public class FileProcessorTests
    {
        [TestMethod()]
        public void EqualsTest()
        {
            MetaAgInfo actualAgInfo = new MetaAgInfo("test")
            {
                InstanceName = new List<string>() {"ze-vm001", "ze-vm003"}
            };

            MetaAgInfo expectedAgInfo = new MetaAgInfo("test")
            {
                InstanceName = new List<string>() { "ze-vm001", "ze-vm003" }
            };
            Assert.IsTrue(actualAgInfo.Equals(expectedAgInfo));
        }

        [TestMethod()]
        public void FlatInstanceListTest()
        {
            Configuration actualAgInfo = new Configuration();
            MetaAgInfo metaAg =  new MetaAgInfo("test")
            {
                InstanceName = new List<string>() { "ze-vm001", "ze-vm003" }
            };

            actualAgInfo.AgInfo.Add(metaAg);

            actualAgInfo.FlatInstanceList();

            HashSet<string> expectedInstanceList = new HashSet<string>() { "ze-vm001", "ze-vm003" };

            Assert.IsTrue(expectedInstanceList.SequenceEqual( actualAgInfo.InstanceList));
        }
    }
}

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
            pFileProcess.ProcessDataDirectory();

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
            expectFileProcessor.NodeList["ze-2016-v1"].ClusterLogPath = @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ze-2016-v1_cluster.log";


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
            expectFileProcessor.NodeList["ze-2016-v2"].ClusterLogPath = @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v2\ze-2016-v2_cluster.log";

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
            expectedNode.ClusterLogPath = @"C:\Temp\FailoverDetector\Data\Demo\ze-2016-v1\ze-2016-v1_cluster.log";
            Assert.IsTrue(expectedNode.Equals(pFileProcess.NodeList["ze-2016-v1"]));


        }

        [TestMethod()]
        public void ProcessParameterFailedTest()
        {
            FileProcessor actualfFileProcessor = new FileProcessor(@".\");

            string testParameter = @"-- Analyze  -test";
            string[] splitStrings = testParameter.Split(' ');

            //bool actualResult = (actualfFileProcessor.ShowResult == false) &&
            //                    (actualfFileProcessor.DefaultMode == false) &&
            //                    (actualfFileProcessor.AnalyzeOnly == false);
            Assert.IsFalse(actualfFileProcessor.ProcessParameter(splitStrings));
        }
        [TestMethod()]
        public void ProcessParameterAnalyzeOnlyTest()
        {
            FileProcessor actualfFileProcessor = new FileProcessor(@".\");

            string testParameter = @"--Analyze";
            string[] splitStrings = testParameter.Split(' ');


            Assert.IsTrue(actualfFileProcessor.ProcessParameter(splitStrings));
            bool actualResult = (actualfFileProcessor.ShowResult == false) &&
                                (actualfFileProcessor.DefaultMode == false) &&
                                (actualfFileProcessor.AnalyzeOnly == true);
            Assert.IsTrue(actualResult);
        }
        [TestMethod()]
        public void ProcessParameterAnalyzeAndShowTest()
        {
            FileProcessor actualfFileProcessor = new FileProcessor(@".\");

            string testParameter = @"--Analyze --Show";
            string[] splitStrings = testParameter.Split(' ');


            Assert.IsTrue(actualfFileProcessor.ProcessParameter(splitStrings));
            bool actualResult = (actualfFileProcessor.ShowResult == true) &&
                                (actualfFileProcessor.DefaultMode == false) &&
                                (actualfFileProcessor.AnalyzeOnly == true);
            Assert.IsTrue(actualResult);
        }

        [DeploymentItem("Configuration.json")]
        [TestMethod()]
        public void ParseConfigurationFileTest()
        {
            FileProcessor actualFileProcessor = new FileProcessor();
            actualFileProcessor.ProcessDirectory();
            Assert.IsTrue(actualFileProcessor.FoundConfiguration);
            Configuration expectedConfiguration = new Configuration
            {
                SourcePath = @"\\zechen-d1\dbshare\Temp\Data",
                AgInfo = new List<MetaAgInfo>()
                {
                    new MetaAgInfo("ag1023")
                    {
                        InstanceName = new List<string>() { "ze-2016-v1", "ze-2016-v2" }
                    }
                }
            };

            MemoryStream ms = new MemoryStream();


            actualFileProcessor.ParseConfigurationFile();
            Assert.IsTrue(expectedConfiguration.Equals(actualFileProcessor.ConfigInfo));

        }

        [DeploymentItem("Data", "Data")]
        [DeploymentItem("Data\\UnitTest\\Configuration\\TestCase_Failed\\Configuration.json")]
        [TestMethod]
        public void ValidateFileConverageFaiedTest()
        {

            // Init Output
            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);

                FileProcessor actualFileProcessor = new FileProcessor();
                actualFileProcessor.ProcessDirectory();
                Assert.IsTrue(actualFileProcessor.FoundConfiguration);

                actualFileProcessor.ParseConfigurationFile();
                actualFileProcessor.ProcessDataDirectory();
                actualFileProcessor.ValidateFileCoverage();

                string expected = string.Format(
                    "Validating log provided for AG: ag1023{0}For Instance: ze-2016-v1.{0}All data is ready.{0}All data about instance: ze-2016-v3 is missing. Please check files that you provided.{0}",
                    Environment.NewLine);
                Assert.AreEqual<string>(expected, sw.ToString());

            }
        }

        [DeploymentItem("Data", "Data")]
        [DeploymentItem("Data\\UnitTest\\Configuration\\TestCase_Pass\\Configuration.json")]
        [TestMethod]
        public void ValidateFileConveragePassTest()
        {

            // Init Output
            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);

                FileProcessor actualFileProcessor = new FileProcessor();
                actualFileProcessor.ProcessDirectory();
                Assert.IsTrue(actualFileProcessor.FoundConfiguration);

                actualFileProcessor.ParseConfigurationFile();
                actualFileProcessor.ProcessDataDirectory();
                actualFileProcessor.ValidateFileCoverage();

                string expected = string.Format(
                    "Validating log provided for AG: ag1023{0}For Instance: ze-2016-v1.{0}All data is ready.{0}For Instance: ze-2016-v2.{0}All data is ready.{0}",
                    Environment.NewLine);
                Assert.AreEqual<string>(expected, sw.ToString());

            }
        }

        [DeploymentItem("Data\\Demo", "Data\\Expected")]
        [DeploymentItem("Data\\Demo", "Data\\Source")]
        [TestMethod()]
        public void DirectoryCopyTest()
        {
            FileProcessor actualFileProcessor = new FileProcessor();
            actualFileProcessor.DirectoryCopy("Data\\Source", "Data\\Actual");
            Assert.IsTrue(ComparetwoFolder("Data\\Expected", "Data\\Actual"));

        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest", "Data\\Expected")]
        [DeploymentItem("Data\\UnitTest", "Data\\Actual")]
        public void ComparetwoFolderTest()
        {
            Assert.IsTrue(ComparetwoFolder("Data\\Expected", "Data\\Actual"));
        }
        public bool ComparetwoFolder(string pathA, string pathB)
        {
            try
            {
                var filesA = from file in Directory.EnumerateFiles(pathA, "*.*", SearchOption.AllDirectories)
                             select new
                             {
                                 File = file.Remove(0, pathA.Length)
                             };
                var filesB = from file in Directory.EnumerateFiles(pathB, "*.*", SearchOption.AllDirectories)
                             select new
                             {
                                 File = file.Remove(0, pathB.Length)
                             };
                return filesA.SequenceEqual(filesB);

            }
            catch (UnauthorizedAccessException UAEx)
            {
                Console.WriteLine(UAEx.Message);
            }
            catch (PathTooLongException PathEx)
            {
                Console.WriteLine(PathEx.Message);
            }
            return false;
        }


        
        [DeploymentItem("Data\\UnitTest\\Configuration\\TestCase_Pass\\Configuration.json")]
        [TestMethod()]
        // Test Copy folder from remote destination
        // this remote located at  \\zechen-d1\\dbshare\\Temp\\Data
        public void CopySourceDataFromRemoteTest()
        {
            // parse configuration file
            FileProcessor fileProcessor = new FileProcessor();

            // manual set parameter is true for default mode
            fileProcessor.DefaultMode = true;

            fileProcessor.ProcessDirectory();
            Assert.IsTrue(fileProcessor.FoundConfiguration);
            fileProcessor.ParseConfigurationFile();
            fileProcessor.CopySourceDataFromRemote();
            Assert.IsTrue(ComparetwoFolder("Data\\Demo", "\\\\zechen-d1\\dbshare\\Temp\\Data"));

        }

        [DeploymentItem("Data\\UnitTest\\Configuration\\TestCase_Pass\\Configuration.json")]
        [TestMethod()]
        // Test Copy folder from remote destination
        // this remote located at  \\zechen-d1\\dbshare\\Temp\\Data
        public void CopySourceDataFromRemoteFailedTest()
        {
            // parse configuration file
            FileProcessor fileProcessor = new FileProcessor();

            // manual set parameter is true for default mode
            fileProcessor.DefaultMode = true;

            fileProcessor.ProcessDirectory();
            Assert.IsTrue(fileProcessor.FoundConfiguration);
            fileProcessor.ParseConfigurationFile();
            fileProcessor.CopySourceDataFromRemote();
            Assert.IsTrue(ComparetwoFolder("Data\\Demo", "\\\\zechen-d1\\dbshare\\Temp\\Data"));

        }
    }
}


