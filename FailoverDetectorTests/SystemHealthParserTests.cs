using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static FailoverDetector.SystemHealthParser;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace FailoverDetectorTests
{
    [TestClass()]
    public class SystemHealthParserTests
    {
        [TestMethod()]
        public void ParseSystemComponentTest()
        {
            using (StreamReader reader = new StreamReader("C:\\Temp\\test.txt"))
            {
                string xml = reader.ReadToEnd();
                Console.WriteLine(xml);
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));

                XmlSerializer serializer = new XmlSerializer(typeof(SystemComponent));
                SystemComponent TestSysComp = new SystemComponent();
                TestSysComp = (SystemComponent)serializer.Deserialize(ms);
                Console.WriteLine(TestSysComp.pageFaults);
                ms.Close();
                Assert.AreEqual(TestSysComp.systemCpuUtilization, (UInt32)16);
            }
        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\SystemHealth", "SystemHealth")]
        public void ParseQPComponentTest()
        {
            using (StreamReader reader = new StreamReader("SystemHealth\\QPCompTest_00.txt"))
            {
                string xml = reader.ReadToEnd();
                Console.WriteLine(xml);
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));

                XmlSerializer serializer = new XmlSerializer(typeof(QPComponent));
                QPComponent qPComponent = new QPComponent();
                qPComponent = (QPComponent)serializer.Deserialize(ms);

                ms.Close();
                Assert.AreEqual(qPComponent.tasksCompletedWithinInterval, (UInt32)2390);
            }
        }
        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\SystemHealth", "SystemHealth")]
        public void ParseResourceComponentTest()
        {
            using (StreamReader reader = new StreamReader("SystemHealth\\ResourceCompTest_00.txt"))
            {
                string xml = reader.ReadToEnd();
                Console.WriteLine(xml);
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));

                XmlSerializer serializer = new XmlSerializer(typeof(ResourceComponent));
                ResourceComponent resourceComponent = new ResourceComponent();
                resourceComponent = (ResourceComponent)serializer.Deserialize(ms);

                ms.Close();
                Assert.AreEqual(resourceComponent.lastNotification, "RESOURCE_MEMPHYSICAL_HIGH");
            }
        }

        [TestMethod()]
        [DeploymentItem("Data\\UnitTest\\SystemHealth", "SystemHealth")]
        public void ParseIoComponentTest()
        {
            using (StreamReader reader = new StreamReader("SystemHealth\\IoCompTest_00.txt"))
            {
                string xml = reader.ReadToEnd();
                Console.WriteLine(xml);
                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(xml));

                XmlSerializer serializer = new XmlSerializer(typeof(IoComponent));
                IoComponent ioComponent = new IoComponent();
                ioComponent = (IoComponent)serializer.Deserialize(ms);

                ms.Close();
                Assert.AreEqual(ioComponent.intervalLongIos, (UInt32)1);
            }
        }
    }
}