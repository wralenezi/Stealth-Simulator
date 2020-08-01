using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterceptPoint
{
    public Vector2 Position;
    public float Probability = 0.1f;
    public InterceptPoint Parent;

    public InterceptPoint(Vector2 position)
    {
        Position = position;
    }

    public void Draw()
    {
        if (Parent != null)
            Gizmos.DrawLine(Parent.Position, Position);
        Gizmos.DrawSphere(Position, Probability);
    }
}