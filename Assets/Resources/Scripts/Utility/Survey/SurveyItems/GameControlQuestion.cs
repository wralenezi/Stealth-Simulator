using System;

public class GameControlQuestion : SurveyMultiple
{
    public override void ProcessAnswer(string _answer)
    {
        switch (type)
        {
            case ItemType.RepeatEpisode:
                RepeatEpisode(_answer);
                break;
            
            case ItemType.SkipEpisode:
                SkipEpisode(_answer);
                break;
            
            case ItemType.Survey:
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void RepeatEpisode(string _answer)
    {
        if (!Equals(_answer, "No")) return;
        
        // GameManager.Instance.EndCurrentGame();
        GameManager.Instance.ClearArea();
        survey.GetManager().AddIntroToFirstHalf();
    }

    public void SkipEpisode(string _answer)
    {
        if(!Equals(_answer, "Yes")) return;
        
        GameManager.Instance.ClearArea();
        // GameManager.Instance.EndCurrentGame();
        survey.SetQuestionIndex(survey.GetQuestionsCount() - 2);
    }
}