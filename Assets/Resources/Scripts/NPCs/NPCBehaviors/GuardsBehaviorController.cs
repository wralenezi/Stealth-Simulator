using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardsBehaviorController : MonoBehaviour
{
    private Behavior m_behavior;

    public Behavior behavior => m_behavior;

    // Guards behavior managers
    private Patroler patroler;
    
    // Search controller for controlling how guards searched for an intruder.
    private Searcher searcher;

    // Interceptor controller for controlling how guards act when chasing an intruder in sight.
    private Interceptor interceptor;
    
    // Time it takes to make a decision for all guards
    public float Decision;

    // Possible locations to search for the intruder in
    private List<InterceptionPoint> m_possiblePositions;

    public void Initiate(Session session, MapManager mapManager)
    {
        m_behavior = session.GetGuardsData()[0].behavior;

        // Patroler controller
        patroler = gameObject.AddComponent<Patroler>();
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
    
    // Start patrol shift
    public void StartShift()
    {
        patroler.FillSegments();
    }

    // Order Guards to patrol
    public void Patrol()
    {
        patroler.UpdatePatroler(NpcsManager.Instance.GetGuards(), 0.3f, Time.deltaTime);
    }


    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch(Intruder intruder)
    {
        // if we were chasing then switch to search
        // if (!(NpcsManager.Instance.GetState() is Chase)) return;

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        // Spawn the coins
        // if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection) m_SA.coinSpawner.SpawnCoins();

        // Flow the probability for intruder positions
        searcher.StartSearch(intruder);
        
        // Order the guards to go the intruder's last known position
        foreach (var guard in NpcsManager.Instance.GetGuards())
            guard.SetDestination(intruder.GetLastKnownLocation(), true, true);
    }


    // Keep searching for the intruder
    public void Search(Intruder intruder)
    {
        searcher.UpdateSearcher(intruder.GetNpcSpeed(), NpcsManager.Instance.GetGuards(), Time.deltaTime);

        // Benchmark purposes 
        float timeBefore = Time.realtimeSinceStartup;

        foreach (var guard in NpcsManager.Instance.GetGuards())
        {
            searcher.Search(guard);
        }

        // Logging purposes
        if (Time.realtimeSinceStartup - timeBefore > Decision)
            Decision = (Time.realtimeSinceStartup - timeBefore);
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
        
        // ChangeState(new Chase(this, m_SA.intrdrManager.GetController()));

        // The region the intruder was last seen in 
        WorldState.Set("intruder_last_seen_region",
            RegionLabelsManager.GetRegion(intruder.GetTransform().position));

        interceptor.Clear();

        // if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection) m_SA.coinSpawner.DisableCoins();
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

        // // Update the score based on the game type if the intruder is seen
        // if (intrdrs.Count > 0)
        // {
        //     if (m_SA.GetSessionInfo().gameType == GameType.Stealth)
        //     {
        //         float percentage = Properties.EpisodeLength - intruder.GetAlertTime();
        //         percentage = Mathf.Round((percentage * 1000f) / Properties.EpisodeLength) / 10f;
        //         GameManager.Instance.GetActiveArea().AreaUiManager.UpdateScore(percentage);
        //     }
        //     else if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection)
        //     {
        //         m_SA.guardsManager.IncrementScore(-decreaseRate * Time.deltaTime);
        //     }
        // }
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