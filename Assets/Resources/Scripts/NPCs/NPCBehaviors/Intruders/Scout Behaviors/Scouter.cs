using UnityEngine;
using UnityEngine.Serialization;

public abstract class Scouter : MonoBehaviour
{
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
        foreach (var hs in _HsC.GetHidingSpots())
            hs.ResetCheck();
    }


    public abstract void Refresh(GameType gameType);


    public virtual void OnDrawGizmos()
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

public abstract class ScouterParams
{
    
}