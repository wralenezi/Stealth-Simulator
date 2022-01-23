using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CoinSpawner : MonoBehaviour
{
    private int concurrentCoinCount;
    private GameObject m_coinPrefab;
    
    private List<Coin> m_coins;

    private List<Intruder> m_intruders;
    
    public void Inititate()
    {
        m_coins = new List<Coin>();
        m_coinPrefab = (GameObject) Resources.Load("Prefabs/Coin");

        concurrentCoinCount = 1;
        CreateCoins();
    }

    public void Reset()
    {
        SpawnCoins();
    }

    public void CreateCoins()
    {
        for (int i = 0; i < concurrentCoinCount; i++)
        {
            GameObject coinGo = Instantiate(m_coinPrefab, transform);
            coinGo.SetActive(false);
            Coin coin = coinGo.GetComponent<Coin>();
            // coin.Initiate(GameManager.Instance.GetActiveArea().intrdrManager.GetIntruders());
            // m_coins.Add(coin);
        }
    }

    public void DisableCoins()
    {
        foreach (var coin in m_coins)
        {
            coin.gameObject.SetActive(false);;
        }
    }

    public void SpawnCoins()
    {
        foreach (var coin in m_coins)
        {
            coin.Spawn();
        } 
    }


    public List<Coin> GetCoins()
    {
        return m_coins;
    }
    
}
