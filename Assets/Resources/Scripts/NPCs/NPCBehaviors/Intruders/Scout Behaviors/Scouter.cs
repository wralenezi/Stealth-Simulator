using UnityEngine;

public abstract class Scouter : MonoBehaviour
{
    public IntruderBehavior intruderBehavior;

    // Hiding spots manager
    public bool ShowHidingSpots;
    protected HidingSpotsCtrlr _HsC;

    public virtual void Initiate(MapManager mapManager, Session session)
    {
        // ShowHidingSpots = true;
        float mapArea = mapManager.mapDecomposer.GetNavMeshArea();
        int rowCount = Mathf.RoundToInt(mapArea * 0.01f);
        int colCount = Mathf.RoundToInt(mapArea * 0.01f);
        _HsC = new HidingSpotsCtrlr(mapManager, mapManager.mapRenderer.GetMapBoundingBox(), colCount, rowCount);
    }

    public virtual void Begin()
    {
        intruderBehavior = GameManager.Instance.GetActiveArea().GetSessionInfo().intruderBehavior;

        foreach (var hs in _HsC.GetHidingSpots())
            hs.ResetCheck();
        
    }


    public abstract void Refresh(GameType gameType);


    public void OnDrawGizmos()
    {
        if (ShowHidingSpots)
            _HsC?.DrawHidingSpots();
    }

    protected Vector2? GetDestination(GameType gameType)
    {
        Vector2? goal = null;

        switch (gameType)
        {
            case GameType.CoinCollection:
                goal = CollectablesManager.Instance.GetGoalPosition(gameType);
                break;

            case GameType.StealthPath:
                break;
        }

        return goal;
    }
}

public struct IntruderBehavior
{
    public SpotsNeighbourhoods spotsNeighbourhood;

    /// <summary>
    /// Path Cancelling method
    /// </summary>
    public PathCanceller pathCancel;

    public RiskThresholdType thresholdType;

    public TrajectoryType trajectoryType;

    public float maxRiskAsSafe;
    
    public GoalPriority goalPriority;

    public SafetyPriority safetyPriority;

    public float fovProjectionMultiplier;

    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += spotsNeighbourhood;
        output += sep;

        output += pathCancel;
        output += sep;

        output += thresholdType;
        output += sep;
        
        output += fovProjectionMultiplier;
        output += sep;            
            
        output += trajectoryType;
        output += sep;

        output += goalPriority;
        output += sep;

        output += safetyPriority;
        output += sep;

        output += maxRiskAsSafe;
        
        return output;
    }
}


public enum PathCanceller
{
    DistanceCalculation,

    RiskComparison,

    None
}