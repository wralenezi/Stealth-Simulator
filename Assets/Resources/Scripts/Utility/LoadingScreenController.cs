using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadingScreenController : MonoBehaviour
{
    private RectTransform m_ImageTransform;
    private TextMeshProUGUI m_Label;

    private float countDown = 1;

    // Start is called before the first frame update
    public void Initiate()
    {
        m_ImageTransform = GetComponent<RectTransform>();
        m_Label = transform.Find("Label").GetComponent<TextMeshProUGUI>();

        Camera cam = Camera.main;
        float height = 100f * cam.orthographicSize;
        float width = height * cam.aspect;

        m_ImageTransform.sizeDelta = new Vector2(width, height);
        m_Label.text = "Loading";
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        StartCoroutine(AddDots());
    }

    public void Deactivate()
    {
        m_Label.text = "Loading";
        gameObject.SetActive(false);
    }


    IEnumerator AddDots()
    {
        int count = 0;
        while (gameObject.activeInHierarchy)
        {
            if (count < 4)
            {
                yield return new WaitForSeconds(1f);
                m_Label.text += ".";
                count++;
            }
            else
            {
                m_Label.text = "Loading";
                count = 0;
            }
        }
    }
}