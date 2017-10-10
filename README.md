# FailoverDetector
1.	This tool will be a command line tool with configuration file template. 
2.	The configuration file is a Json file format. User edits configuration file to customize replica information.   
3.	The tool execution parameters include:
a.	User name and password that have access to specified replica. 
b.	Option that enable data collection option.
c.	Option that allow user to run analyze tool without collecting data. In this option, data should already existed in data directory.
4.	Command line UI will display analysis result with the following information:
a.	Failover timestamp
b.	Failover original and current primary replica information
c.	Failover root cause or Possible failover root cause estimate
d.	Failover root cause detailed description.
e.	Failover root cause supporting information: Evidence message that we decide root cause, including error code and error message, information source and information timestamp. 
5.	Analysis result will save as JSON and XML file under current execution path, use timestamp as filename. 
