using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurveyScores : SurveyItem
{
    private Dictionary<string, ScoresTable> _scoresTables;

    
    private string surveyScorePath = "Prefabs/UIs/ScoresTable";
    private GameObject surveyScorePrefab;
    
    public override void Initiate(string _name, ItemType _type, Survey _survey, string _code)
    {
        base.Initiate(_name, _type, _survey, _code);
        
        _scoresTables = new Dictionary<string, ScoresTable>();
        
        surveyScorePrefab = (GameObject) Resources.Load(surveyScorePath);

        GameObject surveyItemGo;
        foreach (var session in GameManager.Instance.GetClosedSessions())
        {
            surveyItemGo = Instantiate(surveyScorePrefab, inputPanel);
            _scoresTables.Add(session.guardColor, surveyItemGo.GetComponent<ScoresTable>());
            
            _scoresTables[session.guardColor].Initiate(session);
        }
        
   
        // surveyItemGo.SetActive(false);
    }

    public override void ProcessAnswer(string answer)
    {
        throw new System.NotImplementedException();
    }
    
    public override bool IsAnswerValid(string answer)
    {
        return true;
    }
}
