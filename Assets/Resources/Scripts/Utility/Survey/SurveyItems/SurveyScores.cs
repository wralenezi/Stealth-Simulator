using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurveyScores : SurveyItem
{

    
    
    public override void Initiate(string _name, ItemType _type, Survey _survey, string _code)
    {
        base.Initiate(_name, _type, _survey, _code);
        
        
        
    }

    public override void ProcessAnswer(string answer)
    {
        throw new System.NotImplementedException();
    }
}
