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
        _HsC = new HidingSpotsCtrlr(mapManager, mapManager.mapRenderer.GetMapBoundingBox(), 10, 10);
    }

    public virtual void Begin()
    {
        foreach (var hs in _HsC.GetHidingSpots())
            hs.lastFailedTimeStamp = 0;

        intruderBehavior = GameManager.Instance.GetActiveArea().GetSessionInfo().intruderBehavior;
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
    /// <summary>
    /// Path Cancelling method
    /// </summary>
    public PathCanceller pathCancel;

    public RiskThresholdType thresholdType;

    public TrajectoryType trajectoryType;

    public override string ToString()
    {
        string output = "";
        string sep = "_";

        output += pathCancel;
        output += sep;
        
        output += thresholdType;
        output += sep;
        
        output += trajectoryType;

        return output;
    }
}



public enum PathCanceller
{
    DistanceCalculation,
    
    RiskComparison,
    
    None
}
