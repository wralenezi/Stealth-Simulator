using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Implements a List structure as a cyclical list where indices are wrapped.
public class CyclicalList<T> : List<T>
{
    public CyclicalList()
    {
    }

    // Get the element by index
    public new T this[int index]
    {
        get
        {
            //perform the index wrapping
            while (index < 0)
                index = Count + index;
            if (index >= Count)
                index %= Count;

            return base[index];
        }
        set
        {
            //perform the index wrapping
            while (index < 0)
                index = Count + index;
            if (index >= Count)
                index %= Count;

            base[index] = value;
        }
    }
    
    public CyclicalList(IEnumerable<T> collection)
        : base(collection)
    {
    }

    // Remove the element 
    public new void RemoveAt(int index)
    {
        Remove(this[index]);
    }
}