using System.Collections.Generic;

// An Index-able cyclical linked list
public class IndexableCyclicalLinkedList<T> : LinkedList<T>
{
    // Get the linked list node at an index
    public LinkedListNode<T> this[int index]
    {
        get
        {
            //perform the index wrapping
            while (index < 0)
                index = Count + index;
            if (index >= Count && Count != 0)
                index %= Count;


            //find the proper node
            LinkedListNode<T> node = First;
            for (int i = 0; i < index; i++)
                node = node.Next;

            return node;
        }
    }

    // Removes the node at a given index.
    public void RemoveAt(int index)
    {
        Remove(this[index]);
    }

    // Finds the index of a given item.
    public int IndexOf(T item)
    {
        for (int i = 0; i < Count; i++)
            if (this[i].Value.Equals(item))
                return i;

        return -1;
    }
}