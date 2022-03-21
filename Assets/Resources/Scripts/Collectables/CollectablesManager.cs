using System.Collections.Generic;
using UnityEngine;

public class CollectablesManager : MonoBehaviour
{
    private CoinSpawner m_coinManager;
    
    // private StealthGoalController

    public static CollectablesManager Instance; 
    public void Initialize(Session session)
    {
        Instance ??= this;
        
        m_coinManager = gameObject.AddComponent<CoinSpawner>();
        m_coinManager.Inititate(session, MapManager.Instance.GetNavMesh());
    }
    
    public void Reset()
    {
        m_coinManager.Reset(MapManager.Instance.GetNavMesh());
    }

    public Vector2? GetGoalPosition(GameType gameType)
    {
        List<Coin> coins = m_coinManager.GetCoins();

        if (coins.Count > 0)
            return coins[0].gameObject.transform.position;

        return null;
    }
}
