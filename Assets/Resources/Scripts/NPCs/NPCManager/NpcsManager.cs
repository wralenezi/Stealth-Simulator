using System.Collections.Generic;
using UnityEngine;

public class NpcsManager : MonoBehaviour
{
    // Guards manager
    private GuardsManager m_guardsManager;

    // Intruder Manager
    private IntrudersManager m_intrudersManager;

    private StateMachine m_state;

    public static NpcsManager Instance;

    public void Initialize(Session session, MapManager mapManager)
    {
        Instance ??= this;

        // Add the Intruder manager
        GameObject intrudersOG = new GameObject("Intruders");
        intrudersOG.transform.parent = transform;
        m_intrudersManager = gameObject.AddComponent<IntrudersManager>();
        m_intrudersManager.Initiate(session, mapManager);

        // Assign the Guard Manager
        GameObject guardsOG = new GameObject("Guards");
        guardsOG.transform.parent = transform;
        m_guardsManager = guardsOG.AddComponent<GuardsManager>();
        m_guardsManager.Initiate(session, mapManager);

        // Create the Guards
        m_guardsManager.CreateGuards(session, mapManager.GetNavMesh());

        // Create the intruders
        m_intrudersManager.CreateIntruders(session, m_guardsManager.GetGuards(), mapManager.GetNavMesh());

        // Initiate the FSM to patrol for the guards
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
    public IState GetState()
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