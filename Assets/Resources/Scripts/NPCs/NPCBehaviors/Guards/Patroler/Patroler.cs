using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class Patroler : MonoBehaviour
{

    private const float updateFreqInSec = 0.5f;
    private float _countdownToUpdateInSec = 0f;
    
    
    public abstract void Initiate(MapManager mapManager, GuardBehaviorParams _params);


    public abstract void Start();


    public abstract void UpdatePatroler(List<Guard> guards, float speed, float timeDelta);

    public abstract void Patrol(List<Guard> guards);


    public bool IsTimeToUpdate(float timeDelta)
    {
        _countdownToUpdateInSec -= timeDelta;
        
        if (_countdownToUpdateInSec <= 0f)
        {
            _countdownToUpdateInSec = updateFreqInSec;
            return true;
        }

        return false;
    }

}

[Serializable]
public abstract class PatrolerParams
{
    
}