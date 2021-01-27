using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Intruder : NPC
{
    // Hiding spots the intruder can go to
    private HidingSpots m_HidingSpots;

    // Intruder state 
    private StateMachine m_state;

    // The place the intruder was last seen in 
    private Vector2? m_lastKnownLocation;


    // The Current FoV
    private List<Polygon> m_FovPolygon;

    // Total time being chased and visible
    private float m_AlertTime;

    // Total time being chased and invisible 
    private float m_SearchedTime;

    // List of guards; to assess the fitness of the hiding spots
    private List<Guard> m_guards;


    public override void Initialize()
    {
        base.Initialize();

        m_HidingSpots = transform.parent.parent.Find("Map").GetComponent<HidingSpots>();
        m_guards = transform.parent.parent.Find("NpcManager").GetComponent<GuardsManager>().GetGuards();
        AddFoV();

        // The intruder's field of view to detect guards
        m_FovPolygon = new List<Polygon>() {new Polygon()};

        // Start the state as incognito
        m_state = new StateMachine();
        m_state.ChangeState(new Incognito(this));

        // Set the intruder's speed
        NpcSpeed *= 1.5f;
        NpcRotationSpeed *= 2f;
    }


    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        m_AlertTime = 0f;
        m_SearchedTime = 0f;
    }

    public override void Heuristic(float[] actionsOut)
    {
        // MoveByInput();
    }

    // Run the state the intruder is in
    public void ExecuteState()
    {
        if (GetNpcData().intruderPlanner == IntruderPlanner.UserInput)
            MoveByInput();
        else
        {
            m_state.Update();
        }
    }

    public override void UpdateMetrics()
    {
        base.UpdateMetrics();
        if (m_state.GetState() is Chased)
        {
            m_AlertTime += Time.fixedDeltaTime;
        }
        else if (m_state.GetState() is Hide)
        {
            m_SearchedTime += Time.fixedDeltaTime;
        }
    }

    private void LateUpdate()
    {
        CastVision();
    }

    // Cast the guard field of view
    public void CastVision()
    {
        Fov.CastFieldOfView();
        LoadFovPolygon();
    }

    private void LoadFovPolygon()
    {
        m_FovPolygon[0].Clear();
        foreach (var vertex in Fov.GetFovVertices())
            m_FovPolygon[0].AddPoint(vertex);
    }


    public Vector2 GetLastKnownLocation()
    {
        return m_lastKnownLocation.Value;
    }

    // Create and add the Field of View
    public void AddFoV()
    {
        // The game object that contains the field of view
        GameObject fovGameObject = new GameObject("FoV");

        // Assign it as a child to the guard
        var transform1 = transform;
        fovGameObject.transform.parent = transform1;
        fovGameObject.transform.position = transform1.position;

        Fov = fovGameObject.AddComponent<FieldOfView>();
        Fov.Initiate(361f, 50f, new Color32(200, 200, 200, 150));
    }


    // Render the guard and the FoV if seen by the intruder
    public void RenderIntruder(bool isSeen)
    {
        Renderer.enabled = isSeen;
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

                if (m_FovPolygon[0].IsCircleColliding(guard.transform.position, 0.5f))
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
        else if (GetNpcData().intruderPlanner == IntruderPlanner.Heuristic)
        {
            m_HidingSpots.AssignHidingSpotsFitness(m_guards, World.GetNavMesh());
            SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
        }
    }

    public void StartRunningAway()
    {
        m_state.ChangeState(new Chased(this));
    }

    // Intruder behavior when being chased
    public void RunAway()
    {
        if (GetNpcData().intruderPlanner == IntruderPlanner.Random)
            SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        else if (GetNpcData().intruderPlanner == IntruderPlanner.Heuristic)
            if (IsIdle())
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
        if (GetNpcData().intruderPlanner == IntruderPlanner.Random)
            SetGoal(m_HidingSpots.GetRandomHidingSpot(), false);
        else if (GetNpcData().intruderPlanner == IntruderPlanner.Heuristic)
            if (IsIdle())
            {
                m_HidingSpots.AssignHidingSpotsFitness(m_guards, World.GetNavMesh());
                SetGoal(m_HidingSpots.GetBestHidingSpot().Value, false);
            }
    }

    public IState GetState()
    {
        return m_state.GetState();
    }


    // Intruder behavior after escaping guards
    public void Hide()
    {
    }

    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), StealthArea.episodeTime, Data, m_state.GetState().ToString(),
            m_AlertTime, m_SearchedTime, 0, 0f);
    }
}