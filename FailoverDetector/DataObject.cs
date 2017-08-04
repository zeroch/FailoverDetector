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
            string[] compare = { "alter", "availability", "group" };
            string[] failover = { "failover", "force_failover_allow_data_loss" };

            string command = String.Join(" ", words.Take(3)).ToLower();
            string parameter = words[4].ToLower().TrimEnd(';');
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
        public void HandleErrorReported(PublishedEvent evt) { }
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
