using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Intruder : NPC
{
    // For debugging purposes
    public bool isGhost;

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
    
    private bool isWaiting = false;


    public override void Initiate(NpcData data, VoiceParams _voice)
    {
        base.Initiate(data, _voice);

        // Multiply the intruder's speed
        NpcSpeed *= Properties.IntruderSpeedMulti;
        NpcRotationSpeed *= Properties.IntruderRotationSpeedMulti;

        GameObject gameLabel = Resources.Load<GameObject>("Prefabs/PlayerLabel");
        GameObject gameLabelGo = Instantiate(gameLabel, transform);
        
        m_PlayerLabel = gameLabelGo.GetComponent<PlayerLabelController>();
        m_PlayerLabel.Initiate(GetTransform());
        
        // isGhost = true;
        // ShowPath = true;
    }

    public override void ResetNpc()
    {
        base.ResetNpc();
        m_PlayerLabel.Reset();
            
        m_NoTimesSpotted = 0;
        m_AlertTime = 0f;
        m_SearchedTime = 0f;
        m_CollectCoins = 0;
    }
    
    public void UpdateMetrics(State state, float timeDelta)
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

    public void IncrementAlertTime()
    {
        m_AlertTime += Time.deltaTime;
    }

    // In the case of intruder nothing to be done in this function yet
    public override void ClearLines()
    {
    }

    public override bool IsBusy()
    {
        return base.IsBusy() || isWaiting;
    }

    public void ClearIntruderGoal()
    {
        isWaiting = false;
    }


    public Vector2? GetLastKnownLocation()
    {
        return m_lastKnownLocation;
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
            if (GameManager.Instance.gameView == GameView.Spectator)
            {
                guard.RenderGuard(true);
                RenderIntruder(true);
            }
            else if (GameManager.Instance.gameView == GameView.Intruder)
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
    
    public IEnumerator waitThenMove(Vector2 goal)
    {
        isWaiting = true;
        float waitTime = Random.Range(5f, 20f);

        yield return new WaitForSeconds(waitTime);

        if (!IsBusy())
            SetDestination(goal, true, false);

        isWaiting = false;
    }
    
    public float GetPercentAlertTime()
    {
        return m_AlertTime / StealthArea.SessionInfo.episodeLengthSec;
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
        return new LogSnapshot(GetTravelledDistance(), StealthArea.GetElapsedTimeInSeconds(), Data, NpcsManager.Instance.GetState().name,
            m_NoTimesSpotted, 0f,
            m_AlertTime, m_SearchedTime, 0, 0f, m_CollectCoins);
    }
}