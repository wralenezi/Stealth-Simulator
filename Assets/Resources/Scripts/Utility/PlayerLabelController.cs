using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLabelController : MonoBehaviour
{
    private Quaternion m_initRotation;
    private Transform playerTransform;

    private IEnumerator blinking;
    private float blinkingSpeed = 0.5f;

    private TextMeshPro m_text;
    private SpriteRenderer _arrowImage;

    private float currentValue;
    private float startC;
    private float endC;

    public float alpha;

    public void Initiate(Transform _transform)
    {
        playerTransform = _transform;
        m_initRotation = playerTransform.rotation;
        
        m_text = transform.Find("Label").GetComponent<TextMeshPro>();
        _arrowImage = transform.Find("arrow").GetComponent<SpriteRenderer>();
        
        startC = 0f;
        endC = 1f;
        currentValue = 0f;
    }

    public void Reset()
    {
        alpha = 1f;
    }

    private void Update()
    {
        gameObject.transform.position = transform.position;
        gameObject.transform.rotation = m_initRotation;

        float blinkingSpeed = 5f;
        currentValue += Time.deltaTime * blinkingSpeed;

        if (currentValue > 1.0f)
        {
            float temp = endC;
            endC = startC;
            startC = temp;
            currentValue = 0.0f;
        }

        float value = Mathf.Lerp(startC, endC, currentValue);
        m_text.color = new Color(value, value, 1f,alpha);
        _arrowImage.color = new Color(value, value, 1f,alpha);

        alpha -= 0.1f * Time.deltaTime;
    }

    // public void HideLabel()
    // {
    //     gameObject.SetActive(false);
    //     alpha = 0f;
    // }
}