using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FailoverDetector
{
    class FailoverReport
    {
        // property
        private DateTimeOffset  failoverTime;
        private string          currentPrimary;
        private string previousPrimary;
        private bool failoverResult;
        private string rootCause;
        private IList<XEventContainer> dataSourceList;

        public void addDataSource(XEventContainer dataContainer)
        {
            dataSourceList.Add(dataContainer);
        }
        public virtual void buildReport()
        {
            // build dataSource
        }

        public virtual void analyzeData()
        {
            // utilize data from container
            // execute search codepath
            // fill data to property

        }
        public virtual void showReport()
        {
            // simple display data from property
        }


    }

    class AutoFailoverReport : FailoverReport
    {

    }
}
