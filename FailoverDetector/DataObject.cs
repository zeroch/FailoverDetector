using System;
using System.Collections.Generic;
using Microsoft.SqlServer.XEvent.Linq;
using System.Linq;

namespace FailoverDetector
{
    
    public class AlwaysOnData
    {

        readonly int _defaultInterval = 5;
        string _instanceName;

        public Dictionary<string, AgReport> AgEventMap { get; set; }

        public AlwaysOnData()
        {
        }
        enum AlwaysOnEventType {
            DllExecuted,
            AgLeaseExpired,
            ArMangerStateChange,
            ArState,
            ArStateChange,
            LockRedoBlocked,
            Error
        }
        public void HandleDdlExecuted(PublishedEvent evt)
        {
            // find active alter ag failover
            // we only can find based on statement:
            // pattern is 
            // ALTER AVAILABILITY GROUP [ag_name] failover
            // ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss
            string evtStatement = evt.Fields["statement"].Value.ToString();
            bool isForceFailover = ParseStatement(evtStatement);
            if (isForceFailover)
            {
                // receive a agName, which mean PrseStatement valid a failover statement
                // check fill report or populate a report
                bool commited = (evt.Fields["ddl_phase"].Value.ToString() == "commit");
                if (commited)
                {
                    string agName = evt.Fields["availability_group_name"].Value.ToString();
                    string agId = evt.Fields["availability_group_id"].Value.ToString();
                    // get List of report for this ag
                    // get List of report for this ag
                    ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
                    AgReport mReports = pReportMgr.GetAgReports(agName);
                    if ( mReports == null)
                    {
                        mReports = pReportMgr.AddNewAgReport(agName, _instanceName);
                    }

                    PartialReport pReport = mReports.FGetReport(evt.Timestamp);
                    // update information
                    pReport.ForceFailoverFound = true;
                    if(pReport.AgId == string.Empty)
                    {
                        pReport.AgId = agId;
                    }
                    if (pReport.AgName == string.Empty)
                    {
                        pReport.AgName = agName;
                    }

                    mReports.UpdateReport(pReport);

                }

            }

        }
        // parse and find failover statement
        // ALTER AVAILABILITY GROUP [ag_name] failover
        // ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss
        public bool ParseStatement(string str)
        {

            string[] wds = str.Split(' ');
            List<string> words = wds.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ;

            // sanity check, if words count is less than 3
            // we have to check strings that after trim space.

            if(words.Count < 3)
            {
                return false;
            }
            string command = String.Join(" ", words.Take(3)).ToLower();
            // TODO posibble null reference exception
            string parameter = words.LastOrDefault().ToLower().TrimEnd(';');
            // HANDEL case like 
            // "failover;" we need to trim the ';'
            
            if (command.Equals("alter availability group"))
            {
                if (parameter.Equals("failover") || parameter.Equals("force_failover_allow_data_loss"))
                {
                    return true;
                }
            }
            return false;
        }

        public void HandleAgLeaseExpired(PublishedEvent evt)
        {
            string agName = evt.Fields["availability_group_name"].Value.ToString();
            string agId = evt.Fields["availability_group_id"].Value.ToString(); 
            // get List of report for this ag
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            AgReport mReports = pReportMgr.GetAgReports(agName);
            if (mReports == null)
            {
                mReports = pReportMgr.AddNewAgReport(agName, _instanceName);
            }
            PartialReport pReport = mReports.FGetReport(evt.Timestamp);
            pReport.LeaseTimeoutFound = true;
            if (pReport.AgId == string.Empty)
            {
                pReport.AgId = agId;
            }
            if (pReport.AgName == string.Empty)
            {
                pReport.AgName = agName;
            }
            mReports.UpdateReport(pReport);
        }
        public void HandleArMgrStateChange(PublishedEvent evt)
        {   
            // TODO: add it later
;
        }
        public void HandleArState(PublishedEvent evt) { }
        public void HandleArStateChange(PublishedEvent evt)
        {
            string agName = evt.Fields["availability_group_name"].Value.ToString();
            string agId = evt.Fields["availability_group_id"].Value.ToString(); 
            // get List of report for this ag
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            AgReport mReports = pReportMgr.GetAgReports(agName);
            if (mReports == null)
            {
                mReports = pReportMgr.AddNewAgReport(agName, _instanceName);
            }
            PartialReport pReport = mReports.FGetReport(evt.Timestamp);
            if(pReport.IsEmptyRole())
            {
                pReport.AddRoleTransition(evt.Fields["previous_state"].Value.ToString());
            }
            pReport.AddRoleTransition(evt.Fields["current_state"].Value.ToString());
            if (pReport.AgId == string.Empty)
            {
                pReport.AgId = agId;
            }
            if (pReport.AgName == string.Empty)
            {
                pReport.AgName = agName;
            }


        }


