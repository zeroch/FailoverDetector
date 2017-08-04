using Microsoft.VisualStudio.TestTools.UnitTesting;
using FailoverDetector;
using Microsoft.SqlServer.XEvent.Linq;
using System;



namespace FailoverDetector.Tests
{
    [TestClass()]
    public class AlwaysOnDataTests
    {
        [TestMethod()]
        public void ParseStatementTest()
        {
            AlwaysOnData m_data = new AlwaysOnData();
            string testStr = "ALTER AVAILABILITY GROUP [ag_name] failover";
            Assert.AreEqual(true, m_data.ParseStatement(testStr));
        }

        [TestMethod()]
        public void ParseStatementForceFailoverTest()
        {
            AlwaysOnData m_data = new AlwaysOnData();
            string testStr = "   ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss";
            Assert.AreEqual(true, m_data.ParseStatement(testStr));

        }
        [TestMethod()]
        public void ParseStatementCreateAGTest()
        {
            AlwaysOnData m_data = new AlwaysOnData();
            string testStr = @"CREATE AVAILABILITY GROUP MyAg  WITH( 
                                AUTOMATED_BACKUP_PREFERENCE = SECONDARY, 
                                FAILURE_CONDITION_LEVEL = 3,    
                                HEALTH_CHECK_TIMEOUT = 600000
                                )   ";
            Assert.AreEqual(false, m_data.ParseStatement(testStr));

        }

        [TestMethod()]
        public void ParseStatementWithEmptyString()
        {
            AlwaysOnData m_data = new AlwaysOnData();
            string testStr = @"   ";
            Assert.AreEqual(false, m_data.ParseStatement(testStr));

        }
    }
}

