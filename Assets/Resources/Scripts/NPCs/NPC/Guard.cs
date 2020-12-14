using System;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;
using Unity.MLAgents.Policies;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public abstract class Guard : NPC
{
    [Header("Debug")] [Tooltip("Seen Area")]
    public bool drawSeenArea;

    // Guard's role assigned by the manager
    public GuardRole role = GuardRole.Patrol;

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

        m_FovPolygon = new List<Polygon>() {new Polygon()};
        SeenArea = new List<Polygon>();
    }

    public void RestrictSeenArea(float resetThreshold)
    {
        if (m_guardSeenAreaPercentage > resetThreshold)
            ClearSeenArea();
    }


    public void RestrictSearchArea()
    {
        // m_SpaceFiller.RemoveFromCircle(m_FovPolygon);
        // m_SpaceFiller.RemoveSpottedInterceptionPoints(m_FovPolygon);
    }


    public void ClearSeenArea()
    {
        SeenArea.Clear();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        ClearGoal();
        m_FovPolygon[0].Clear();
        SeenArea.Clear();
    }

    public override void Heuristic(float[] actionsOut)
    {
        // Execute the current state
        //m_state.Update();
    }


    // public override void ExecutePlan(IState state)
    // {
    //
    // }

    public void Patrol()
    {
        if (Goal == null)
            Goal = GetPatrolGoal();

        SetPathToGoal();
    }


    // Check if any intruder is spotted, return true if at least one is spotted
    public bool SpotIntruders(List<Intruder> intruders)
    {
        foreach (var intruder in intruders)
        {
            if (m_FovPolygon[0].IsCircleColliding(intruder.transform.position, 0.03f))
            {
                intruder.Seen();
                RenderIntruder(intruder, true);
                return true;
            }

            RenderIntruder(intruder, false);
        }

        return false;
    }
    
    // Get field of vision
    public Polygon GetFoV()
    {
        return m_FovPolygon[0];
    }

    // Rendering the intuder
    public void RenderIntruder(Intruder intruder, bool seen)
    {
        if (Area.gameView == GameView.Spectator)
        {
            intruder.RenderIntruder(true);
            RenderGuard(true);
        }
        else if (Area.gameView == GameView.Guard)
        {
            RenderGuard(true);
            if (seen)
                intruder.RenderIntruder(true);
            else
                intruder.RenderIntruder(false);
        }
    }

    // Render the guard and the FoV if seen by the intruder
    public void RenderGuard(bool isSeen)
    {
        Renderer.enabled = isSeen;
        FovRenderer.enabled = isSeen;
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

        Fov = fovGameObject.AddComponent<FieldOfView>();
        Fov.Initiate(Properties.ViewAngle, Properties.ViewRadius, new Color32(0, 100, 100, 100));
    }

    private void LoadFovPolygon()
    {
        m_FovPolygon[0].Clear();
        foreach (var vertex in Fov.GetFovVertices())
            m_FovPolygon[0].AddPoint(vertex);
    }

    public void LateUpdate()
    {
        CastVision();
    }

    // Cast the guard field of view
    public void CastVision()
    {
        Fov.CastFieldOfView();
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
                if (m_FovPolygon[i].IsPointInPolygon(point, true))
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
        base.OnDrawGizmos();
        
        if (drawSeenArea)
        {
            foreach (var p in SeenArea)
                p.Draw(p.DetermineWindingOrder().ToString());
        }
    }
}