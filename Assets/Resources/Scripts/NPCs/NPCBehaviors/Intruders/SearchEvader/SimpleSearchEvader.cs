using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSearchEvader : SearchEvader
{
    private SimpleSearchEvaderParams _params;


    public override void Initiate(MapManager mapManager, Session session)
    {
        base.Initiate(mapManager, session);
        _params = (SimpleSearchEvaderParams) session.IntruderBehaviorParams.searchEvaderParams;
    }

    public override void Begin()
    {
        foreach (var intruder in NpcsManager.Instance.GetIntruders())
            intruder.ClearIntruderGoal();
    }

    public override void Refresh()
    {
        foreach (var intruder in NpcsManager.Instance.GetIntruders())
        {
            if (intruder.IsBusy()) return;

            if (!_params.IsReadyToMove())
            {
                _params.DecrementTime();
                return;
            }

            _params.SetTimer();

            Vector2? goal = null;

            switch (_params.destinationType)
            {
                case DestinationType.Heurisitic:
                    m_HsC.AssignHidingSpotsFitness(NpcsManager.Instance.GetGuards());
                    goal = m_HsC.GetBestHidingSpot();
                    break;

                case DestinationType.Random:
                    goal = m_HsC.GetRandomSpot();
                    break;
            }


            if (!Equals(goal, null))
                intruder.SetDestination(goal.Value, true, false);
        }
    }
}

public class SimpleSearchEvaderParams : SearchEvaderParams
{
    public DestinationType destinationType;
    private float _waitRemainingTime;

    private float _minWaitTime;
    private float _maxWaitTime;


    public SimpleSearchEvaderParams(DestinationType destinationType, float minWaitTime, float maxWaitTime)
    {
        _minWaitTime = minWaitTime;
        _maxWaitTime = maxWaitTime;
        this.destinationType = destinationType;
    }

    public bool IsReadyToMove()
    {
        return _waitRemainingTime <= 0f;
    }

    public void DecrementTime()
    {
        _waitRemainingTime -= Time.deltaTime;
    }

    public void SetTimer()
    {
        _waitRemainingTime = Random.Range(_minWaitTime, _maxWaitTime);
    }

    public override string ToString()
    {
        string output = "";
        string sep = "_";
        
        output += GetType().ToString();
        output += sep;

        output += destinationType;
        output += sep;
        
        output += _minWaitTime;
        output += sep;
        
        output += _maxWaitTime;

        return output;
    }
}


public enum DestinationType
{
    Random,
    Heurisitic
}