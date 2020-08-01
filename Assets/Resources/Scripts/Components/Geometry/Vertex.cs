using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vertex 
{
    // position of the vertex
    public Vector2 position;

    // index of the vertex
    public int index;
    
    // If the vertex is an ear
    public bool isEar;

    // Create the vertex
    public Vertex(Vector2 _position, int _index)
    {
        position = _position;
        index = _index;
    }    
    
    public override bool Equals(object obj)
    {
        if (obj.GetType() != typeof(Vertex))
            return false;
        return Equals((Vertex)obj);
    }
    
    public bool Equals(Vertex obj)
    {
        return obj.position.Equals(position) && obj.index == index;
    }

    
    public override int GetHashCode()
    {
        unchecked
        {
            return (position.GetHashCode() * 397) ^ index;
        }
    }

    public override string ToString()
    {
        return string.Format("{0} ({1})", position, index);
    }

}
