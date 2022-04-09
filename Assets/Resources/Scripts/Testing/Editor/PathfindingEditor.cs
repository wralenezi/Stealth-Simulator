using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(PathfindingTester))]
public class PathfindingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PathfindingTester pathf = (PathfindingTester) target;
        if (GUILayout.Button("PathFind"))
        {
            pathf.PathFind();
        }
    }
}