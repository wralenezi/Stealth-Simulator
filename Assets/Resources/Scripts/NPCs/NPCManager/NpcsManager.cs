using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class NpcsManager : MonoBehaviour
{
    // Guards manager
    private GuardsManager _guardsManager;

    // Intruder Manager
    private IntrudersManager _intrudersManager;

    [SerializeField] public StateMachine _state;

    // Score 
    private float m_score;

    public static NpcsManager Instance;

    public void Initialize(Session session, MapManager mapManager)
    {
        Instance = this;

        // Add the Intruder manager
        GameObject intrudersOG = new GameObject("Intruders");
        intrudersOG.transform.parent = transform;
        _intrudersManager = intrudersOG.AddComponent<IntrudersManager>();
        _intrudersManager.Initiate(session, mapManager);

        // Assign the Guard Manager
        GameObject guardsOG = new GameObject("Guards");
        guardsOG.transform.parent = transform;
        _guardsManager = guardsOG.AddComponent<GuardsManager>();
        _guardsManager.Initiate(session, mapManager);

        _state = new StateMachine();

        // Create the intruders
        _intrudersManager.CreateIntruders(session, _guardsManager.GetGuards(), mapManager.GetNavMesh());

        // Create the Guards
        _guardsManager.CreateGuards(session, mapManager.GetNavMesh());

        Reset(mapManager.GetNavMesh(), session);
    }

    public void ResetState()
    {
        // Set the guards to the default mode (patrol)
        ChangeState<Patrol>();
    }

    public void Reset(List<MeshPolygon> navMesh, Session session)
    {
        Instance = this;
        m_score = 0f;
        AreaUIManager.Instance.UpdateScore(m_score, m_score);
        _intrudersManager.Reset(navMesh, GetIntruders(), GetGuards(), session);
        _guardsManager.Reset(navMesh, GetIntruders(), session);
        ResetState();
    }

    public void Done()
    {
        _guardsManager.Done();
    }

    public void Move(float timeDelta)
    {
        _guardsManager.Move(GetState(), timeDelta);
        _intrudersManager.Move(GetState(), timeDelta);
    }

    public void CastVision()
    {
        _guardsManager.CastVision();
        _intrudersManager.CastVision();
    }

    public void MakeDecisions(GameType gameType)
    {
        _state.UpdateState(gameType);
    }

    public List<Guard> GetGuards()
    {
        return _guardsManager.GetGuards();
    }

    public List<Intruder> GetIntruders()
    {
        return _intrudersManager.GetIntruders();
    }

    public void Speak(NPC speaker, string lineType, float prob)
    {
        if (speaker is Guard)
            _guardsManager.Speak(speaker, lineType, prob);
    }


    public void ExecuteState(GameType gameType)
    {
        _state.UpdateState(gameType);
    }

    public void CoinPicked()
    {
        if (!(GetState() is Chase))
        {
            IncrementScore(1f);
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
        state.MakeState(_guardsManager.GetController(), _intrudersManager.GetController());
        _state.ChangeState(state);
    }

    // Get current state
    public State GetState()
    {
        return _state.GetState();
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

        // if (GetState() is Patrol)
        //     GetIntruders()[0].ModifyTimeInFOV(intruderSpotted ? Time.deltaTime : -Time.deltaTime);

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
            ChangeState<Chase>();
            Speak(spotter, "Spot", 1f);
        }
        else if (GetState() is Chase)
            // if the intruder is not seen and the guards were chasing then start searching
        {
            // Change the guard state
            ChangeState<Search>();
        }
    }
}