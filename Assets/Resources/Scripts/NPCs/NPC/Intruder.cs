using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Intruder : NPC
{
    // The place the intruder was last seen in 
    private Vector2? m_lastKnownLocation;

    // Count of how many time this intruder has been spotted by guards
    private int m_NoTimesSpotted;

    // Total time being chased and visible
    private float m_AlertTime;

    // Total time being chased and invisible 
    private float m_SearchedTime;

    // Number of collected coins
    private int m_CollectCoins;

    private PlayerLabelController m_PlayerLabel;


    public override void Initiate(StealthArea area, NpcData data)
    {
        base.Initiate(area, data);

        // Multiply the intruder's speed
        NpcSpeed *= Properties.IntruderSpeedMulti;
        NpcRotationSpeed *= Properties.IntruderRotationSpeedMulti;

        GameObject gameLabel = Resources.Load<GameObject>("Prefabs/PlayerLabel");
        GameObject gameLabelGo = Instantiate(gameLabel, transform);

        m_PlayerLabel = gameLabelGo.GetComponent<PlayerLabelController>();
        m_PlayerLabel.Initiate(GetTransform());
    }

    public override void ResetNpc()
    {
        base.ResetNpc();

        m_NoTimesSpotted = 0;
        m_AlertTime = 0f;
        m_SearchedTime = 0f;
        m_CollectCoins = 0;
    }

    public void HideLabel()
    {
        m_PlayerLabel.HideLabel();
    }

    // Run the state the intruder is in
    public void ExecuteState()
    {
        // if (GetNpcData().intruderPlanner != IntruderPlanner.UserInput)
        // {
        //     m_state.UpdateState();
        // }
    }

    public void UpdateMetrics(IState state, float timeDelta)
    {
        base.UpdateMetrics(timeDelta);

        // If the guards are alert for the presence of an intruder
        if (state is Chase)
        {
            m_AlertTime += timeDelta;
        }
        // If the guards are searching for an intruder 
        else if (state is Search)
        {
            m_SearchedTime += timeDelta;
        }
    }

    // In the case of intruder nothing to be done in this function yet
    public override void ClearLines()
    {
    }

    public override bool IsBusy()
    {
        return base.IsBusy() || isWaiting;
    }


    public Vector2 GetLastKnownLocation()
    {
        return m_lastKnownLocation.Value;
    }

    // Render the guard and the FoV if seen by the intruder
    public void RenderIntruder(bool isSeen)
    {
        Renderer.enabled = isSeen;
    }

    public void RenderIntruderFov(bool isSeen)
    {
        FovRenderer.enabled = isSeen;
    }

    // Intruder is seen so update the known location of the intruder 
    public void Seen()
    {
        m_lastKnownLocation = transform.position;
    }

    // Rendering 
    public void SpotGuards(List<Guard> guards)
    {
        foreach (var guard in guards)
        {
            if (Area.gameView == GameView.Spectator)
            {
                guard.RenderGuard(true);
                RenderIntruder(true);
            }
            else if (Area.gameView == GameView.Intruder)
            {
                RenderIntruder(true);

                if (GetFovPolygon().IsCircleInPolygon(guard.transform.position, 0.5f))
                    guard.RenderGuard(true);
                else
                    guard.RenderGuard(false);
            }
        }
    }


    public void SpotCoins(List<Coin> coins)
    {
        foreach (var coin in coins.Where(coin => GetFovPolygon().IsCircleInPolygon(coin.transform.position, 0.3f)))
        {
            coin.Render();
        }
    }

    public void AddCoin()
    {
        m_CollectCoins++;
    }


    private bool isWaiting = false;

    public IEnumerator waitThenMove(Vector2 goal)
    {
        isWaiting = true;
        float waitTime = Random.Range(5f, 20f);

        yield return new WaitForSeconds(waitTime);

        if (!IsBusy())
            SetGoal(goal, false);

        isWaiting = false;
    }


    public float GetPercentAlertTime()
    {
        return m_AlertTime / Properties.EpisodeLength;
    }

    public float GetAlertTime()
    {
        return m_AlertTime;
    }

    public int GetNumberOfTimesSpotted()
    {
        return m_NoTimesSpotted;
    }


    public override LogSnapshot LogNpcProgress()
    {
        return new LogSnapshot(GetTravelledDistance(), StealthArea.GetElapsedTime(), Data, "Chased",
            m_NoTimesSpotted, GuardsManager.GuardsOverlapTime,
            m_AlertTime, m_SearchedTime, 0, 0f, m_CollectCoins);
    }
}