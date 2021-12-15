using System.Collections.Generic;
using ClipperLib;
using TMPro;
using UnityEngine;


public abstract class Guard : NPC
{
    private GameObject m_excMarkPrefab;
    private GameObject m_excMarkGo;

    [Header("Debug")] [Tooltip("Seen Area")]
    public bool drawSeenArea;

    // Guard's role assigned by the manager; default is in patrol
    public GuardRole role = GuardRole.Patrol;

    //************ Guard's Vision *****************//

    // The seen area of the guard
    protected List<Polygon> SeenArea;

    // the percentage of the seen area by this guard
    protected int m_GuardSeenAreaPercentage;

    // Number of pellets found
    protected int m_FoundHidingSpots;

    // Initialize the guard
    public override void Initiate(StealthArea area, NpcData data)
    {
        base.Initiate(area, data);
        SeenArea = new List<Polygon>();
        m_excMarkPrefab = (GameObject) Resources.Load("Prefabs/exclamation_mark");
        m_excMarkGo = Instantiate(m_excMarkPrefab, GameManager.Instance.GetActiveArea().transform);
        m_excMarkGo.SetActive(false);

        // Add guard label
        GameObject labelOg = new GameObject();

        labelOg.name = "Label";
        labelOg.transform.parent = transform;
        labelOg.AddComponent<SeparateRotator>();
        TextMeshPro textMeshPro = labelOg.AddComponent<TextMeshPro>();
        textMeshPro.text = gameObject.name.Replace("Guard0", "");
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.fontSize = 4;
        textMeshPro.sortingOrder = 5;
        textMeshPro.color = Color.black;
        
    }

    public override void ResetNpc()
    {
        base.ResetNpc();
        ClearGoal();
        SeenArea.Clear();
    }

    public abstract float GetPassingsAverage();

    // resets the guards covered area
    public void RestrictSeenArea(float resetThreshold)
    {
        if (m_GuardSeenAreaPercentage > resetThreshold)
            ClearSeenArea();
    }

    public void ClearSeenArea()
    {
        SeenArea.Clear();
    }

    // Get the guard to patrol 
    public void Patrol()
    {
        if (!IsBusy())
            SetGoal(GetPatrolGoal().Value, false);
    }

    // Check if any intruder is spotted, return true if at least one is spotted
    public bool SpotIntruders(List<Intruder> intruders)
    {
        foreach (var intruder in intruders)
        {
            // Check if the intruder is seen
            if (GetFovPolygon().IsCircleInPolygon(intruder.transform.position, 0.03f))
            {
                intruder.Seen();
                WorldState.Set(name + "_see_" + intruder.name, true.ToString());
                WorldState.Set("last_time_"+name+"_saw_"+intruder.name, Time.time.ToString());
                RenderIntruder(intruder, true);
                ShowExclamation();
                return true;
            }

            // Intruder not seen
            WorldState.Set(name + "_see_" + intruder.name, false.ToString());
            RenderIntruder(intruder, false);
        }


        m_excMarkGo.SetActive(false);
        return false;
    }


    private void ShowExclamation()
    {
        m_excMarkGo.SetActive(true);
        m_excMarkGo.transform.position = (Vector2) GetTransform().position + Vector2.up * 0.7f;
    }


    // Rendering the intruder
    public void RenderIntruder(Intruder intruder, bool seen)
    {
        if (Area.gameView == GameView.Spectator)
        {
            intruder.RenderIntruder(true);
            intruder.RenderIntruderFov(true);
            RenderGuard(true);
        }
        else if (Area.gameView == GameView.Guard)
        {
            RenderGuard(true);
            if (seen)
                intruder.RenderIntruder(true);
            else
                intruder.RenderIntruder(false);

            intruder.RenderIntruderFov(false);
        }
    }

    // Render the guard and the FoV if seen by the intruder
    public void RenderGuard(bool isSeen)
    {
        Renderer.enabled = isSeen;
        FovRenderer.enabled = isSeen;
    }

    public abstract Vector2? GetPatrolGoal();

    // Add the FoV to the Overall Seen Area
    public void AccumulateSeenArea()
    {
        // If there is no area seen start with the guards current vision
        if (SeenArea.Count == 0)
        {
            SeenArea.Add(GetFovPolygon());
        }
        else
        {
            // Merge with the total seen area by this guard
            SeenArea = PolygonHelper.MergePolygons(GetFov(), SeenArea, ClipType.ctUnion);
        }

        CheckForFoundHidingSpots();
    }

    // 
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

    // Check if a spot is seen
    void CheckForFoundHidingSpots()
    {
        List<Vector2> hidingSpots = World.GetHidingSpots();

        int index = 0;
        while (index < hidingSpots.Count)
        {
            if (IsPointInFoV(hidingSpots[index]))
            {
                hidingSpots.RemoveAt(index);
                m_FoundHidingSpots++;
            }
            else
            {
                index++;
            }
        }
    }

    // Check if a point is in the FoV
    public bool IsPointInFoV(Vector2 point)
    {
        bool isIn = GetFovPolygon().IsPointInPolygon(point, true);
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