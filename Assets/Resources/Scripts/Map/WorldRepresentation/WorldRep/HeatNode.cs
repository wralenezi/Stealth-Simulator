using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatNode
{
    public int Col;
    public int Row;

    public Vector2 position;
    private float spottedTime;

    // A normalized value where 0 means the node was seen less
    public float heatValue;

    public HeatNode()
    {
        spottedTime = 0f;
    }

    public void Reset()
    {
        spottedTime = 0f;
        heatValue = 0f;
    }

    public void Increment(float timeDelta)
    {
        spottedTime += timeDelta;
    }

    public float GetTime()
    {
        return spottedTime;
    }

    public Color32 GetColor()
    {
        byte colorLevel = (byte) (heatValue * 255);
        Color32 color = new Color32(colorLevel, colorLevel, colorLevel, 255);

        return color;
    }
}