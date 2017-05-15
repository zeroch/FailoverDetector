// <copyright file="XEventContainerTest.cs">Copyright ©  2017</copyright>
using System;
using FailoverDetector;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetector.Tests
{
    /// <summary>This class contains parameterized unit tests for XEventContainer</summary>
    [PexClass(typeof(XEventContainer))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class XEventContainerTest
    {
        /// <summary>Test stub for createTable()</summary>
        [PexMethod]
        internal void createTableTest([PexAssumeUnderTest]XEventContainer target)
        {
            target.createTable();
            // TODO: add assertions to method XEventContainerTest.createTableTest(XEventContainer)
        }

        /// <summary>Test stub for filterNoise()</summary>
        [PexMethod]
        internal void filterNoiseTest([PexAssumeUnderTest]XEventContainer target)
        {
            target.filterNoise();
            // TODO: add assertions to method XEventContainerTest.filterNoiseTest(XEventContainer)
        }

        /// <summary>Test stub for insertDataToTable()</summary>
        [PexMethod]
        internal void insertDataToTableTest([PexAssumeUnderTest]XEventContainer target)
        {
            target.insertDataToTable();
            // TODO: add assertions to method XEventContainerTest.insertDataToTableTest(XEventContainer)
        }

        /// <summary>Test stub for openXelFile(String)</summary>
        [PexMethod]
        internal bool openXelFileTest([PexAssumeUnderTest]XEventContainer target, string fileName)
        {
            bool result = target.openXelFile(fileName);
            return result;
            // TODO: add assertions to method XEventContainerTest.openXelFileTest(XEventContainer, String)
        }
    }
}
