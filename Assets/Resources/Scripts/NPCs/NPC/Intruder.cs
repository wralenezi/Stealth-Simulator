using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Intruder : NPC
{
    // Hiding spots the intruder can go to
    private HidingSpots m_HidingSpots;

    // Intruder state 
    private StateMachine m_state;

    // The place the intruder was last seen in 
    private Vector2? m_lastKnownLocation;

    // Count of how many time this intruder has been spotted by guards
    private int m_NoTimesSpotted;

    // Total time being chased and visible
    private float m_AlertTime;

    // Total time being chased and invisible 
    private float m_SearchedTime;

    // List of guards; to assess the fitness of the hiding spots
    private List<Guard> m_guards;

    public override void Initiate(StealthArea area, NpcData data)
    {
        base.Initiate(area, data);

        m_HidingSpots = transform.parent.parent.Find("Map").GetComponent<HidingSpots>();
        m_guards = transform.parent.parent.Find("NpcManager").GetComponent<GuardsManager>().GetGuards();

        // Start the state as incognito
        m_state = new StateMachine();
        m_state.ChangeState(new Incognito(this));

        // Multiply the intruder's speed
        NpcSpeed *= Properties.IntruderSpeedPercentage / 100f;
        NpcRotationSpeed *= Properties.IntruderRotationSpeedMulti;
    }

    public override void ResetNpc()
    {
        base.ResetNpc();

        m_NoTimesSpotted = 0;
        m_AlertTime = 0f;
        m_SearchedTime = 0f;
    }

    // Run the state the intruder is in
    public void ExecuteState()
    {
        if (GetNpcData().intruderPlanner != IntruderPlanner.UserInput)
        {
            m_state.UpdateState();
        }
    }

    public override void UpdateMetrics(float timeDelta)
    {
        base.UpdateMetrics(timeDelta);
        if (m_state.GetState() is Chased)
        {
            m_AlertTime += timeDelta;
        }
        else if (m_state.GetState() is Hide)
        {
            m_SearchedTime += timeDelta;
        }
    }

    public Vector2 GetLastKnownLocation()
    {
        return m_lastKnownLocation.Value;
    }

    // Render the guard and the FoV if seen by the intruder
    public void RenderIntruder(bool isSeen)
    {
        Renderer.enabled = isSeen;
    }

    public void RenderIntruderFov(bool isSeen)
    {
        FovRenderer.enabled = isSeen;
    }

    // Intruder is seen so update the known location of the intruder 
    public void Seen()
    {
        m_lastKnownLocation = transform.position;
    }

    // Rendering 
    public void SpotGuards(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (Area.gameView == GameView.Spectator)
            {
                guard.RenderGuard(true);
                RenderIntruder(true);
            }
            else if (Area.gameView == GameView.Intruder)
            {
                RenderIntruder(true);

                if (GetFovPolygon().IsCircleInPolygon(guard.transform.position, 0.5f))
                    guard.RenderGuard(true);
                else
                    guard.RenderGuard(false);
            }
        }
    }

    // Incognito behavior
    public void Incognito()
    {
        if (GetNpcData().intruderPlanner == IntruderPlanner.Random)
            SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        else
        {
            m_HidingSpots.AssignHidingSpotsFitness(m_guards, World.GetNavMesh());
            SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        }
    }

    // Called when the intruder is spotted
    public void StartRunningAway()
    {
        m_state.ChangeState(new Chased(this));
        m_NoTimesSpotted++;
    }

    // Intruder behavior when being chased
    public void RunAway()
    {
        if (GetNpcData().intruderPlanner == IntruderPlanner.Random)
            SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        else //if (GetNpcData().intruderPlanner == IntruderPlanner.Heuristic)
        if (!IsBusy())
        {
            m_HidingSpots.AssignHidingSpotsFitness(m_guards, World.GetNavMesh());
            SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        }
    }

    // To start hiding from guards searching for the intruder
    public void StartHiding()
    {
        m_state.ChangeState(new Hide(this));

        // Find a place to hide
        if (GetNpcData().intruderPlanner == IntruderPlanner.Random ||
            GetNpcData().intruderPlanner == IntruderPlanner.RandomMoving)
            SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        else if (GetNpcData().intruderPlanner == IntruderPlanner.Heuristic ||
                 GetNpcData().intruderPlanner == IntruderPlanner.HeuristicMoving)
            if (!IsBusy())
            {
                m_HidingSpots.AssignHidingSpotsFitness(m_guards, World.GetNavMesh());
                SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
            }
    }

    private bool isWaiting = false;

    // Intruder behavior after escaping guards
    public void Hide()
    {
        if (!IsBusy() && !isWaiting)
        {
            if (GetNpcData().intruderPlanner == IntruderPlanner.HeuristicMoving)
                StartCoroutine(waitThenHeuristicMove());
            else if (GetNpcData().intruderPlanner == IntruderPlanner.RandomMoving)
                StartCoroutine(waitThenRandomMove());
        }
    }

    private IEnumerator waitThenHeuristicMove()
    {
        isWaiting = true;
        float waitTime = Random.Range(5f, 20f);

        yield return new WaitForSeconds(waitTime);

        if (!IsBusy())
        {
            m_HidingSpots.AssignHidingSpotsFitness(m_guards, World.GetNavMesh());
            SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        }

        isWaiting = false;
    }


    private IEnumerator waitThenRandomMove()
    {
        isWaiting = true;

        float waitTime = Random.Range(5f, 20f);
        yield return new WaitForSeconds(waitTime);

        if (!IsBusy())
        {
            SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        }

        isWaiting = false;
    }

    public float GetPercentAlertTime()
    {
        return m_AlertTime / Properties.EpisodeLength;
    }

    public int GetNumberOfTimesSpotted()
    {
        return m_NoTimesSpotted;
    }

    public IState GetState()
    {
        return m_state.GetState();
    }


    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), StealthArea.episodeTime, Data, m_state.GetState().ToString(),
            m_NoTimesSpotted, GuardsManager.GuardsOverlapTime,
            m_AlertTime, m_SearchedTime, 0, 0f);
    }
}