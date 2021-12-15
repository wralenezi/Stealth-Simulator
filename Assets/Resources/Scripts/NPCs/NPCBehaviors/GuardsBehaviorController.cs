using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardsBehaviorController : MonoBehaviour
{
    private StealthArea m_SA;

    // Guards behavior managers
    private Patroler patroler;

    // Time of the last timestamp a patrol state initiated 
    private float m_lstPatrolTime;

    // Interceptor controller for controlling how guards act when chasing an intruder in sight.
    private Interceptor interceptor;

    private float m_lastAlertTime;

    // Time of the last timestamp a chase state initiated 
    private float m_lstChaseTime;

    // Search controller for controlling how guards searched for an intruder.
    private Searcher searcher;

    // timestamp since the search started
    private float m_searchStamp;

    // Time of the last timestamp a search state initiated 
    private float m_lstSearchTime;

    // Guards planner
    private GuardBehavior m_guardPlanner;

    // Guards state
    private StateMachine m_state;
    public string StateName;

    // Script and dialog manager
    private Scriptor m_Scriptor;

    // Time it takes to make a decision for all guards
    public float Decision;

    // Possible locations to search for the intruder in
    private List<InterceptionPoint> m_possiblePositions;


    public void Initiate(StealthArea stealthArea, Transform map)
    {
        m_SA = stealthArea;

        // Patroler controller
        patroler = gameObject.AddComponent<Patroler>();
        patroler.Initiate(stealthArea);

        // Interception controller
        interceptor = gameObject.AddComponent<Interceptor>();
        interceptor.Initiate(stealthArea);

        // Search Controller
        searcher = gameObject.AddComponent<Searcher>();
        searcher.Initiate(stealthArea);

        // Initiate the FSM to patrol for the guards
        m_state = new StateMachine();
        ChangeState(new Patrol(this, m_SA.intrdrManager.GetController()));
        m_lstPatrolTime = Time.time;
        WorldState.Set("lastPatrolTime", m_lstPatrolTime.ToString());

        // Initiate scriptor
        m_Scriptor = gameObject.AddComponent<Scriptor>();
        m_Scriptor.Initialize(stealthArea.GetSessionInfo().isDialogEnabled);
    }

    public void ResetBehavior()
    {
        // Set the guards to the default mode (patrol)
        ChangeState(new Patrol(this, m_SA.intrdrManager.GetController()));
        m_lstPatrolTime = Time.time;
        WorldState.Set("lastPatrolTime", m_lstPatrolTime.ToString());

        m_Scriptor.Disable();
    }

    public void SetGuardPlanner(GuardBehavior gb)
    {
        m_guardPlanner = gb;
    }


    public void ExecuteState()
    {
        m_state.UpdateState();
    }


    // Start patrol shift
    public void StartShift()
    {
        patroler.FillSegments();
    }

    // Order Guards to patrol
    public void Patrol()
    {
        // This is for the VisMesh
        // foreach (var guard in m_SA.guardsManager.GetGuards())
        //     guard.Patrol();

        // patroler.UpdatePatroler(m_SA.intrdrManager.GetIntruders()[0].GetNpcSpeed(), Time.deltaTime);
        patroler.UpdatePatroler(0.3f, Time.deltaTime);

        foreach (var guard in m_SA.guardsManager.GetGuards())
        {
            if (!guard.IsBusy())
                patroler.GetPatrolPath(guard);
        }
    }


    // In case the intruder is not seen and the guards were on alert, start the search or keep doing it.
    public void StartSearch()
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        // if we were chasing then switch to search
        if (!(m_state.GetState() is Chase)) return;

        // Change the guard state
        ChangeState(new Search(this, m_SA.intrdrManager.GetController()));

        WorldState.Set("lastChaseTimeEnd", m_lstSearchTime.ToString());

        m_lstSearchTime = Time.time;
        WorldState.Set("lastSearchTimeStart", m_lstSearchTime.ToString());
        WorldState.Set("lastSearchTimeEnd", WorldState.EMPTY_VALUE);
        m_searchStamp = StealthArea.GetElapsedTime();

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        // Spawn the coins
        if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection)
            m_SA.coinSpawner.SpawnCoins();

        // Flow the probability for intruder positions
        searcher.PlaceSsForSearch(intrdrs[0].GetLastKnownLocation(),
            intrdrs[0].GetDirection());

        // Assign the guard roles
        // AssignGuardRoles();

        // Order the guards to go the intruder's last known position
        foreach (var guard in m_SA.guardsManager.GetGuards())
            guard.SetGoal(intrdrs[0].GetLastKnownLocation(), true);
    }


    // Keep searching for the intruder
    public void Search()
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        searcher.UpdateSearcher(intrdrs[0].GetNpcSpeed(), m_SA.guardsManager.GetGuards(), Time.deltaTime);

        // Benchmark purposes 
        float timeBefore = Time.realtimeSinceStartup;

        foreach (var guard in m_SA.guardsManager.GetGuards())
        {
            // In case of cheating behavior, Don't wait till the guard is free and just guide them to intruder's actual position.
            if (m_guardPlanner.search == GuardSearchPlanner.Cheating)
            {
                guard.SetGoal(intrdrs[0].GetTransform().position, true);
                continue;
            }


            // Two method for choosing the guard plans

            // Choose the segment (First method)
            // SearchIndividualSegments(guard);

            // Define a path the guard should follow (Second method)
            SearchPath(guard);
        }

        // Logging purposes
        if (Time.realtimeSinceStartup - timeBefore > Decision)
            Decision = (Time.realtimeSinceStartup - timeBefore);
    }


    // Individual segment search
    public void SearchIndividualSegments(Guard guard)
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        // Once the chaser is idle that means that the intruder is still not seen
        // Now Guards should start visiting the nodes with distance more than zero
        if (!guard.IsBusy())
        {
            // Search behavior based on the planner type 
            if (m_guardPlanner.search == GuardSearchPlanner.RmPropSimple ||
                m_guardPlanner.search == GuardSearchPlanner.RmPropOccupancyDiffusal)
            {
                // Get a new goal and swap it with the closest guard to that goal and take their goals instead.
                Vector2? newGoal = searcher.GetSearchSegment(guard, m_SA.guardsManager.GetGuards(), intrdrs[0],
                    m_SA.worldRep.GetNavMesh(), m_SA.guardsManager.searchWeights); 
                if(!Equals(newGoal, null)) SwapGoal(guard, newGoal.Value , false);
            }
            else if (m_guardPlanner.search == GuardSearchPlanner.Random)
            {
                Vector2 randomRoadmap = m_SA.mapDecomposer.GetRandomPolygonInNavMesh().GetRandomPosition();
                guard.SetGoal(randomRoadmap, false);
            }
        }
    }

    public void SearchPath(Guard guard)
    {
        if (!guard.IsBusy())
        {
            if (m_guardPlanner.search == GuardSearchPlanner.RmPropSimple ||
                m_guardPlanner.search == GuardSearchPlanner.RmPropOccupancyDiffusal)
            {
                float timeDiff = StealthArea.GetElapsedTime() - m_searchStamp;
                float distancePortion = timeDiff / 15f;

                float length = Properties.MaxPathDistance * distancePortion;


                searcher.GetPath(guard);

                // searcher.FindLineAndPath(guard);
            }
        }
    }

    // Assign goal to closest guard and swap goals if needed if the guard was busy.
    public void SwapGoal(Guard assignedGuard, Vector2 newGoal, bool isEnabled)
    {
        // Find the closest guard to the new goal
        float minDistance = Vector2.Distance(assignedGuard.transform.position, newGoal);
        Guard closestGuard = null;
        foreach (var curGuard in m_SA.guardsManager.GetGuards())
        {
            // float dstToOldGuard = Vector2.Distance(curGuard.transform.position, newGoal);
            float dstToOldGuard = PathFinding.GetShortestPathDistance(
                GameManager.Instance.GetActiveArea().mapDecomposer.GetNavMesh(), curGuard.transform.position, newGoal);

            // Check if the other guard is closer
            if (minDistance > dstToOldGuard)
            {
                minDistance = dstToOldGuard;
                closestGuard = curGuard;
            }
        }

        string heading = "";

        // Probability of the npc saying a line
        float prob = 0.8f;
        // Sort out the guard assignment
        if (isEnabled && !Equals(closestGuard, assignedGuard) && !Equals(closestGuard, null))
        {
            // Swap the goals between the closer guard
            if (closestGuard.IsBusy())
            {
                Vector2 tempGoal = closestGuard.GetGoal().Value;
                assignedGuard.SetGoal(tempGoal, true);

                // // Update the guards heading
                // heading = WorldState.GetHeading(assignedGuard.GetTransform().position, tempGoal);
                // WorldState.Set(assignedGuard.name + "_goal", heading);

                m_SA.guardsManager.UpdateWldStNpcs();

                // guard announce to go instead 
                m_Scriptor.ChooseDialog(assignedGuard, closestGuard, "TakeOver_Plan", prob);
            }

            // Assign the new goal to the other idle guard
            closestGuard.SetGoal(newGoal, true);

            // // Update the guards heading
            // heading = WorldState.GetHeading(closestGuard.GetTransform().position, newGoal);
            // WorldState.Set(assignedGuard.name + "_goal", heading);

            m_SA.guardsManager.UpdateWldStNpcs();

            m_Scriptor.ChooseDialog(closestGuard, null, "Plan", prob);
        }
        else // since no guards are closer then simply assign it to the one who chose it
        {
            assignedGuard.SetGoal(newGoal, false);

            // // Update the guards heading
            // heading = WorldState.GetHeading(assignedGuard.GetTransform().position, newGoal);
            // WorldState.Set(assignedGuard.name + "_goal", heading);

            m_SA.guardsManager.UpdateWldStNpcs();

            m_Scriptor.ChooseDialog(assignedGuard, null, "Plan", prob);
        }
    }

    public void EndSearch()
    {
        searcher.Clear();
    }

    /// <summary>
    /// Update the guard behavior
    /// </summary>
    /// <param name="timeDelta"></param>
    public void UpdateBehaviorController(float timeDelta)
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        // Update the search area in case the guards are searching for an intruder
        // Move and propagate the possible intruder position (phantoms)
        if (GetState() is Search)
        {
            searcher.UpdateSearcher(intrdrs[0].GetNpcSpeed(), m_SA.guardsManager.GetGuards(), timeDelta);
        }
    }


    // In case of intruder is seen
    public void StartChase(NPC spotter)
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        // Switch to chase state
        if (m_state.GetState() is Chase) return;

        if (m_state.GetState() is Search)
            WorldState.Set("lastSearchTimeEnd", Time.time.ToString());
        else if (m_state.GetState() is Patrol)
            WorldState.Set("lastPatrolTimeEnd", Time.time.ToString());

        ChangeState(new Chase(this, m_SA.intrdrManager.GetController()));

        m_Scriptor.ChooseDialog(spotter, null, "Spot", 1f);

        m_lstChaseTime = Time.time;
        WorldState.Set("lastChaseTimeStart", m_lstChaseTime.ToString());
        WorldState.Set("lastChaseTimeEnd", WorldState.EMPTY_VALUE);

        interceptor.Clear();

        if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection)
            m_SA.coinSpawner.DisableCoins();

        // m_StealthArea.AreaUiManager.UpdateGuardLabel(GetState());

        foreach (var intruder in intrdrs)
        {
            // intruder.StartRunningAway();
        }
    }

    // Order guards to chase
    public void Chase()
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        // The score reduction rate while being chased
        float decreaseRate = 5f;

        m_lastAlertTime = StealthArea.GetElapsedTime();

        // Loop through the guards to order them
        foreach (var guard in m_SA.guardsManager.GetGuards())
        {
            // Decide the guard behavior in chasing based on its parameter
            if (m_guardPlanner.chase == GuardChasePlanner.Intercepting)
            {
                if (guard.role == GuardRole.Chase || !guard.IsBusy())
                    guard.SetGoal(intrdrs[0].GetLastKnownLocation(), true);
            }
            else if (m_guardPlanner.chase == GuardChasePlanner.Simple)
                guard.SetGoal(intrdrs[0].GetLastKnownLocation(), true);


            // Update the score based on the game type if the intruder is seen
            if (intrdrs.Count > 0)
            {
                if (m_SA.GetSessionInfo().gameType == GameType.Stealth)
                {
                    float percentage = Properties.EpisodeLength - intrdrs[0].GetAlertTime();
                    percentage = Mathf.Round((percentage * 1000f) / Properties.EpisodeLength) / 10f;
                    GameManager.Instance.GetActiveArea().AreaUiManager.UpdateScore(percentage);
                }
                else if (m_SA.GetSessionInfo().gameType == GameType.CoinCollection)
                {
                    m_SA.guardsManager.IncrementScore(-decreaseRate * Time.deltaTime);
                }
            }
        }
    }

    // Assign the interception points to the guards
    public void AssignGuardToInterceptionPoint()
    {
        List<Intruder> intrdrs = m_SA.intrdrManager.GetIntruders();

        // Calculate the distance to each future possible position of the intruder and choose the closest to the guard's current position to intercept. 
        m_possiblePositions = interceptor.GetPossiblePositions();

        foreach (var guard in m_SA.guardsManager.GetGuards())
        {
            // 
            if (guard.role == GuardRole.Chase && guard.IsBusy())
                continue;


            float highestScore = Mathf.NegativeInfinity;
            InterceptionPoint designatedInterceptionPoint = null;


            foreach (var node in m_possiblePositions)
            {
                // Avoid going to the same goal of another guard if there are more interception points available.
                if (m_possiblePositions.Count > 1)
                {
                    bool isTargeted = false;
                    foreach (var guard1 in m_SA.guardsManager.GetGuards())
                        if (guard1 != guard && guard1.IsBusy() && guard1.GetGoal() == node.position)
                        {
                            isTargeted = true;
                            break;
                        }

                    if (isTargeted)
                        continue;
                }

                // The distance from the guard
                float distanceToNode = PathFinding.GetShortestPathDistance(m_SA.worldRep.GetNavMesh(),
                    guard.transform.position, node.position);

                // Distance from the intruder's last seen position
                float distanceFromIntruder = PathFinding.GetShortestPathDistance(m_SA.worldRep.GetNavMesh(),
                    intrdrs[0].GetLastKnownLocation(), node.position);

                float score = (1f / (node.generationIndex + 1f)) * 0.6f +
                              (1f / (node.distanceToEndNode + 1f)) * 0.1f +
                              (1f / (distanceToNode + 1f)) * 0.3f +
                              (1f / (distanceFromIntruder + 1f)) * 0.001f;

                if (score > highestScore)
                {
                    highestScore = score;
                    designatedInterceptionPoint = node;
                }
            }

            if (designatedInterceptionPoint != null)
            {
                guard.SetGoal(designatedInterceptionPoint.position, true);
            }
        }
    }

    // Check if the intercepting guard can switch to chasing; this is not done every frame since it requires path finding and is expensive. 
    public void StopChase()
    {
        if (!(m_state.GetState() is Chase)) return;

        // When the interceptor is closer to its target the the tolerance distance it can go straight for the intruder as long as the intruder is being chased
        float toleranceDistance = 1f;

        foreach (var guard in m_SA.guardsManager.GetGuards())
            if (guard.role == GuardRole.Intercept)
            {
                float distanceToGoal = guard.GetRemainingDistanceToGoal();

                if (distanceToGoal < toleranceDistance)
                    guard.role = GuardRole.Chase;
            }
    }

    public void ClearGoals()
    {
        foreach (var guard in m_SA.guardsManager.GetGuards())
            guard.ClearGoal();
    }


    // private void Update()
    // {
    //     StateName = m_state.GetState().ToString();
    // }

    /// <summary>
    /// Change the current guard state to a new one.
    /// </summary>
    /// <param name="state"> The new state </param>
    private void ChangeState(IState state)
    {
        m_state.ChangeState(state);
        WorldState.Set("guard_state", GetState().ToString());
    }

    // Get current state
    public IState GetState()
    {
        return m_state.GetState();
    }
}