using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerformanceMonitor : MonoBehaviour
{
    public Session Sa;

    public List<LogSnapshot> snapshots;

    // Number of episodes done
    private int m_episodeCount;

    public void ResetResults()
    {
        snapshots = new List<LogSnapshot>();
    }

    public void SetArea(Session sa)
    {
        Sa = sa;
        GetEpisodesCountInLogs();
    }

    // Update the Episode count if there are any before
    public void GetEpisodesCountInLogs()
    {
        m_episodeCount = CsvController.ReadFileStartWith(Sa);
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

    public void IncrementEpisode()
    {
        // Increment the episode counter
        m_episodeCount++;
    }

    // Append the Episode performance to the log
    public void LogEpisodeFinish()
    {
        IncrementEpisode();

        // make sure the data list is non empty
        if (snapshots.Count > 0)
        {
            CsvController.WriteString(
                CsvController.GetPath(Sa, m_episodeCount),
                GetEpisodeResults(), true);
        }

        // Reset results
        ResetResults();
    }

    // Upload the results to the server
    public void UploadEpisodeData()
    {
        StartCoroutine(FileUploader.UploadLevel(Sa, GetEpisodeResults()));
    }


    // return the data of the episode's result into a string
    public string GetEpisodeResults()
    {
        if (snapshots != null)
        {
            // Write the exploration results for this episode
            string data = "";

            data +=
                "gameCode,guardType,guardId,guardPlanner,guardHeuristic,guardPathFollowing,elapsedTime,distanceTravelled,state,NoTimesSpotted,alertTime,searchedTime,foundHidingSpots,stalenessAverages\n";

            for (int i = 0; i < snapshots.Count; i++)
            {
                data += Sa.gameCode + "," + snapshots[i] + "\n";
            }

            return data;
        }

        return "";
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

    // Number of times and intruder is spotted
    public int NoTimesSpotted;

    // Total time under alert
    public float AlertTime;

    // Total time being search for
    public float SearchTime;

    // Total found spots 
    public int FoundHidingSpots;

    // Average staleness of the map
    public float StalenessAverage;


    public LogSnapshot(float travelledDistance, float elapsedTime, NpcData npcData, string npcState, int noTimesSpotted,
        float alertTime,
        float searchTime, int foundHidingSpots,
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
        NoTimesSpotted = noTimesSpotted;
    }

    public override string ToString()
    {
        string output = NpcDetail + "," + ElapsedTime + "," +
                        TravelledDistance + "," + State + "," + NoTimesSpotted + "," + AlertTime + "," + SearchTime +
                        "," + FoundHidingSpots +
                        "," + StalenessAverage;

        return output;
    }
}