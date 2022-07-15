using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class PerformanceLogger : MonoBehaviour
{
    public Session Sa;

    // Last timestamp the game was logged.
    private float _lastLoggedTime;

    // Number of episodes done
    private int _episodeCount;

    private List<LogSnapshot> _snapshots;

    private Dictionary<string, NPCReplay> _npcsRecording;

    public static PerformanceLogger Instance; 

    public void Initialize()
    {
        _snapshots = new List<LogSnapshot>();
        _npcsRecording = new Dictionary<string, NPCReplay>();
    }

    public void ResetResults()
    {
        Instance = this;
        
        _lastLoggedTime = 0f;

        _snapshots.Clear();
        _npcsRecording.Clear();

        foreach (var guard in NpcsManager.Instance.GetGuards())
            _npcsRecording[guard.name] = new NPCReplay(guard.GetNpcData().id, guard.GetNpcData().npcType);


        foreach (var intruder in NpcsManager.Instance.GetIntruders())
            _npcsRecording[intruder.name] = new NPCReplay(intruder.GetNpcData().id, intruder.GetNpcData().npcType);
    }

    public void SetArea(Session sa)
    {
        Sa = sa;

        if (GameManager.Instance.loggingMethod == Logging.Local)
            GetEpisodesCountInLogs();
    }

    private bool IsTimeToLog()
    {
        if (StealthArea.GetElapsedTimeInSeconds() - _lastLoggedTime >= 0.5f)
        {
            _lastLoggedTime = StealthArea.GetElapsedTimeInSeconds();
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
        return _episodeCount;
    }

    // Update the Episode count if there are any before
    private void GetEpisodesCountInLogs()
    {
        // _episodeCount = CsvController.ReadFileStartWith(FileType.Performance, Sa);
        _episodeCount = CsvController.GetFileLength(CsvController.GetPath(Sa, FileType.Performance, null));
    }

    public static bool IsLogged(Session sa)
    {
        // int episodeCount = CsvController.ReadFileStartWith(FileType.Performance, sa);
        int episodeCount = CsvController.GetFileLength(CsvController.GetPath(sa, FileType.Performance, null));
        return episodeCount >= Properties.EpisodesCount;
    }

    // Did the scenario recorded the required number of episodes
    public bool IsDone()
    {
        return _episodeCount >= Properties.EpisodesCount;
    }

    private void UpdateProgress()
    {
        float timeElapsed = StealthArea.GetElapsedTimeInSeconds();

        // foreach (var guard in NpcsManager.Instance.GetGuards())
        // {
        //     _snapshots.Add(guard.LogNpcProgress());
        //     _npcsRecording[guard.name].AddSnapshot(new ReplaySnapshot(timeElapsed,
        //         new Position2D(guard.GetTransform().position.x, guard.GetTransform().position.y)));
        // }

        foreach (var intruder in NpcsManager.Instance.GetIntruders())
        {
            _snapshots.Add(intruder.LogNpcProgress());

            _npcsRecording[intruder.name]
                .AddSnapshot(new ReplaySnapshot(timeElapsed,
                    new Position2D(intruder.GetTransform().position.x, intruder.GetTransform().position.y)));
        }
    }


    public void IncrementEpisode()
    {
        // Increment the episode counter
        _episodeCount++;
    }

    // Append the Episode performance to the log
    private void LogEpisodeFinish()
    {
        // make sure the data list is non empty
        if (_snapshots.Count > 0)
        {
            CsvController.WriteString(
                CsvController.GetPath(Sa, FileType.Performance, null),
                GetLastResult(CsvController.IsFileExist(Sa, FileType.Performance, null)), true);

            // CsvController.WriteString(
            //     CsvController.GetPath(Sa, FileType.Performance, _episodeCount),
            //     GetEpisodeResults(), true);

            // CsvController.WriteString(
            //     CsvController.GetPath(Sa, FileType.Npcs, _episodeCount),
            //     GetNpcDataJson(), true);
        }

        // Reset results
        ResetResults();
    }

    // Upload the results to the server
    private void UploadEpisodeData()
    {
        StartCoroutine(FileUploader.UploadData(Sa, FileType.Performance, "text/csv", GetEpisodeResults(false)));
        StartCoroutine(FileUploader.UploadData(Sa, FileType.Npcs, "text/csv", GetNpcDataJson()));
    }

    private string GetNpcDataJson()
    {
        string output = JsonConvert.SerializeObject(_npcsRecording, Formatting.None,
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

        // var object = JsonConvert.DeserializeObject<Dictionary<string, NPCReplay>>(json);
        return output;
    }

    // return the data of the episode's result into a string
    private string GetEpisodeResults(bool isFileExist)
    {
        if (_snapshots != null)
        {
            // Write the exploration results for this episode
            string data = "";

            if (!isFileExist)
                data += LogSnapshot.Headers + "\n";

            foreach (var snapshot in _snapshots)
                data += snapshot + "\n";

            return data;
        }

        return "";
    }


    private string GetLastResult(bool isFileExist)
    {
        if (_snapshots != null)
        {
            // Write the exploration results for this episode
            string data = "";

            if (!isFileExist)
                data += "episodeId," + LogSnapshot.Headers + "\n";

            data += _episodeCount + "," + _snapshots[_snapshots.Count - 1] + "\n";

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
    public static string Headers = //NpcData.Headers +
        "ElapseTime,TravelledDistance,State,CollectedCoin";

    public override string ToString()
    {
        string output = //NpcDetail + "," + 
            ElapsedTime +
            "," + TravelledDistance +
            "," + State +
            // "," + AlertTime +
            // "," + SearchTime +
            // "," + GuardsOverlapTime +
            // "," + FoundHidingSpots +
            // "," + StalenessAverage +
            "," + CollectedCoin;
        // + "," + Score;

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

    public override string ToString()
    {
        return "(" + x + "," + y + ")";
    }
}