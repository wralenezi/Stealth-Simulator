using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

// Used for the search representation the game 
public abstract class Searcher : MonoBehaviour
{
    // timestamp since the search started
    protected float m_SearchstartTimestamp;

    // Time it takes to make a decision for all guards
    public float DecisionTimeInSeconds;
    public float UpdateTimeInSeconds;

    // How long the search will know the intruder's position after the search start.
    private float m_CheatingDuration = 0f;

    // Intruder being searched
    protected Intruder m_Intruder;

    // If the searcher still know the intruder's position
    protected bool isStillCheating;

    public virtual void Initiate(MapManager mapManager, GuardBehaviorParams guardParams)
    {
    }

    /// <summary>
    /// Start the search logic given the last known position and direction of the intruder.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="dir"></param>
    public virtual void StartSearch(Intruder intruder)
    {
        m_Intruder = intruder;

        m_SearchstartTimestamp = StealthArea.GetElapsedTimeInSeconds();
        
        WorldStateController.LostTrackOfIntruder(intruder);
        
        StartCoroutine(RememberIntruderDetails());
    }

    IEnumerator RememberIntruderDetails()
    {
        isStillCheating = true;
        yield return new WaitForSeconds(m_CheatingDuration);
        CommenceSearch(m_Intruder);
        isStillCheating = false;
    }

    public abstract void CommenceSearch(NPC target);
    
    public void UpdateRepresentation(float speed, List<Guard> guards, float timeDelta)
    {
        if (isStillCheating) return;
        float timeBefore = Time.realtimeSinceStartup;
        UpdateSearcher(speed, guards, timeDelta);
        UpdateTimeInSeconds = (Time.realtimeSinceStartup - timeBefore);
    }

    protected abstract void UpdateSearcher(float speed, List<Guard> guards, float timeDelta);
    
    public void Decide(List<Guard> guards)
    {
        if (isStillCheating) return;
        float timeBefore = Time.realtimeSinceStartup;
        Search(guards);
        DecisionTimeInSeconds = (Time.realtimeSinceStartup - timeBefore);
    }
    
    protected abstract void Search(List<Guard> guard);

    // The search is over so clear the variables
    public virtual void Clear()
    {
        isStillCheating = true;
    }
    
}


public abstract class SearcherParams
{
    
}