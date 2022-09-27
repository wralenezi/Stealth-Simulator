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
        ColorUtility.TryParseHtmlString(session.guardColor, out Color parsedColor);
        _titleImage.color = parsedColor;

        _title = transform.Find("Title").GetComponent<Text>();
        _title.text = session.guardColor + " Team";
        _title.text = char.ToUpper(_title.text[0]) + _title.text.Substring(1);

        _scoreList = transform.Find("ScoresList").gameObject;
        rowObject = _scoreList.transform.GetChild(0).gameObject;
        rowObject.SetActive(false);

        rows = new List<GameObject>();

        LoadLeaderboard(session);
    }

    public void LoadLeaderboard(Session session)
    {
        for (int i = 0; i < session._scores.Count; i++)
        {
            GameObject rowOb = Instantiate(rowObject, _scoreList.transform);
            RectTransform rowRectTransform = rowOb.GetComponent<RectTransform>();
            rowRectTransform.anchoredPosition = new Vector2(0, -templatRow * i);
            rowOb.SetActive(true);
            rows.Add(rowOb);

            Color color = i % 2 == 0 ? lineColor1 : lineColor2;

            float alpha = 0.05f;

            if (Equals(PlayerData.PlayerName, session._scores[i].name))
            {
                ColorUtility.TryParseHtmlString(session.guardColor, out Color parsedColor);
                color = parsedColor;
                alpha = 0.5f;
            }

            SetRow(rowOb, color, session._scores[i], alpha);
        }
    }

    private void SetRow(GameObject rowOb, Color bgColor, ScoreRecord scoreRecord, float alpha = 0.05f)
    {
        bgColor.a = alpha;
        rowOb.GetComponent<Image>().color = bgColor;

        rowOb.transform.Find("Rank").GetComponent<Text>().text = scoreRecord.rank.ToString().PadLeft(2, '0');
        rowOb.transform.Find("Score").GetComponent<Text>().text = scoreRecord.score.ToString();
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