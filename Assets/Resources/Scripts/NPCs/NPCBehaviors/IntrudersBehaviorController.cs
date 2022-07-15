using System.Collections.Generic;
using UnityEngine;

public class IntrudersBehaviorController : MonoBehaviour
{
    List<BehaviorPerformanceSnapshot> _decisionTimes;

    private Behavior m_behavior;

    public Behavior behavior
    {
        get { return m_behavior; }
    }

    // The controller for intruders behavior when they have never been spotted.
    private Scouter m_Scouter;

    private ChaseEvader m_ChaseEvader;

    private SearchEvader m_SearchEvader;

    private bool noIntruders = true;

    public void Initiate(Session session, MapManager mapManager)
    {
        noIntruders = session.GetIntrudersData().Count == 0;

        if (noIntruders) return;

        m_behavior = session.GetIntrudersData()[0].behavior;

        _decisionTimes = new List<BehaviorPerformanceSnapshot>();

        switch (behavior.patrol)
        {
            case PatrolPlanner.iSimple:
                m_Scouter = gameObject.AddComponent<SimpleGreedyScouter>();
                break;

            case PatrolPlanner.iRoadMap:
                m_Scouter = gameObject.AddComponent<RoadMapScouter>();
                break;

            case PatrolPlanner.iPathFinding:
                m_Scouter = gameObject.AddComponent<GreedyToGoalScouter>();
                break;

            case PatrolPlanner.UserInput:
                break;
        }

        switch (behavior.alert)
        {
            case AlertPlanner.iHeuristic:
                m_ChaseEvader = gameObject.AddComponent<SimpleChaseEvader>();
                break;

            case AlertPlanner.UserInput:
                break;
        }


        switch (behavior.search)
        {
            case SearchPlanner.iHeuristic:
                m_SearchEvader = gameObject.AddComponent<SimpleSearchEvader>();
                break;

            case SearchPlanner.UserInput:
                return;
        }

        m_Scouter?.Initiate(mapManager, session);
        m_ChaseEvader?.Initiate(mapManager);
        m_SearchEvader?.Initiate(mapManager);
    }

    public void Reset()
    {
        LogResults();
        _decisionTimes.Clear();
    }

    private void LogResults()
    {
        if (_decisionTimes.Count == 0) return;

        if (!Equals(GameManager.Instance.loggingMethod, Logging.None))
            CsvController.WriteString(
                CsvController.GetPath(StealthArea.SessionInfo, FileType.RunningTimes, null),
                GetResult(CsvController.IsFileExist(StealthArea.SessionInfo, FileType.RunningTimes, null)), true);
    }

    private string GetResult(bool isFileExist)
    {
        if (_decisionTimes != null)
        {
            // Write the exploration results for this episode
            string data = "";

            if (!isFileExist) data += BehaviorPerformanceSnapshot.Headers + ",EpisodeID" + "\n";

            foreach (var decisionTime in _decisionTimes)
                data += decisionTime + "," +  + PerformanceLogger.Instance.GetEpisodeNo() +"\n";
            return data;
        }

        return "";
    }


    public void StartScouter()
    {
        if (Equals(behavior.patrol, PatrolPlanner.UserInput) || noIntruders) return;

        m_Scouter.Begin();
    }

    public void StayIncognito(GameType gameType)
    {
        if (Equals(behavior.search, SearchPlanner.UserInput) || noIntruders) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        m_Scouter?.Refresh(gameType);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(m_Scouter.GetType().Name, elapsedMs));
    }

    public void StartChaseEvader()
    {
        if (Equals(behavior.alert, AlertPlanner.UserInput) || noIntruders) return;

        m_ChaseEvader.Begin();
    }

    // Intruder behavior when being chased
    public void KeepRunning()
    {
        if (Equals(behavior.alert, AlertPlanner.UserInput) || noIntruders) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        m_ChaseEvader.Refresh();
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(m_ChaseEvader.GetType().Name, elapsedMs));
    }


    public void StartHiding()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput) || noIntruders) return;

        m_SearchEvader.Begin();
    }


    // Intruder behavior after escaping guards
    public void KeepHiding()
    {
        if (Equals(behavior.search, SearchPlanner.UserInput) || noIntruders) return;

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