using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldState
{
    private static Dictionary<string, string> _worldState;

    public static void Initialize()
    {
        _worldState = new Dictionary<string, string>();
    }

    public static void Reset()
    {
        _worldState.Clear();
    }

    public static void Set(string name, string value)
    {
        _worldState[name] = value;
    }


    public static string Get(string name)
    {
        return _worldState.TryGetValue(name, out string value) ? value : "";
    }
    

    // Get the time spent since a timeStamp
    public static int GetTimeSince(string timeStamp)
    {
        return Mathf.RoundToInt(Time.time - float.Parse(Get(timeStamp)));
    }

    // Get the heading from a position to another in words
    public static string GetHeading(Vector2 from, Vector2 to)
    {
        Vector2 dir = (to - from).normalized;

        string heading = "";

        if (Mathf.Abs(dir.y) > 0.5f)
        {
            if (dir.y > 0.5f)
                heading += "north";
            else if (dir.y < -0.5f)
                heading += "south";
            // Add a separator
            heading += "-";
        }

        if (dir.x > 0.5f)
            heading += "east";
        else if (dir.x < -0.5f)
            heading += "west";

        if (heading == "")
            heading = "around";

        return heading;
    }
    
    // get the world state as a string
    public static string GetWorldState()
    {
        string result = "";

        foreach (var pair in _worldState)
        {
            result += pair.Key + " = " + pair.Value + "\n";
        }

        return result;
    }
}