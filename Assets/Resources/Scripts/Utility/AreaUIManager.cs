﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AreaUIManager : MonoBehaviour
{
    // Guard state label
    public TextMeshProUGUI guardsLabel { set; get; }

    // Remaining episode time label
    public TextMeshProUGUI timeLabel { set; get; }

    // Score of the player
    public TextMeshProUGUI scoreLabel { set; get; }

    // Announcing label
    public TextMeshProUGUI announcementLabel { set; get; }

    private RectTransform canvasRect;

    private Transform timeGo;

    private IEnumerator blinking;
    private float blinkingSpeed = 0.5f;

    private IEnumerator scoreShaking;

    private Vector2 timeLabelPosition;

    public static float Score;

    public void Initiate()
    {
        canvasRect = GetComponent<RectTransform>();

        guardsLabel = transform.Find("Guard state label").GetComponent<TextMeshProUGUI>();

        timeGo = transform.Find("Time");
        timeLabelPosition = timeGo.transform.position;
        timeLabel = timeGo.Find("label").GetComponent<TextMeshProUGUI>();

        scoreLabel = transform.Find("Score").GetComponent<TextMeshProUGUI>();

        announcementLabel = transform.Find("Announcement").GetComponent<TextMeshProUGUI>();
        announcementLabel.text = "";

        Score = 0f;
    }


    public void Reset()
    {
        scoreLabel.color = Color.white;
        scoreShaking = null;

        timeLabel.gameObject.SetActive(true);
    }


    // Update the label of the status of the game.
    public void UpdateGuardLabel(IState state)
    {
        if (state is Chase)
        {
            guardsLabel.text = "Alert";
            guardsLabel.color = Color.red;
        }
        else if (state is Search)
        {
            guardsLabel.text = "Searching";
            guardsLabel.color = Color.yellow;
        }
        else if (state is Patrol)
        {
            guardsLabel.text = "Normal";
            guardsLabel.color = Color.green;
        }
    }

    public void UpdateGuardLabel(string name, Color color)
    {
        guardsLabel.color = color;
        guardsLabel.text = name + " Team";
    }

    public void DisplayLabel(string text)
    {
        announcementLabel.text = text;
    }

    public void UpdateTime(float remainingTime)
    {
        int time = Mathf.RoundToInt(remainingTime);
        timeLabel.text = time.ToString();

        if (time <= 3f)
        {
            timeLabel.color = Color.red;

            // timeGo.position = canvasRect.sizeDelta / 2f;

            if (blinking == null)
            {
                blinking = Blinking();
                StartCoroutine(blinking);
            }
        }
        else
        {
            timeGo.position = timeLabelPosition;
            timeLabel.color = Color.white;

            if (blinking != null)
                StopCoroutine(blinking);

            blinking = null;
        }
    }

    private IEnumerator Blinking()
    {
        timeGo.GetComponent<AudioSource>().Play();

        while (true)
        {
            yield return new WaitForSeconds(blinkingSpeed);
            timeLabel.gameObject.SetActive(!timeLabel.gameObject.activeSelf);
        }
    }

    public void UpdateScore(float score)
    {
        Score = score;

        if (GameManager.Instance.GetActiveArea().GetSessionInfo().gameType == GameType.CoinCollection)
            scoreLabel.text = "Score: " + score;
        else
            scoreLabel.text = "Score: " + score + " %";
    }


    public void ShakeScore(float scoreChange)
    {
        if (scoreShaking == null)
        {
            if (scoreChange <= 0f)
                scoreShaking = ShakeText(scoreLabel, Color.red);
            else
                scoreShaking = ShakeText(scoreLabel, Color.green);

            StartCoroutine(scoreShaking);
        }
    }


    private IEnumerator ShakeText(TextMeshProUGUI textMeshPro, Color color)
    {
        Color originalColor = Color.white;
        Vector2 originalPosition = textMeshPro.transform.position;

        int shakeCount = 50;
        for (int i = 0; i < shakeCount; i++)
        {
            textMeshPro.transform.position += (Vector3) Vector2.right * Mathf.Pow(-1, i);
            textMeshPro.color = Color.Lerp(color, originalColor, i / shakeCount);
            yield return new WaitForSeconds(0.01f);
        }

        textMeshPro.color = Color.white;
        textMeshPro.transform.position = originalPosition;
        scoreShaking = null;
    }
}