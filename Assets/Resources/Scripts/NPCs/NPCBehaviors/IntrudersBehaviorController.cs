using System;
using System.Collections.Generic;
using UnityEngine;

public class IntrudersBehaviorController : MonoBehaviour
{
    List<BehaviorPerformanceSnapshot> _decisionTimes;

    // The controller for intruders behavior when they have never been spotted.
    private Scouter m_Scouter;

    private ChaseEvader m_ChaseEvader;

    private SearchEvader m_SearchEvader;

    private bool noIntruders = true;

    public void Initiate(Session session, MapManager mapManager)
    {
        noIntruders = session.GetIntrudersData().Count == 0;

        if (noIntruders) return;

        _decisionTimes = new List<BehaviorPerformanceSnapshot>();

        Type scouterType = session.IntruderBehaviorParams.scouterParams?.GetType();

        if (scouterType == typeof(SimpleGreedyScouterParams))
            m_Scouter = gameObject.AddComponent<SimpleGreedyScouter>();
        else if (scouterType == typeof(RoadMapScouterParams))
            m_Scouter = gameObject.AddComponent<RoadMapScouter>();
        else if (scouterType == typeof(GreedyToGoalScouterParams))
            m_Scouter = gameObject.AddComponent<GreedyToGoalScouter>();

        Type chaseEvaderType = session.IntruderBehaviorParams.chaseEvaderParams?.GetType();

        if (chaseEvaderType == typeof(SimpleChaseEvaderParams))
            m_ChaseEvader = gameObject.AddComponent<SimpleChaseEvader>();

        Type searchEvaderType = session.IntruderBehaviorParams.searchEvaderParams?.GetType();

        if (searchEvaderType == typeof(SimpleSearchEvaderParams))
            m_SearchEvader = gameObject.AddComponent<SimpleSearchEvader>();

        m_Scouter?.Initiate(mapManager, session);
        m_ChaseEvader?.Initiate(mapManager);
        m_SearchEvader?.Initiate(mapManager);
    }

    public void Reset()
    {
        LogResults();
        _decisionTimes?.Clear();
    }

    private void LogResults()
    {
        if (!Equals(GameManager.Instance.loggingMethod, Logging.Local)) return;
        if (!GameManager.Instance.RecordRunningTimes) return;

        noIntruders = StealthArea.SessionInfo.GetIntrudersData().Count == 0;

        if (noIntruders) return;

        if (_decisionTimes.Count == 0) return;

        if (!Equals(GameManager.Instance.loggingMethod, Logging.None))
            CsvController.WriteString(
                CsvController.GetPath(StealthArea.SessionInfo, FileType.RunningTimesIntruder, null),
                GetResult(CsvController.IsFileExist(StealthArea.SessionInfo, FileType.RunningTimesIntruder, null)),
                true);

        _decisionTimes.Clear();
    }

    private string GetResult(bool isFileExist)
    {
        if (_decisionTimes == null) return "";

        // Write the exploration results for this episode
        string data = "";

        if (!isFileExist) data += BehaviorPerformanceSnapshot.Headers + ",EpisodeID" + "\n";

        foreach (var decisionTime in _decisionTimes)
            data += decisionTime + "," + +StealthArea.SessionInfo.currentEpisode + "\n";
        return data;
    }


    public void StartScouter()
    {
        m_Scouter?.Begin();
    }

    public void StayIncognito(GameType gameType)
    {
        if (Equals(m_Scouter, null)) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        m_Scouter.Refresh(gameType);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(m_Scouter.GetType().Name, elapsedMs));
    }

    public void StartChaseEvader()
    {
        m_ChaseEvader?.Begin();
    }

    // Intruder behavior when being chased
    public void KeepRunning()
    {
        if (Equals(m_ChaseEvader, null)) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        m_ChaseEvader.Refresh();
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(m_ChaseEvader.GetType().Name, elapsedMs));
    }


    public void StartHiding()
    {
        m_SearchEvader?.Begin();
    }


    // Intruder behavior after escaping guards
    public void KeepHiding()
    {
        if (Equals(m_SearchEvader, null)) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        m_SearchEvader.Refresh();
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(m_SearchEvader.GetType().Name, elapsedMs));
    }
}

public struct BehaviorPerformanceSnapshot
{
    public static string Headers = "label,duration(MS)";
    public string label;
    public float duration;

    public BehaviorPerformanceSnapshot(string _label, float _duration)
    {
        label = _label;
        duration = _duration;
    }

    public override string ToString()
    {
        return label + "," + duration;
    }
}