        public void HandleLockRedoBlocked(PublishedEvent evt) { }


        // error code reported in this event. 

        // --alwayson connection timeout information
        // or[error_number]=(35201) --new connection timeout
        // or[error_number]=(35202) --connected
        //or[error_number] = (35206)--existing connection timeout
        //or[error_number] = (35207)--general connection issue message

        // check
        // --wsfc cluster issues
        // or[error_number]>(41047) and[error_number]<(41056)

        // check
        // --failover validation message
        // or[error_number] = (41142)

        // check
        // --availability group resource failure
        // or[error_number]=(41144) 

        // check           
        // --database replica role change
        // or[error_number]=(1480) 

        public void HandleErrorReported(PublishedEvent evt)
        {
            //read error code
            string errCode = evt.Fields["error_number"].Value.ToString();

            switch(errCode)
            {
                case "41144":
                    break;
                // ErrorFormat: The availability replica for availability group '%.*ls' on this instance of SQL Server cannot become the primary replica. One or more databases are not synchronized or have not joined the availability group. If the availability replica uses the asynchronous-commit mode, consider performing a forced manual failover (with possible data loss). Otherwise, once all local secondary databases are joined and synchronized, you can perform a planned manual failover to this secondary replica (without data loss). For more information, see SQL Server Books Online.
                // ErrorCause: One or more databases in the local replica of the availability group are not synchronized or have not joined the Availability group.
                // ErrorCorrectiveAction: If the availability replica uses the asynchronous-commit mode, consider performing a forced manual failover (with possible data loss). Otherwise, once all local secondary databases are joined and synchronized, you can perform a planned manual failover to this secondary replica (without data loss). For more information, see SQL Server Books Online.
                case "41142":
                    break;
                // WSFC message
                // ErrorFormat: Failed to obtain the Windows Server Failover Clustering (WSFC) node state for the local WSFC node (Error code %d).  If this is a WSFC availability group, the WSFC service may not be running or may not be accessible in its current state.  Otherwise, contact your primary support provider.  For information about this error code, see "System Error Codes" in the Windows Development documentation.
                // ErrorCause: The WSFC service, if applicable, may not be running or may not be accessible in its current state.
                case "41047":
                    break;
                // ErrorFormat: Always On Availability Groups: Local Windows Server Failover Clustering service has become unavailable. This is an informational message only. No user action is required.
                // ErrorWinFSFormat: Always On Availability Groups: Local Windows Server Failover Clustering service has become unavailable. This is an informational message only. No user action is required.
                case "41048":
                    break;
                // ErrorWinFSFormat: Always On Availability Groups: Local Windows Server Failover Clustering node is no longer online. This is an informational message only. No user action is required.
                case "41049":
                    break;
                // ErrorFormat: Always On Availability Groups: Waiting for local Windows Server Failover Clustering node to come online. This is an informational message only. No user action is required.
                // ErrorWinFSFormat: Always On Availability Groups: Waiting for local Windows Server Failover Clustering node to come online. This is an informational message only. No user action is required.
                case "41054":
                    break;
                // ErrorFormat: Availability replica '%.*ls' of availability group '%.*ls' cannot be brought online on this SQL Server instance.  Another replica of the same availability group is already online on the node.  Each node can host only one replica of an availability group, regardless of the number of SQL Server instances on the node.  Use the ALTER AVAILABILITY GROUP command to correct the availability group configuration.  Then, if the other replica is no longer being hosted on this node, restart this instance of SQL Server to bring the local replica of the availability group online.
                // ErrorCause: Another replica of the same availability group is already online on this node '%.*ls'.  Each node can host only one replica of an availability group, regardless of the number of SQL Server instances on the node.
                // ErrorCorrectiveAction: Use the ALTER AVAILABILITY GROUP command to correct the availability group configuration.  Then, if the other replica is no longer being hosted on this node, restart this instance of SQL Server to bring the local replica of the availability group online.
                case "41056":
                    break;

            }

        }
        private void DispatchEvent(PublishedEvent evt)
        {
            switch(evt.Name)
            {
                case "alwayson_ddl_executed":
                    HandleDdlExecuted(evt);
                    break;
                case "availability_group_lease_expired":
                    HandleAgLeaseExpired(evt);
                    break;
                case "availability_replica_manager_state_change":
                    HandleArMgrStateChange(evt);
                    break;
                case "availability_replica_state":
                    HandleArState(evt);
                    break;
                case "availability_replica_state_change":
                    HandleArStateChange(evt);
                    break;
                case "lock_redo_blocked":
                    HandleLockRedoBlocked(evt);
                    break;
                case "error_reported":
                    HandleErrorReported(evt);
                    break;
                default:
                    break;
            }
        }
        public void LoadData(string xelFileName, string serverName)
        {
            _instanceName = serverName;
            // load xel File
            using (QueryableXEventData events = new QueryableXEventData(xelFileName))
            {
                foreach(PublishedEvent evt in events)
                {
                    // dispatch event and handle by own method.

                        DispatchEvent(evt);
                }
            }

           
        }
        public void AnalyzeReports()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            foreach (AgReport rlMgr in pReportMgr.ReportIterator())
            {
                rlMgr.AnalyzeReport();
            }
        }
        public void ShowAgRoleTransition()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            foreach (AgReport rlMgr in pReportMgr.ReportIterator())
            {
                rlMgr.ShowReportArRoleTransition();
            }
        }

        public void ShowFailoverReports()
        {
            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;


            foreach (AgReport rlMgr in pReportMgr.ReportIterator())
            {
                rlMgr.ShowReport();
            }

        }

        // FIX IT, we need to fix this method. now ReportMgr is global
        public void MergeInstance(AlwaysOnData nextNode)
        {

            ReportMgr pReportMgr = ReportMgr.ReportMgrInstance;
            // fetch one AgReport and find same agName from another Data source
            foreach(var kvp in pReportMgr.ReportValuePairsIterator())
            {
                string pAgName = kvp.Key;
                AgReport pReport = kvp.Value;

                List<PartialReport> newList = new List<PartialReport>();

                if (nextNode.AgEventMap.TryGetValue(pAgName, out var nReportMgr))
                {
                    // pReport vs nReportMgr
                    // merge these two reportMgr
                    pReport.SortReports();
                    nReportMgr.SortReports();

                    List<PartialReport> pReports = pReport.Reports;
                    List<PartialReport> nReports = nReportMgr.Reports;

                    int i = 0;
                    int j = 0;

                    while(i < pReports.Count && j < nReports.Count)
                    { 
                        PartialReport left = pReports[i];
                        PartialReport right = nReports[j];
                        // NOT in the same time range
                        if( ((left.StartTime-right.EndTime).TotalMinutes >_defaultInterval) )
                        {
                            // push right to new list
                            newList.Add(right);
                            j++;

                        }else if 
                            ((right.StartTime - left.EndTime).TotalMinutes > _defaultInterval)  
                        {
                            // push left to new list
                            newList.Add(left);
                            i++;
                        }else
                        {
                            // TODO: report that merged compare with next left/right
                            // merge
                            // time

                            left.MergeReport(right);
                            newList.Add(left);
                            i++;j++;
                        }

                    }
                    pReport.Reports.Clear();
                    pReport.Reports = newList;

                }
                

            }
        }

    }
}
