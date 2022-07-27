using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Patroler : MonoBehaviour
{

    public abstract void Initiate(MapManager mapManager, GuardBehaviorParams _params);


    public abstract void Start();


    public abstract void UpdatePatroler(List<Guard> guards, float speed, float timeDelta);

    public abstract void Patrol(List<Guard> guards);

}

public abstract class PatrolerParams
{
    
}