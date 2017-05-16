using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.XEvent.Linq;
using System.Collections;


namespace FailoverDetector
{
    public class XEventContainer : DataContainer
    {
        private string filePath;
        private AlwaysOnData m_AlwaysOn;
        private SystemData m_System;

        public XEventContainer(string path)
        {
            filePath = path;
            m_AlwaysOn = new AlwaysOnData();
            m_System = new SystemData();
        }
     

        public override void loadDataFile()
        {
            // load xel File
            // filter data
            // extract into alwaysOnData

            // load xel File
            // filter data
            // extract into systemData
        }
        public override bool DetectFailover()
        {
            // run search at m_AlwaysOn
            return true;
        }
    }

}
