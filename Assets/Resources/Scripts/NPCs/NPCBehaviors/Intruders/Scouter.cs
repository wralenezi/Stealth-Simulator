using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scouter : MonoBehaviour
{
    private StealthArea m_SA;
    
    public virtual void Initiate(StealthArea stealthArea)
    {
        m_SA = stealthArea;
    }

    // Get the stealth area
    protected StealthArea GetSA()
    {
        return m_SA;
    }



}
