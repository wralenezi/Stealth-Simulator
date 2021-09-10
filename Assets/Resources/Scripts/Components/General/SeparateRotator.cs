using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is a component to separate the rotation of its gameobject from the parent gameobject 
public class SeparateRotator : MonoBehaviour
{
    // Initial quaternion value
    private Quaternion m_InitRotation;

    public void Awake()
    {
        m_InitRotation = transform.parent.transform.rotation;
    }


    public void Update()
    {
        gameObject.transform.rotation = m_InitRotation;
    }
}
