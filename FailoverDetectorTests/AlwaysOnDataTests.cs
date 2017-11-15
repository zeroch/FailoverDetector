using FailoverDetector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FailoverDetectorTests
{
    [TestClass()]
    public class AlwaysOnDataTests
    {
        [TestMethod()]
        public void ParseStatementTest()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = "ALTER AVAILABILITY GROUP [ag_name] failover";
            Assert.AreEqual(true, mData.ParseStatement(testStr));
        }

        [TestMethod()]
        public void ParseStatementForceFailoverTest()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = "   ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss";
            Assert.AreEqual(true, mData.ParseStatement(testStr));

        }
        [TestMethod()]
        public void ParseStatementCreateAgTest()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = @"CREATE AVAILABILITY GROUP MyAg  WITH( 
                                AUTOMATED_BACKUP_PREFERENCE = SECONDARY, 
                                FAILURE_CONDITION_LEVEL = 3,    
                                HEALTH_CHECK_TIMEOUT = 600000
                                )   ";
            Assert.AreEqual(false, mData.ParseStatement(testStr));

        }

        [TestMethod()]
        public void ParseStatementWithEmptyString()
        {
            AlwaysOnData mData = new AlwaysOnData();
            string testStr = @"   ";
            Assert.AreEqual(false, mData.ParseStatement(testStr));

        }
    }
}

