using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerLabelController : MonoBehaviour
{
    private Quaternion m_initRotation;
    private Transform playerTransform;

    private IEnumerator blinking;
    private float blinkingSpeed = 0.5f;

    private TextMeshPro m_text;

    private float currentValue;
    private float startC;
    private float endC;


    public void Initiate(Transform _transform)
    {
        playerTransform = _transform;
        m_initRotation = playerTransform.rotation;
        m_text = transform.Find("Label").GetComponent<TextMeshPro>();
        startC = 0f;
        endC = 1f;
        currentValue = 0f;
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
        m_text.color = new Color(value, value, 1f);
    }


    public IEnumerator FadeAway()
    {
        while (true)
        {
            yield return new WaitForSeconds(blinkingSpeed);
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }


    public void HideLabel()
    {
        gameObject.SetActive(false);
    }
}