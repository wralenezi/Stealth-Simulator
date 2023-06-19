using System.Collections.Generic;
using UnityEngine;

public class CollectablesManager : MonoBehaviour
{
    private CoinSpawner m_coinManager;
    
    public static CollectablesManager Instance; 
    public void Initialize(Session session)
    {
        Instance = this;
        
        m_coinManager = gameObject.AddComponent<CoinSpawner>();
        m_coinManager.Initiate(session, MapManager.Instance.GetNavMesh());
    }
    
    public void Reset(Session session)
    {
        m_coinManager.Reset(session, MapManager.Instance.GetNavMesh());
    }
    
    public void Disable()
    {
        m_coinManager.DisableCoins();
    }

    public void SpreadCollectables()
    {
        m_coinManager.SpawnCoins(MapManager.Instance.GetNavMesh());
    }

    public Vector2? GetGoalPosition(GameType gameType)
    {
        List<Coin> coins = m_coinManager.GetCoins();

        if (coins.Count > 0)
            return coins[0].gameObject.transform.position;

        return null;
    }
}
