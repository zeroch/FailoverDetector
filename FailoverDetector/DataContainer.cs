using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FailoverDetector
{
    public class DataContainer
    {
        public DataContainer() { }
        public DataContainer(string filename) { }
        public DataContainer(string[] fileList) { }
        public virtual void loadDataFile() { }
        public virtual void filterData() { }
        public virtual void DetectFilover() { }
        public virtual void SearchFilover() { }
        public virtual void SearchLeaseTimeout() { }
        public virtual void SearchHealthCheckTimeout() { }
        public virtual void SearchComponentResult() { }
    }
}
