using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetector.Tests
{
    public class AlwaysOnDataTests
    {
        [TestMethod()]
        public void TestParseStatement()
        {
            AlwaysOnData m_data = new AlwaysOnData();
            string testStr = "ALTER AVAILABILITY GROUP [ag_name] failover";
            Assert.AreEqual(true, m_data.ParseStatement(testStr));
        }
    }
}