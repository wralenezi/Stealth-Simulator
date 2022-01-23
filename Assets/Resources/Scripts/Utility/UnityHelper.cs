using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UnityHelper
{
    public static T AddChildComponent<T>(Transform parent, string _name) where T : MonoBehaviour
    {
        GameObject meshManagerGO = new GameObject(_name);
        meshManagerGO.transform.parent = parent;
        return meshManagerGO.AddComponent<T>();
    }
}