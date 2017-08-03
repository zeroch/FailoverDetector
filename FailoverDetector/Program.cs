using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.SqlServer.XEvent.Linq;

namespace FailoverDetector
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Directory.GetCurrentDirectory();
            path += @"\Data";
            string[] nodes = Directory.GetDirectories(path);
            List<AlwaysOnData> nodeList = new List<AlwaysOnData>();
            AlwaysOnData instance = null;
            int i = 0;
            foreach( string node in nodes)
            {
                string xelPath = node + @"\*.xel";
                string nodeName = node.Remove(0, path.Length + 1);
                Console.WriteLine("xel file path is : {0}\n Node name: {1}", xelPath, nodeName);
                instance = new AlwaysOnData();
                instance.loadData(xelPath, nodeName);
                if(i != 0 )
                {
                    nodeList.First().MergeInstance(instance);
                }
                nodeList.Add(instance);

            }
            AlwaysOnData Node001 = nodeList.First();
            Node001.AnalyzeReports();
            Node001.ShowFailoverReports();

            Console.ReadLine();
        }

    }


}
