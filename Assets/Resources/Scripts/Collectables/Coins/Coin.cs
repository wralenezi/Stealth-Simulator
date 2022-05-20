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
            chosenPos = GetGoalPosition(startPosition, navMesh, mapData);
        else
            chosenPos = GetFurthestRandomPosition(startPosition, navMesh);


        transform.position = Equals(chosenPos, null) ? transform.position : (Vector3) chosenPos.Value;
        renderer.enabled = !m_isHideCoinWhenSpawned;
        gameObject.SetActive(true);
    }


    private Vector2 GetFurthestRandomPosition(Vector2 startPos, List<MeshPolygon> navMesh)
    {
        MeshPolygon furthestPolygon = null;
        float maxSqrMag = Mathf.NegativeInfinity;

        foreach (var p in navMesh)
        {
            Vector2 centroid = p.GetCentroidPosition();

            Vector2 offset = centroid - startPos;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > maxSqrMag)
            {
                furthestPolygon = p;
                maxSqrMag = sqrMag;
            }
        }


        int innerAttempts = 100;
        maxSqrMag = Mathf.NegativeInfinity;
        Vector2? furthestPoint = null;

        while (innerAttempts > 0)
        {
            innerAttempts--;
            Vector2 pos = furthestPolygon.GetRandomPosition();
            bool inPoly = furthestPolygon.IsCircleContainedInPolygon(pos, 0.3f);
            
            if(!inPoly) continue;

            Vector2 offset = pos - startPos;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > maxSqrMag)
            {
                maxSqrMag = sqrMag;
                furthestPoint = pos;
            }
        }

        return Equals(furthestPoint, null) ? furthestPolygon.GetCentroidPosition() : furthestPoint.Value;
    }


    private Vector2 GetRandomPosition(Vector2 startPos, List<MeshPolygon> navMesh)
    {
        Vector2? chosenPos = null;

        bool positionFound = false;
        int numberRemainingAttempts = 1000;
        float minDistance = PathFinding.Instance.longestShortestPath * 0.5f;

        while (numberRemainingAttempts > 0 && !positionFound)
        {
            int random = Random.Range(0, navMesh.Count);
            Vector2 pos = navMesh[random].GetRandomPosition();

            int innerAttempts = 1000;
            bool inPoly = false;
            while (!inPoly && innerAttempts > 0)
            {
                innerAttempts--;

                if (innerAttempts < 10)
                    pos = navMesh[random].GetCentroidPosition();
                else
                    pos = navMesh[random].GetRandomPosition();

                inPoly = navMesh[random].IsCircleContainedInPolygon(pos, 0.3f);
            }


            Vector2 offset = startPos - pos;
            float sqrMag = offset.sqrMagnitude;

            if (sqrMag > minDistance * minDistance)
            {
                positionFound = true;
                chosenPos = pos;
            }

            numberRemainingAttempts--;
        }

        return chosenPos.Value;
    }


    public Vector2 GetGoalPosition(Vector2 startPosition, List<MeshPolygon> navMesh, MapData mapData)
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
            // Debug.LogError("Goal location is missing for the map " + mapData.name + ".");
            return GetFurthestRandomPosition(startPosition, navMesh);
            // throw;
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
            // audioSource.Play();
            ModifyScore();
            Spawn(gameObject.transform.position, MapManager.Instance.GetNavMesh(), MapManager.Instance.mapData, false);
            other.gameObject.GetComponent<Intruder>().AddCoin();
        }
    }
}