using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardsBehaviorController : MonoBehaviour
{
    List<BehaviorPerformanceSnapshot> _decisionTimes;

    // Guards behavior managers
    private Patroler patroler;

    // Search controller for controlling how guards searched for an intruder.
    private Searcher searcher;

    // Interceptor controller for controlling how guards act when chasing an intruder in sight.
    private Interceptor interceptor;

    // Possible locations to search for the intruder in
    private List<InterceptionPoint> m_possiblePositions;

    public void Initiate(Session session, MapManager mapManager)
    {
        _decisionTimes = new List<BehaviorPerformanceSnapshot>();

        Type patrolerType = session.guardBehaviorParams.patrolerParams?.GetType();

        if (patrolerType == typeof(RoadMapPatrolerParams))
            patroler = gameObject.AddComponent<RoadMapPatroler>();
        else if (patrolerType == typeof(VisMeshPatrolerParams))
            patroler = gameObject.AddComponent<VisMeshPatroler>();
        else if (patrolerType == typeof(GridPatrolerParams))
            patroler = gameObject.AddComponent<GridPatroler>();
        else if (patrolerType == typeof(RandomPatrolerParams))
            patroler = gameObject.AddComponent<RandomPatroler>();
        else if (patrolerType == typeof(ScriptedPatrolerParams))
            patroler = gameObject.AddComponent<ScriptedPatroler>();

        patroler?.Initiate(mapManager, session.guardBehaviorParams);

        // Interception controller
        interceptor = gameObject.AddComponent<Interceptor>();
        interceptor.Initiate(mapManager);

        Type searcherType = session.guardBehaviorParams.searcherParams?.GetType();

        if (searcherType == typeof(GridSearcherParams))
            searcher = gameObject.AddComponent<GridSearcher>();
        else if (searcherType == typeof(RoadMapSearcherParams))
            searcher = gameObject.AddComponent<RoadMapSearcher>();
        else if (searcherType == typeof(CheatingSearcherParams))
            searcher = gameObject.AddComponent<CheatingSearcher>();
        else if (searcherType == typeof(RandomSearcherParams))
            searcher = gameObject.AddComponent<RandomSearcher>();

        searcher?.Initiate(mapManager, session.guardBehaviorParams);
    }

    public void Reset()
    {
        LogResults();
        _decisionTimes.Clear();
    }

    private void LogResults()
    {
        if (!GameManager.Instance.RecordRunningTimes) return;
        if (_decisionTimes.Count == 0) return;

        if (!Equals(GameManager.Instance.loggingMethod, Logging.None))
            CsvController.WriteString(
                CsvController.GetPath(StealthArea.SessionInfo, FileType.RunningTimesGuard, null),
                GetResult(CsvController.IsFileExist(StealthArea.SessionInfo, FileType.RunningTimesGuard, null)), true);
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


    // Start patrol shift
    public void StartShift()
    {
        patroler?.Start();
    }

    // Order Guards to patrol
    public void Patrol()
    {
        if (Equals(patroler, null)) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        patroler.UpdatePatroler(NpcsManager.Instance.GetGuards(), 0.3f, Time.deltaTime);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(patroler.GetType().Name + " Update", elapsedMs));


        watch = System.Diagnostics.Stopwatch.StartNew();
        patroler.Patrol(NpcsManager.Instance.GetGuards());
        watch.Stop();
        elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(patroler.GetType().Name + " Decision", elapsedMs));
    }


    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch(Intruder intruder)
    {
        // Flow the probability for intruder positions
        searcher?.StartSearch(intruder);

        // Order the guards to go the intruder's last known position
        foreach (var guard in NpcsManager.Instance.GetGuards())
            guard.SetDestination(intruder.GetLastKnownLocation().Value, true, true);
    }


    // Keep searching for the intruder
    public void Search(Intruder intruder)
    {
        if (Equals(searcher, null)) return;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        searcher.UpdateRepresentation(intruder.GetNpcSpeed(), NpcsManager.Instance.GetGuards(), Time.deltaTime);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(searcher.GetType().Name + " Update", elapsedMs));


        watch = System.Diagnostics.Stopwatch.StartNew();
        searcher.Decide(NpcsManager.Instance.GetGuards());
        watch.Stop();
        elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(searcher.GetType().Name + " Decision", elapsedMs));
    }

    public void ClearSearch()
    {
        searcher?.Clear();
    }

    /// <summary>
    /// Update the guard behavior
    /// </summary>
    /// <param name="timeDelta"></param>
    public void UpdateBehaviorController(Intruder intruder, float timeDelta)
    {
        // Update the search area in case the guards are searching for an intruder
        // Move and propagate the possible intruder position (phantoms)
        if (GetState() is Search)
        {
            searcher.UpdateRepresentation(intruder.GetNpcSpeed(), NpcsManager.Instance.GetGuards(), timeDelta);
        }
    }


    // In case of intruder is seen
    public void StartChase(Intruder intruder)
    {
        // Switch to chase state
        if (GetState() is Chase) return;

        // The region the intruder was last seen in 
        WorldState.Set("intruder_last_seen_region",
            RegionLabelsManager.GetRegion(intruder.GetTransform().position));

        interceptor.Clear();
    }

    // Order guards to chase
    public void Chase(Intruder intruder)
    {
        // The score reduction rate while being chased
        float decreaseRate = 3f * Properties.SpeedMultiplyer;

        // Loop through the guards to order them
        foreach (var guard in NpcsManager.Instance.GetGuards())
        {
            if (Equals(intruder.GetLastKnownLocation(), null)) break;
            guard.SetDestination(intruder.GetLastKnownLocation().Value, true, true);
        }
    }

    // Check if the intercepting guard can switch to chasing; this is not done every frame since it requires path finding and is expensive. 
    public void StopChase()
    {
        if (!(GetState() is Chase)) return;

        // When the interceptor is closer to its target the the tolerance distance it can go straight for the intruder as long as the intruder is being chased
        float toleranceDistance = 1f;

        foreach (var guard in NpcsManager.Instance.GetGuards())
            if (guard.role == GuardRole.Intercept)
            {
                float distanceToGoal = guard.GetRemainingDistanceToGoal();

                if (distanceToGoal < toleranceDistance)
                    guard.role = GuardRole.Chase;
            }
    }

    public void ClearGoals()
    {
        foreach (var guard in NpcsManager.Instance.GetGuards())
            guard.ClearGoal();
    }


    // Get current state
    public State GetState()
    {
        return NpcsManager.Instance.GetState();
    }
}