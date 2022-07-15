using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardsBehaviorController : MonoBehaviour
{
    List<BehaviorPerformanceSnapshot> _decisionTimes;

    private Behavior m_behavior;

    public Behavior behavior => m_behavior;

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

        m_behavior = session.GetGuardsData()[0].behavior;

        // Patroler controller
        switch (behavior.patrol)
        {
            case PatrolPlanner.gRoadMap:
                patroler = gameObject.AddComponent<RoadMapPatroler>();
                break;

            case PatrolPlanner.gVisMesh:
                patroler = gameObject.AddComponent<VisMeshPatroler>();
                break;

            case PatrolPlanner.gRandom:
                patroler = gameObject.AddComponent<RandomPatroler>();
                break;

            case PatrolPlanner.gScripted:
                patroler = gameObject.AddComponent<ScriptedPatroler>();
                break;
        }

        patroler.Initiate(mapManager);


        // Interception controller
        interceptor = gameObject.AddComponent<Interceptor>();
        interceptor.Initiate(mapManager);


        // Search Controller
        switch (behavior.search)
        {
            case SearchPlanner.RmPropSimple:
                searcher = gameObject.AddComponent<SimpleRmPropSearcher>();
                break;

            case SearchPlanner.RmPropOccupancyDiffusal:
                searcher = gameObject.AddComponent<OccupancyRmSearcher>();
                break;

            case SearchPlanner.Cheating:
                searcher = gameObject.AddComponent<CheatingSearcher>();
                break;

            case SearchPlanner.Random:
                searcher = gameObject.AddComponent<RandomSearcher>();
                break;
        }

        searcher.Initiate(session, mapManager);
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


    // Start patrol shift
    public void StartShift()
    {
        patroler.Start();
    }

    // Order Guards to patrol
    public void Patrol()
    {
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
        searcher.StartSearch(intruder);

        // Order the guards to go the intruder's last known position
        foreach (var guard in NpcsManager.Instance.GetGuards())
            guard.SetDestination(intruder.GetLastKnownLocation(), true, true);
    }


    // Keep searching for the intruder
    public void Search(Intruder intruder)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        searcher.UpdateSearcher(intruder.GetNpcSpeed(), NpcsManager.Instance.GetGuards(), Time.deltaTime);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(patroler.GetType().Name + " Update", elapsedMs));



        watch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var guard in NpcsManager.Instance.GetGuards())
            searcher.Search(guard);
        watch.Stop();
        elapsedMs = watch.ElapsedMilliseconds;
        _decisionTimes.Add(new BehaviorPerformanceSnapshot(searcher.GetType().Name + " Decision", elapsedMs));
    }

    public void ClearSearch()
    {
        searcher.Clear();
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
            searcher.UpdateSearcher(intruder.GetNpcSpeed(), NpcsManager.Instance.GetGuards(), timeDelta);
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
            // Decide the guard behavior in chasing based on its parameter
            if (behavior.alert == AlertPlanner.Intercepting)
            {
                if (guard.role == GuardRole.Chase || !guard.IsBusy())
                    guard.SetDestination(intruder.GetLastKnownLocation(), true, true);
            }
            else if (guard.GetNpcData().behavior.alert == AlertPlanner.Simple)
                guard.SetDestination(intruder.GetLastKnownLocation(), true, true);
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