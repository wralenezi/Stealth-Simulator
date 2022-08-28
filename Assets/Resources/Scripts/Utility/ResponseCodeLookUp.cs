using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResponseCodeLookUp : MonoBehaviour
{
    public static string GetMeaning(long code)
    {
        string meaning = code switch
        {
            200 => "OK",
            411 => "Roadmap for this scale is not available",
            _ => code.ToString()
        };

        return meaning;
    }
}
