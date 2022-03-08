using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcsManager : MonoBehaviour
{
    // Guards manager
    private GuardsManager m_guardsManager;

    // Intruder Manager
    private IntrudersManager m_intrudersManager;

    [SerializeField]
    public StateMachine m_state;

    // Score 
    private float m_score;

    public static NpcsManager Instance;

    public void Initialize(Session session, MapManager mapManager)
    {
        Instance ??= this;

        // Assign the Guard Manager
        GameObject guardsOG = new GameObject("Guards");
        guardsOG.transform.parent = transform;
        m_guardsManager = guardsOG.AddComponent<GuardsManager>();
        m_guardsManager.Initiate(session, mapManager);

        // Create the Guards
        m_guardsManager.CreateGuards(session, mapManager.GetNavMesh());
        
        // Add the Intruder manager
        GameObject intrudersOG = new GameObject("Intruders");
        intrudersOG.transform.parent = transform;
        m_intrudersManager = intrudersOG.AddComponent<IntrudersManager>();
        m_intrudersManager.Initiate(session, mapManager);

        // Create the intruders
        m_intrudersManager.CreateIntruders(session, m_guardsManager.GetGuards(), mapManager.GetNavMesh());

        m_state = new StateMachine();

        ResetState();
    }

    public void ResetState()
    {
        // Set the guards to the default mode (patrol)
        ChangeState<Patrol>();
    }

    public void Reset(List<MeshPolygon> navMesh, Session session)
    {
        m_score = 0f;
        AreaUIManager.Instance.UpdateScore(m_score, m_score);
        m_guardsManager.Reset(navMesh, session);
        m_intrudersManager.Reset(navMesh, GetGuards(), session);
    }

    public void Done()
    {
        m_guardsManager.Done();
    }

    public void Move(float timeDelta)
    {
        m_guardsManager.Move(GetState(),timeDelta);
        m_intrudersManager.Move(GetState(),timeDelta);
    }

    public void CastVision()
    {
        m_guardsManager.CastVision();
        m_intrudersManager.CastVision();
    }

    public void MakeDecisions()
    {
        m_state.UpdateState();
    }

    public List<Guard> GetGuards()
    {
        return m_guardsManager.GetGuards();
    }

    public List<Intruder> GetIntruders()
    {
        return m_intrudersManager.GetIntruders();
    }

    public void Speak(NPC speaker, string lineType, float prob)
    {
        if (speaker is Guard)
            m_guardsManager.Speak(speaker, lineType, prob);
    }


    public void ExecuteState()
    {
        m_state.UpdateState();
    }
    
    public void CoinPicked()
    {
        if (GetState() is Search)
        {
            IncrementScore(20f);
        }
    }

    public void IncrementScore(float score)
    {
        float oldScore = m_score;
        m_score += score;
        m_score = Mathf.Max(0, m_score);
        AreaUIManager.Instance.UpdateScore(m_score, oldScore);
    }



    /// <summary>
    /// Change the current guard state to a new one.
    /// </summary>
    /// <param name="state"> The new state </param>
    public void ChangeState<T>() where T : State, new()
    {
        T state = new T();
        state.MakeState(m_guardsManager.GetController(), m_intrudersManager.GetController());
        m_state.ChangeState(state);
    }

    // Get current state
    public State GetState()
    {
        return m_state.GetState();
    }

    public void ProcessNpcsVision(Session session)
    {
        bool intruderSpotted = false;
        NPC spotter = null;
        foreach (var guard in GetGuards())
        {
            // Check if any intruders are spotted
            bool seen = guard.SpotIntruders(GetIntruders());

            if (intruderSpotted) continue;

            intruderSpotted = seen;
            spotter = guard;
        }

        // Render guards if the intruder can see them
        foreach (var intruder in GetIntruders())
        {
            intruder.SpotGuards(GetGuards());

            // if (session.gameType == GameType.CoinCollection)
            //     intruder.SpotCoins(m_SA.coinSpawner.GetCoins());
        }

        // Switch the state of the guards 
        if (intruderSpotted)
        {
            // Guards knows the intruders location
            // m_guardsManager.GetController().StartChase(GetIntruders()[0]);
            ChangeState<Chase>();
            Speak(spotter, "Spot", 1f);
        }
        else if (GetState() is Chase)
            // if the intruder is not seen and the guards were chasing then start searching
        {
            // Change the guard state
            ChangeState<Search>();
            // m_guardsManager.GetController().StartSearch(GetIntruders()[0]);
        }
    }
}