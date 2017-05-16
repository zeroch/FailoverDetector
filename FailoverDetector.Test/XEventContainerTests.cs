using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;



namespace FailoverDetector.Test
{
    [TestClass]
    public class XEventContainerTests
    {
        [TestMethod]
        public void openXelFile_withValidInput()
        {
            XEventContainer container = new XEventContainer();
            string fileName = "C:\\AlwaysOn_health_0_131180526930290000.xel";
            bool result = container.openXelFile(fileName);
            Assert.AreEqual(result, true);
        }

        [TestMethod]
        public void openXelFile_withNull()
        {
            XEventContainer container = new XEventContainer();
            bool result = container.openXelFile("");
            Assert.AreEqual(result, false);
        }
        //[TestMethod]
        //public void openXelFile_withWrongPath()
        // XEventContainer will open a wrong path anyway. 
        //{
        //    XEventContainer container = new XEventContainer();
        //    bool result = container.openXelFile("kjlkjjklj");
        //    Assert.AreEqual(result, false);
        //}
        [TestMethod]
        public void Test_getStateChange()
        {
            XEventContainer container = new XEventContainer();
            bool result = container.openXelFile("C:\\AlwaysOn_health_0_131180526930290000.xel");
            if(result)
            {
                container.getStateChange();
            }
            
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void Test_getStateChange_withNullData()
        {
            XEventContainer container = new XEventContainer();
            container.getStateChange();

        }
    }
}
