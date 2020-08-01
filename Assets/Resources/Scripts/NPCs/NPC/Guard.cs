using System;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class Guard : NPC
{
    [Header("Debug")] [Tooltip("Seen Area")]
    public bool drawSeenArea;

    private FieldOfView m_Fov;

    // To model the search planner
    private SpaceFiller m_SpaceFiller;

    // npc state
    private StateMachine m_state;

    //************ Guard's Vision *****************//

    // The seen area of the guard
    protected List<Polygon> SeenArea;

    // The Current FoV
    private List<Polygon> m_FovPolygon;

    // the percentage of the seen area by this guard
    protected int m_guardSeenAreaPercentage;
    
    protected int m_foundHidingSpots;


    public override void Initialize()
    {
        base.Initialize();
        AddFoV();

        m_SpaceFiller = transform.parent.parent.Find("Map").GetComponent<SpaceFiller>();

        m_FovPolygon = new List<Polygon>() {new Polygon()};
        SeenArea = new List<Polygon>();

        m_state = new StateMachine();
        m_state.ChangeState(new Patrol(this));
    }

    public void RestrictSeenArea(float resetThreshold)
    {
        if (m_guardSeenAreaPercentage > resetThreshold)
            ClearSeenArea();
    }


    public void RestrictSearchArea()
    {
        m_SpaceFiller.RemoveFromCircle(m_FovPolygon);
        m_SpaceFiller.RemoveSpottedInterceptionPoints(m_FovPolygon);
    }


    public void ClearSeenArea()
    {
        SeenArea.Clear();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        Goal = null;
        m_FovPolygon[0].Clear();
        SeenArea.Clear();
    }

    public override void Heuristic(float[] actionsOut)
    {
        // Execute the current state
        m_state.Update();
    }

    public IState GetState()
    {
        return m_state.GetState();
    }

    public override void ExecutePlan()
    {
        if (PathToTake.Count > 0)
            if (GoStraightTo(PathToTake[0], m_state.GetState()))
            {
                PathToTake.RemoveAt(0);

                if (PathToTake.Count == 0)
                    Goal = null;
            }
    }


    // Clear the designated goal and path to take
    public void ClearGoal()
    {
        Goal = null;
        PathToTake.Clear();
    }

    public void Patrol()
    {
        Goal = GetPatrolGoal();

        if (Goal != null)
        {
            PathToTake = PathFinding.GetShortestPath(World.GetNavMesh(),
                transform.position, Goal.Value);

            // EditorApplication.isPaused = true;
        }
    }

    // In case of an intruder is located order the guard to chase to that assigned @param: target
    public void UpdateChasingTarget(Vector2 target)
    {
        if (m_state.GetState() is Chase)
        {
            Goal = target;
            RequestDecision();
        }
        else
            // Switch to chase state
            m_state.ChangeState(new Chase(this));
    }


    public void Chase()
    {
        if (Goal != null)
        {
            PathToTake = PathFinding.GetShortestPath(World.GetNavMesh(),
                transform.position, Goal.Value);

            // EditorApplication.isPaused = true;
        }
    }

    // Start searching for the intruder if the guard was chasing
    public void StartSearch()
    {
        // if we were chasing then switch to search
        if (m_state.GetState() is Chase)
        {
            // Goal = intruder.transform.position;
            m_state.ChangeState(new Search(this));
            RequestDecision();
        }
    }


    public void Search()
    {
        Goal = m_SpaceFiller.GetGoal(transform.position);

        if (Goal != null)
        {
            PathToTake = PathFinding.GetShortestPath(World.GetNavMesh(),
                transform.position, Goal.Value);

            // EditorApplication.isPaused = true;
        }
    }

    public void EndSearch()
    {
        m_SpaceFiller.Clear();
    }


    // Check if any intruder is spotted, return true if at least one is spotted
    public bool SpotIntruders(List<Intruder> intruders)
    {
        foreach (var intruder in intruders)
        {
            if (m_FovPolygon[0].IsPointInPolygon(intruder.transform.position, true))

                return true;
        }

        return false;
    }


    public abstract Vector2? GetPatrolGoal();


    // Create and add the Field of View
    public void AddFoV()
    {
        // The game object that contains the field of view
        GameObject fovGameObject = new GameObject("FoV");

        // Assign it as a child to the guard
        var transform1 = transform;
        fovGameObject.transform.parent = transform1;
        fovGameObject.transform.position = transform1.position;

        m_Fov = fovGameObject.AddComponent<FieldOfView>();
        m_Fov.Initiate(Properties.ViewAngle, Properties.ViewRadius, new Color32(0, 100, 100, 100));
    }

    private void LoadFovPolygon()
    {
        m_FovPolygon[0].Clear();
        foreach (var vertex in m_Fov.GetFovVertices())
            m_FovPolygon[0].AddPoint(vertex);
    }

    // Cast the guard field of view
    public void CastVision()
    {
        m_Fov.CastFieldOfView();
        LoadFovPolygon();
    }

    // Add the FoV to the Overall Seen Area
    public void AccumulateSeenArea()
    {
        // If there is no area seen start with the guards current vision
        if (SeenArea.Count == 0)
        {
            SeenArea.Add(m_FovPolygon[0]);
        }
        else
        {
            // Merge with the total seen area by this guard
            SeenArea = PolygonHelper.MergePolygons(m_FovPolygon, SeenArea, ClipType.ctUnion);
        }

        CheckForFoundHidingSpots();
    }

    public List<Polygon> CopySeenArea()
    {
        List<Polygon> seenArea = new List<Polygon>();

        foreach (Polygon poly in GetSeenArea())
        {
            Polygon p = new Polygon(poly);
            seenArea.Add(p);
        }

        return seenArea;
    }

    void CheckForFoundHidingSpots()
    {
        List<Vector2> hidingSpots = World.GetHidingSpots();

        int index = 0;
        while (index < hidingSpots.Count)
        {
            if (IsNodeInFoV(hidingSpots[index]))
            {
                hidingSpots.RemoveAt(index);
                m_foundHidingSpots++;
            }
            else
            {
                index++;
            }
        }
    }
    
    public bool IsNodeInFoV(Vector2 point)
    {
        bool isIn = false;

        if (m_FovPolygon.Count > 0)
            for (int i = 0; i < m_FovPolygon.Count; i++)
                if (m_FovPolygon[i].IsPointInPolygon(point,true))
                {
                    isIn = true;
                }


        return isIn;
    }

    
    public virtual void SetSeenPortion()
    {
    }

    public List<Polygon> GetSeenArea()
    {
        return SeenArea;
    }

    private void OnDrawGizmos()
    {
        if (drawSeenArea)
        {
            foreach (var p in SeenArea)
                p.Draw(p.DetermineWindingOrder().ToString());
        }

        if (PathToTake.Count > 0)
            for (int i = 0; i < PathToTake.Count; i++)
            {
                if (i < PathToTake.Count - 1)
                    Gizmos.DrawLine(PathToTake[i], PathToTake[i + 1]);
                Gizmos.DrawSphere(PathToTake[i], 0.05f);
            }
    }
}