using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CoinSpawner : MonoBehaviour
{
    private int concurrentCoinCount = 1;
    private GameObject m_coinPrefab;

    private List<Coin> m_coins;

    private List<Intruder> m_intruders;

    private bool _isRandom = true;

    public void Initiate(Session session, List<MeshPolygon> navMesh)
    {
        m_coins = new List<Coin>();
        m_coinPrefab = (GameObject) Resources.Load("Prefabs/Coin");
        
        _isRandom = session.guardBehaviorParams.patrolerParams?.GetType() != typeof(ScriptedPatroler);

        concurrentCoinCount = session.coinCount;
        Reset(session, navMesh);
    }

    public void Reset(Session session, List<MeshPolygon> navMesh)
    {
        DestroyCoins();
        if (session.gameType == GameType.CoinCollection)
        {
            CreateCoins();
            SpawnCoins(navMesh);
        }
        else if (session.gameType == GameType.Stealth)
            DisableCoins();
    }

    public void CreateCoins()
    {
        for (int i = 0; i < concurrentCoinCount; i++)
        {
            GameObject coinGo = Instantiate(m_coinPrefab, transform);
            coinGo.SetActive(false);
            Coin coin = coinGo.GetComponent<Coin>();
            coin.Initiate();
            m_coins.Add(coin);
        }
    }

    public void SwitchActive()
    {
        foreach (var coin in m_coins)
            coin.gameObject.SetActive(!gameObject.activeSelf);
    }
    
    public void DisableCoins()
    {
        foreach (var coin in m_coins)
            coin.gameObject.SetActive(false);
    }

    public void SpawnCoins(List<MeshPolygon> navMesh)
    {
        foreach (var coin in m_coins)
        {
            // int randIndex = Random.Range(0, navMesh.Count);
            // Vector2 coinPos = navMesh[randIndex].GetRandomPosition();

            // Vector2 coinPos = NpcsManager.Instance.GetIntruders()[0].GetTransform().position;

            Vector2 coinPos = PathFinding.Instance.GetPointFromCorner(Properties.NpcRadius * 2f);
            coin.Spawn(coinPos, navMesh, MapManager.Instance.mapData, _isRandom);
        }
    }

    public void DestroyCoins()
    {
        while (m_coins.Count > 0)
        {
            Coin coin = m_coins[0];
            Destroy(coin.gameObject);
            m_coins.RemoveAt(0);
        }
    }


    public List<Coin> GetCoins()
    {
        return m_coins;
    }
}