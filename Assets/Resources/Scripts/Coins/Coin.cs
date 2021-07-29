using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class Coin : MonoBehaviour
{
    private bool m_isFree;
    private StealthArea m_StealthArea;
    private Renderer renderer;
    private AudioSource audioSource;

    private List<Intruder> m_intruders;

    public void Initiate(List<Intruder> npcs)
    {
        m_StealthArea = GameManager.instance.GetActiveArea();
        renderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        m_intruders = npcs;
    }

    public void Spawn()
    {
        List<MeshPolygon> navMesh = m_StealthArea.mapDecomposer.GetNavMesh();

        Vector2? pos = null;
        while (pos == null || IsCoinSeen(pos.Value))
        {
            int random = Random.Range(0, navMesh.Count);
            pos = navMesh[random].GetRandomPosition();
        }

        transform.position = pos.Value;
        m_isFree = false;
        renderer.enabled = false;
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

    public bool IsFree()
    {
        return m_isFree;
    }

    public void Render()
    {
        renderer.enabled = true;
    }

    public void ModifyScore()
    {
        m_StealthArea.guardsManager.CoinPicked();
    }


    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.name.Contains("Intruder"))
        {
            audioSource.Play();
            ModifyScore();
            Spawn();
        }
    }
}