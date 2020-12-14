using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceMonitor : MonoBehaviour
{
    private StealthArea m_stealthArea;

    public List<LogSnapshot> snapshots;

    // Number of episodes done
    private int m_episodeCount = 0;

    private bool m_addHeader = false;


    public void ResetResults()
    {
        snapshots = new List<LogSnapshot>();
    }

    public void SetArea()
    {
        m_stealthArea = transform.parent.GetComponent<StealthArea>();
        GetEpisodesCountInLogs();
    }

    // Update the Episode count if there are any before
    public void GetEpisodesCountInLogs()
    {
        Session sa = m_stealthArea.GetScenario();

        m_episodeCount = CsvController.ReadEpisodesCount(CsvController.GetPath(sa.gameCode, sa.GetMapScale(), sa.worldRepType.ToString(), sa.map.ToString(), sa.coveredRegionResetThreshold, "summary"));

        if (m_episodeCount == 0)
            m_addHeader = true;
        else
            m_addHeader = false;
    }

    // Did the scenario recorded the required number of episodes
    public bool IsDone()
    {
        return m_episodeCount >= Properties.EpisodesCount;
    }

    public void UpdateProgress(LogSnapshot logSnapshot)
    {
        snapshots.Add(logSnapshot);
    }

    // Append the Episode performance to the log
    public void LogEpisodeFinish()
    {
        Session sa = m_stealthArea.GetScenario();

        // Increment the episode counter
        m_episodeCount++;

        // make sure the data list is non empty
        if (snapshots.Count > 0)
        {
            CsvController.WriteString(
                CsvController.GetPath(sa.gameCode, sa.GetMapScale(), sa.worldRepType.ToString(), sa.map.ToString(),
                    sa.coveredRegionResetThreshold, "details"),
                GetEpisodeResults(sa.gameCode), true);

            // Update latest episode count
            UpdateEpisodeCount(sa.gameCode, sa.GetMapScale(), sa.worldRepType.ToString(), sa.map.ToString(), sa.coveredRegionResetThreshold);
        }

        // Reset results
        ResetResults();
    }


    // return the data of the episode's result into a string
    public string GetEpisodeResults(string gameCode)
    {
        if ( snapshots != null)
        {
            // Write the exploration results for this episode
            string data = "";

            if (m_addHeader)
            {
                data +=
                    "gameCode,episodeID,guardType,guardId,guardPlanner,guardHeuristic,guardPathFollowing,elapsedTime,distanceTravelled,state,alertTime,searchedTime,foundHidingSpots,stalenessAverages\n";
                m_addHeader = false;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                data += gameCode + "," + m_episodeCount + "," + snapshots[i] + "\n";
            }

            return data;
        }


        return "";
    }
    
    // Log Episode data and reset
    public void UpdateEpisodeCount(string gameCode, float mapScale, string worldRep, string mapName, int resetThreshold)
    {
        CsvController.WriteString(CsvController.GetPath(gameCode, mapScale, worldRep, mapName, resetThreshold, "summary"),
            m_episodeCount.ToString(),
            false);
    }
}


public struct LogSnapshot
{
    // Total distance travelled by the npc
    public float TravelledDistance;
    // Elapsed time of the episode
    public float ElapsedTime;
    // Details of the npcs
    public NpcData NpcDetail;
    
    // Current state of the NPC
    public string State;
    
    // Total time under alert
    public float AlertTime;
    
    // Total time being search for
    public float SearchTime;

    // Total found spots 
    public int FoundHidingSpots;
    
    // Average staleness of the map
    public float StalenessAverage;


    public LogSnapshot(float travelledDistance, float elapsedTime, NpcData npcData, string npcState, float alertTime, float searchTime, int foundHidingSpots,
        float stalenessAverage)
    {
        TravelledDistance = travelledDistance;
        ElapsedTime = elapsedTime;
        NpcDetail = npcData;
        State = npcState;
        AlertTime = alertTime;
        SearchTime = searchTime;
        FoundHidingSpots = foundHidingSpots;
        StalenessAverage = stalenessAverage;
    }

    public override string ToString()
    {
        string output =  NpcDetail + "," + ElapsedTime + "," +
                         TravelledDistance + "," + State + "," + AlertTime + "," + SearchTime + "," + FoundHidingSpots +
                         "," + StalenessAverage;


        return output;
    }
}