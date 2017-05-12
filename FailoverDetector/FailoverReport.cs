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
        }

        public virtual void analyzeData()
        {

        }
        public virtual void showReport()
        {

        }


    }
}
