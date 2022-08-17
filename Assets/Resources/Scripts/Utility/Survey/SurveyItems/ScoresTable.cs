using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoresTable : MonoBehaviour
{
    private Image _titleImage;
    private Text _title;

    private GameObject rowObject;
    private float templatRow = 2f;

    private GameObject _scoreList;

    private List<GameObject> rows;

    private Color lineColor1 = Color.black;
    private Color lineColor2 = Color.white;
    
    public void Initiate(Session session)
    {
        _titleImage = transform.Find("title_bg").GetComponent<Image>();
        _titleImage.color = Color.black;
        
        _title = transform.Find("Title").GetComponent<Text>();
        _title.text = session.guardColor + " Team";

        _scoreList = transform.Find("ScoresList").gameObject;
        rowObject = _scoreList.transform.GetChild(0).gameObject;
        rowObject.SetActive(false);
        
        rows = new List<GameObject>();
        
        LoadLeaderboard(session._scores);
        
    }

    public void LoadLeaderboard(List<ScoreRecord> scores)
    {
        for (int i = 0; i < scores.Count; i++)
        {
            GameObject rowOb = Instantiate(rowObject, _scoreList.transform);
            RectTransform rowRectTransform = rowOb.GetComponent<RectTransform>();
            rowRectTransform.anchoredPosition = new Vector2(0, -templatRow * i);
            rowOb.SetActive(true);
            rows.Add(rowOb);

            SetRow(rowOb, i % 2 == 0 ? lineColor1 : lineColor2, scores[i]);
        }
        
    }

    private void SetRow(GameObject rowOb, Color bgColor, ScoreRecord scoreRecord)
    {
        bgColor.a = 0.05f;
        rowOb.GetComponent<Image>().color = bgColor;

        rowOb.transform.Find("Rank").GetComponent<Text>().text = scoreRecord.rank.ToString().PadLeft(2,'0');
        rowOb.transform.Find("Score").GetComponent<Text>().text = scoreRecord.score.ToString().PadLeft(6,'0');
        rowOb.transform.Find("Name").GetComponent<Text>().text = scoreRecord.name;
    }

}


public class ScoreRecord
{
    public int rank;
    public float score;
    public string name;

    public ScoreRecord(int _rank, float _score, string _name)
    {
        rank = _rank;
        score = _score;
        name = _name;
    }
}
