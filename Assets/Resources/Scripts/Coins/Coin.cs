using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Coin : MonoBehaviour
{
    private StealthArea m_StealthArea;
    private Renderer renderer;
    
    // Hide coins when spawned
    private bool m_isHideCoinWhenSpawned;

    private AudioSource audioSource;

    private List<Intruder> m_intruders;

    public void Initiate(List<Intruder> npcs)
    {
        m_StealthArea = GameManager.Instance.GetActiveArea();
        renderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.4f;
        m_intruders = npcs;
    }

    public void Spawn()
    {
        List<MeshPolygon> navMesh = MapManager.Instance.GetNavMesh();

        Vector2 currentPos = m_intruders[0].GetTransform().position;

        bool positionFound = false;
        Vector2? chosenPos = null;
        int numberRemainingAttempts = 100;
        while (numberRemainingAttempts > 0 && !positionFound)
        {
            int random = Random.Range(0, navMesh.Count);
            Vector2 pos = navMesh[random].GetRandomPosition();
            
            float distance = PathFinding.GetShortestPathDistance(currentPos, pos);

            if (distance > Properties.MaxPathDistance * 0.4f)
            {
                positionFound = true;
                chosenPos = pos;
            }

            numberRemainingAttempts--;
        }

        transform.position = Equals(chosenPos, null) ? transform.position : (Vector3) chosenPos.Value;
        renderer.enabled = !m_isHideCoinWhenSpawned;
        gameObject.SetActive(true);
    }

    public bool IsCoinSeen(Vector2 pos)
    {
        foreach (var npc in m_intruders)
        {
            bool seen = npc.GetFovPolygon().IsCircleInPolygon(pos, 0.5f);

            if (seen)
                return true;
        }

        return false;
    }

    public void Render()
    {
        renderer.enabled = true;
    }

    public void ModifyScore()
    {
        // m_StealthArea.guardsManager.CoinPicked();
    }


    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name.Contains("Intruder"))
        {
            audioSource.Play();
            ModifyScore();
            Spawn();
            other.gameObject.GetComponent<Intruder>().AddCoin();
        }
    }
}