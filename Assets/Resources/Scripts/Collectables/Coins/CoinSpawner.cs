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

    public void Inititate(Session session, List<MeshPolygon> navMesh)
    {
        m_coins = new List<Coin>();
        m_coinPrefab = (GameObject) Resources.Load("Prefabs/Coin");

        if (session.gameType == GameType.CoinCollection)
        {
            CreateCoins();
            Reset(navMesh);
        }
        else if (session.gameType == GameType.Stealth)
            DisableCoins();
    }

    public void Reset(List<MeshPolygon> navMesh)
    {
        SpawnCoins(navMesh);
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

    public void DisableCoins()
    {
        foreach (var coin in m_coins)
        {
            coin.gameObject.SetActive(false);
        }
    }

    public void SpawnCoins(List<MeshPolygon> navMesh)
    {
        foreach (var coin in m_coins)
        {
            int randIndex = Random.Range(0, navMesh.Count);
            Vector2 coinPos = navMesh[randIndex].GetRandomPosition();
            coin.Spawn(coinPos, navMesh);
        }
    }


    public List<Coin> GetCoins()
    {
        return m_coins;
    }
}