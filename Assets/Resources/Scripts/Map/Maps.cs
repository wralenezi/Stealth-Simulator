using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maps : MonoBehaviour
{
    private Dictionary<string, string> m_MapList;
    private Dictionary<string, string> m_RoadMapList;


    public void LoadMaps()
    {
        m_MapList = new Dictionary<string, string>();
        m_RoadMapList = new Dictionary<string, string>();

        string name = "MgsDock";

        string data =
            "-2.4,4.1,-2.4,3.1,2.1,3.1,2.1,2.6,2.6,2.6,2.6,3.1,4.1,3.1,4.1,1.1,7.6,1.1,7.6,-1.4,6.1,-1.4,6.1,-3.9,-3.9,-3.9,-3.9,-2.4,-4.4,-2.4,-4.4,-3.9,-5.4,-3.9,-5.4,-1.4,-3.4,-1.4,-3.4,1.6,-4.9,1.6,-4.9,3.1,-5.9,3.1,-5.9,1.6,-7.4,1.6,-7.4,4.1\n4.1,-2.4,5.1,-2.4,5.1,-2.9,4.1,-2.9\n-2.4,0.5999999,-1.9,0.5999999,-1.9,1.6,-0.4000001,1.6,-0.4000001,0.0999999,-2.4,0.0999999\n1.1,1.6,3.1,1.6,3.1,0.0999999,1.1,0.0999999\n-1.9,-0.9000001,-0.4000001,-0.9000001,-0.4000001,-2.4,-1.9,-2.4\n1.1,-0.9000001,3.1,-0.9000001,3.1,-2.4,1.1,-2.4";
    }


    private void LoadMap(string name, string data)
    {
        m_MapList.Add(name, data);
    }
}