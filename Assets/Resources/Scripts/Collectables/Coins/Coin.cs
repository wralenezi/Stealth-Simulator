using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Coin : MonoBehaviour
{
    private Renderer renderer;

    // Hide coins when spawned
    private bool m_isHideCoinWhenSpawned;

    private AudioSource audioSource;

    public void Initiate()
    {
        renderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.4f;
    }

    public void Spawn(Vector2 startPosition, List<MeshPolygon> navMesh, MapData mapData, bool isRandom)
    {
        Vector2? chosenPos = null;

        if (!isRandom)
        {
            chosenPos = GetGoalPosition(mapData);
        }
        else
        {
            bool positionFound = false;
            int numberRemainingAttempts = 100;
            while (numberRemainingAttempts > 0 && !positionFound)
            {
                int random = Random.Range(0, navMesh.Count);
                Vector2 pos = navMesh[random].GetRandomPosition();

                float distance = PathFinding.Instance.GetShortestPathDistance(startPosition, pos);

                if (distance > PathFinding.Instance.longestShortestPath * 0.4f)
                {
                    positionFound = true;
                    chosenPos = pos;
                }

                numberRemainingAttempts--;
            }
        }


        transform.position = Equals(chosenPos, null) ? transform.position : (Vector3) chosenPos.Value;
        renderer.enabled = !m_isHideCoinWhenSpawned;
        gameObject.SetActive(true);
    }

    public Vector2 GetGoalPosition(MapData mapData)
    {
        try
        {
            Dictionary<string, Vector2> goals = new Dictionary<string, Vector2>();

            goals.Add("MgsDock", new Vector2(13.87f, -0.28f));
            
            goals.Add("Hall", new Vector2(13.87f, -0.28f));

            return goals[mapData.name];
        }
        catch (Exception e)
        {
            Debug.LogError("Goal location is missing for the map "+mapData.name+".");
            throw;
        }
    }

    // public bool IsCoinSeen(Vector2 pos)
    // {
    //     foreach (var npc in m_intruders)
    //     {
    //         bool seen = npc.GetFovPolygon().IsCircleInPolygon(pos, 0.5f);
    //
    //         if (seen)
    //             return true;
    //     }
    //
    //     return false;
    // }

    public void Render()
    {
        renderer.enabled = true;
    }

    public void ModifyScore()
    {
        NpcsManager.Instance.CoinPicked();
    }


    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name.Contains("Intruder"))
        {
            audioSource.Play();
            ModifyScore();
            Spawn(gameObject.transform.position, MapManager.Instance.GetNavMesh(), MapManager.Instance.mapData, false);
            other.gameObject.GetComponent<Intruder>().AddCoin();
        }
    }
}