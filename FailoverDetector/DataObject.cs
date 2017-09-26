using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.XEvent.Linq;
using System.Linq;

namespace FailoverDetector
{
    
    public class AlwaysOnData
    {

        readonly int DefaultInterval = 5;
        string instanceName;
        Dictionary<string, AgReportMgr> agEventMap;

        public Dictionary<string, AgReportMgr> AgEventMap { get => agEventMap; set => agEventMap = value; }

        public AlwaysOnData()
        {
            agEventMap = new Dictionary<string, AgReportMgr>();
        }
        enum AlwaysOn_EventType {
            DLL_EXECUTED,
            AG_LEASE_EXPIRED,
            AR_MANGER_STATE_CHANGE,
            AR_STATE,
            AR_STATE_CHANGE,
            LOCK_REDO_BLOCKED,
            ERROR
        }
        public void HandleDDLExecuted(PublishedEvent evt)
        {
            // find active alter ag failover
            // we only can find based on statement:
            // pattern is 
            // ALTER AVAILABILITY GROUP [ag_name] failover
            // ALTER AVAILABILITY GROUP ag_name force_failover_allow_data_loss
            string evt_statement = evt.Fields["statement"].Value.ToString();
            bool isForceFailover = ParseStatement(evt_statement);
            if (isForceFailover)
            {
                // receive a agName, which mean PrseStatement valid a failover statement
                // check fill report or populate a report
                bool commited = (evt.Fields["ddl_phase"].Value.ToString() == "commit");
                if (commited)
                {
                    string agName = evt.Fields["availability_group_name"].Value.ToString();
                    // get List of report for this ag
                    if (!agEventMap.TryGetValue(agName, out AgReportMgr m_reports))
                    {
                        m_reports = new AgReportMgr(agName, instanceName);
                        agEventMap.Add(agName, m_reports);
                    }
                    PartialReport pReport = m_reports.FGetReport(evt);
                    pReport.ForceFailoverFound = true;

                    m_reports.UpdateReport(pReport);

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

        public void HandleAGLeaseExpired(PublishedEvent evt)
        {
            string agName = evt.Fields["availability_group_name"].Value.ToString();
            // get List of report for this ag
            if (!agEventMap.TryGetValue(agName, out AgReportMgr m_reports))
            {
                m_reports = new AgReportMgr(agName, instanceName);
                agEventMap.Add(agName, m_reports);
            }
            PartialReport pReport = m_reports.FGetReport(evt);
            pReport.LeaseTimeoutFound = true;
            m_reports.UpdateReport(pReport);
        }
        public void HandleARMgrStateChange(PublishedEvent evt)
        {
;
        }
        public void HandleARState(PublishedEvent evt) { }
        public void HandleARStateChange(PublishedEvent evt)
        {
            string agName = evt.Fields["availability_group_name"].Value.ToString();
            // get List of report for this ag
            if (!agEventMap.TryGetValue(agName, out AgReportMgr m_reports))
            {
                m_reports = new AgReportMgr(agName, instanceName);
                agEventMap.Add(agName, m_reports);
            }
            PartialReport pReport = m_reports.FGetReport(evt);
            if(pReport.IsEmptyRole())
            {
                pReport.AddRoleTransition(evt.Fields["previous_state"].Value.ToString());
            }
            pReport.AddRoleTransition(evt.Fields["current_state"].Value.ToString());

            m_reports.UpdateReport(pReport);

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
                    HandleDDLExecuted(evt);
                    break;
                case "availability_group_lease_expired":
                    HandleAGLeaseExpired(evt);
                    break;
                case "availability_replica_manager_state_change":
                    HandleARMgrStateChange(evt);
                    break;
                case "availability_replica_state":
                    HandleARState(evt);
                    break;
                case "availability_replica_state_change":
                    HandleARStateChange(evt);
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
        public void loadData(string xelFileName, string serverName)
        {
            instanceName = serverName;
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
            Dictionary<string, AgReportMgr>.ValueCollection rlMgrColl = agEventMap.Values;
            foreach (AgReportMgr rlMgr in rlMgrColl)
            {
                rlMgr.AnalyzeReport();
            }
        }
        public void ShowAGRoleTransition()
        {
            Dictionary<string, AgReportMgr>.ValueCollection rlMgrColl = agEventMap.Values;
            foreach (AgReportMgr rlMgr in rlMgrColl)
            {
                rlMgr.ShowReportArRoleTransition();
            }
        }

        public void ShowFailoverReports()
        {
            Dictionary<string, AgReportMgr>.ValueCollection rlMgrColl = agEventMap.Values;
            foreach (AgReportMgr rlMgr in rlMgrColl)
            {
                rlMgr.ShowReport();
            }

        }

        public void MergeInstance(AlwaysOnData nextNode)
        {

            // fetch one AgReportMgr and find same agName from another Data source
            foreach(KeyValuePair<string,AgReportMgr> kvp in agEventMap)
            {
                string pAgName = kvp.Key;
                AgReportMgr pReportMgr = kvp.Value;
                AgReportMgr nReportMgr;

                List<PartialReport> new_list = new List<PartialReport>();

                if (nextNode.agEventMap.TryGetValue(pAgName, out nReportMgr))
                {
                    // pReportMgr vs nReportMgr
                    // merge these two reportMgr
                    pReportMgr.SortReports();
                    nReportMgr.SortReports();

                    List<PartialReport> pReports = pReportMgr.Reports;
                    List<PartialReport> nReports = nReportMgr.Reports;

                    int i = 0;
                    int j = 0;

                    while(i < pReports.Count && j < nReports.Count)
                    { 
                        PartialReport left = pReports[i];
                        PartialReport right = nReports[j];
                        // NOT in the same time range
                        if( ((left.StartTime-right.EndTime).TotalMinutes >DefaultInterval) )
                        {
                            // push right to new list
                            new_list.Add(right);
                            j++;

                        }else if 
                            ((right.StartTime - left.EndTime).TotalMinutes > DefaultInterval)  
                        {
                            // push left to new list
                            new_list.Add(left);
                            i++;
                        }else
                        {
                            // TODO: report that merged compare with next left/right
                            // merge
                            // time

                            left.MergeReport(right);
                            new_list.Add(left);
                            i++;j++;
                        }

                    }
                    pReportMgr.Reports.Clear();
                    pReportMgr.Reports = new_list;

                }
                

            }
        }

    }
}
