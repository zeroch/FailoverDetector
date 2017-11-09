﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
            int i = 0;
            foreach( string node in nodes)
            {
                string xelPath = node + @"\AlwaysOn_health*.xel";
                string nodeName = node.Remove(0, path.Length + 1);
                Console.WriteLine("xel file path is : {0}\n Node name: {1}", xelPath, nodeName);
                var instance = new AlwaysOnData();
                instance.LoadData(xelPath, nodeName);
                if(i != 0 )
                {
                    nodeList.First().MergeInstance(instance);
                }
                nodeList.Add(instance);
                i++;

            }
            AlwaysOnData node001 = nodeList.First();
            node001.AnalyzeReports();
            node001.ShowFailoverReports();

            Console.ReadLine();
        }

    }


}
