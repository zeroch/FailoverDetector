﻿using System;
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
        [TestMethod]
        public void openXelFile_withWrongPath()
        {
            XEventContainer container = new XEventContainer();
            bool result = container.openXelFile("kjlkjjklj");
            Assert.AreEqual(result, false);
        }
    }
}
