using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class PerformanceLogger : MonoBehaviour
{
    public Session Sa;

    // Last timestamp the game was logged.
    private float m_lastLoggedTime;

    // Number of episodes done
    private int m_episodeCount;

    private List<LogSnapshot> snapshots;

    private Dictionary<string, NPCReplay> m_npcsRecording;


    public void Initialize()
    {
        snapshots = new List<LogSnapshot>();
        m_npcsRecording = new Dictionary<string, NPCReplay>();
    }

    public void ResetResults()
    {
        m_lastLoggedTime = 0f;

        snapshots.Clear();
        m_npcsRecording.Clear();

        foreach (var guard in NpcsManager.Instance.GetGuards())
            m_npcsRecording[guard.name] = new NPCReplay(guard.GetNpcData().id, guard.GetNpcData().npcType);


        foreach (var intruder in NpcsManager.Instance.GetIntruders())
            m_npcsRecording[intruder.name] = new NPCReplay(intruder.GetNpcData().id, intruder.GetNpcData().npcType);
    }

    public void SetArea(Session sa)
    {
        Sa = sa;

        if (GameManager.Instance.loggingMethod == Logging.Local)
            GetEpisodesCountInLogs();
    }

    public bool IsTimeToLog()
    {
        if (StealthArea.GetElapsedTime() - m_lastLoggedTime >= 0.5f)
        {
            m_lastLoggedTime = StealthArea.GetElapsedTime();
            return true;
        }

        return false;
    }

    public void Log()
    {
        if ((GameManager.Instance.loggingMethod != Logging.None) && IsTimeToLog())
            UpdateProgress();
    }
    
    public void FinalizeLogging(Logging type)
    {
        UpdateProgress();

        switch (type)
        {
            // Log the overall performance in case of local logging.
            case Logging.Local:
                LogEpisodeFinish();
                IncrementEpisode();
                break;

            // Log the performance of this episode and upload it to the server.
            case Logging.Cloud:
                UploadEpisodeData();
                break;

            case Logging.None:
                break;
        }
    }



    public int GetEpisodeNo()
    {
        return m_episodeCount;
    }

    // Update the Episode count if there are any before
    public void GetEpisodesCountInLogs()
    {
        m_episodeCount = CsvController.ReadFileStartWith(FileType.Performance, Sa);
    }

    public static bool IsLogged(Session sa)
    {
        int episodeCount = CsvController.ReadFileStartWith(FileType.Performance, sa);

        return episodeCount >= Properties.EpisodesCount;
    }

    // Did the scenario recorded the required number of episodes
    public bool IsDone()
    {
        return m_episodeCount >= Properties.EpisodesCount;
    }

    public void UpdateProgress()
    {
        float timeElapsed = StealthArea.GetElapsedTime();

        foreach (var guard in NpcsManager.Instance.GetGuards())
        {
            snapshots.Add(guard.LogNpcProgress());
            m_npcsRecording[guard.name].AddSnapshot(new ReplaySnapshot(timeElapsed,
                new Position2D(guard.GetTransform().position.x, guard.GetTransform().position.y)));
        }

        foreach (var intruder in NpcsManager.Instance.GetIntruders())
        {
            snapshots.Add(intruder.LogNpcProgress());
            m_npcsRecording[intruder.name]
                .AddSnapshot(new ReplaySnapshot(timeElapsed,
                    new Position2D(intruder.GetTransform().position.x, intruder.GetTransform().position.y)));
        }
    }


    public void IncrementEpisode()
    {
        // Increment the episode counter
        m_episodeCount++;
    }

    // Append the Episode performance to the log
    public void LogEpisodeFinish()
    {
        // make sure the data list is non empty
        if (snapshots.Count > 0)
        {
            CsvController.WriteString(
                CsvController.GetPath(Sa, FileType.Performance, m_episodeCount),
                GetEpisodeResults(), true);

            CsvController.WriteString(
                CsvController.GetPath(Sa, FileType.Npcs, m_episodeCount),
                GetNpcDataJson(), true);
        }

        // Reset results
        ResetResults();
    }

    // Upload the results to the server
    public void UploadEpisodeData()
    {
        StartCoroutine(FileUploader.UploadData(Sa, FileType.Performance, "text/csv", GetEpisodeResults()));
        StartCoroutine(FileUploader.UploadData(Sa, FileType.Npcs, "text/csv", GetNpcDataJson()));
    }

    private string GetNpcDataJson()
    {
        string output = JsonConvert.SerializeObject(m_npcsRecording, Formatting.None,
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

        // var object = JsonConvert.DeserializeObject<Dictionary<string, NPCReplay>>(json);
        return output;
    }

    // return the data of the episode's result into a string
    public string GetEpisodeResults()
    {
        if (snapshots != null)
        {
            // Write the exploration results for this episode
            string data = "";

            data +=
                "GameCode," + LogSnapshot.Headers +
                "\n"; //"guardType,guardId,guardPlanner,guardHeuristic,guardPathFollowing,elapsedTime,distanceTravelled,state,NoTimesSpotted,alertTime,searchedTime,GuardsOverlapTime,foundHidingSpots,stalenessAverages\n";

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

    // Guards overlap time
    public float GuardsOverlapTime;

    // Total time under alert
    public float AlertTime;

    // Total time being search for
    public float SearchTime;

    // Total found spots 
    public int FoundHidingSpots;

    // Average staleness of the map
    public float StalenessAverage;

    // Number of coins collect
    public int CollectedCoin;

    public float Score;


    public LogSnapshot(float travelledDistance, float elapsedTime, NpcData npcData, string npcState, int noTimesSpotted,
        float guardOverlapTime,
        float alertTime,
        float searchTime, int foundHidingSpots,
        float stalenessAverage, int collectedCoin)
    {
        TravelledDistance = travelledDistance;
        ElapsedTime = elapsedTime;
        NpcDetail = npcData;
        State = npcState;
        AlertTime = alertTime;
        SearchTime = searchTime;
        GuardsOverlapTime = guardOverlapTime;
        FoundHidingSpots = foundHidingSpots;
        StalenessAverage = stalenessAverage;
        NoTimesSpotted = noTimesSpotted;
        CollectedCoin = collectedCoin;
        Score = ScoreController.Instance.score;
    }

    // Headers
    public static string Headers = NpcData.Headers +
                                   ",ElapseTime,TravelledDistance,State,NoTimesSpotted,AlertTime,SearchTime,GuardsOverlapTime,FoundHidingSpots,StalenessAverage,CollectedCoin,Score";

    public override string ToString()
    {
        string output = NpcDetail +
                        "," + ElapsedTime +
                        "," + TravelledDistance +
                        "," + State +
                        "," + NoTimesSpotted +
                        "," + AlertTime +
                        "," + SearchTime +
                        "," + GuardsOverlapTime +
                        "," + FoundHidingSpots +
                        "," + StalenessAverage +
                        "," + CollectedCoin +
                        "," + Score;

        return output;
    }
}

[Serializable]
public class NPCReplay
{
    public int npcId;
    public NpcType npcType;
    public List<ReplaySnapshot> replaySnapshots;

    public NPCReplay(int _npcId, NpcType _npcType)
    {
        npcId = _npcId;
        npcType = _npcType;
        replaySnapshots = new List<ReplaySnapshot>();
    }

    public void AddSnapshot(ReplaySnapshot snapshot)
    {
        replaySnapshots.Add(snapshot);
    }
}

/// <summary>
/// A snapshot of the agents reply
/// </summary>
[Serializable]
public struct ReplaySnapshot
{
    public float timeStamp;

    public Position2D position;

    public ReplaySnapshot(float _timeSnapshot, Position2D _position)
    {
        timeStamp = _timeSnapshot;
        position = _position;
    }
}

[Serializable]
public struct Position2D
{
    public float x;
    public float y;

    public Position2D(float _x, float _y)
    {
        x = _x;
        y = _y;
    }
}