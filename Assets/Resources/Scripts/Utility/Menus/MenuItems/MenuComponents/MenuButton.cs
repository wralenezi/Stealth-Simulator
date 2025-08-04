using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    private MenuItem m_buttonItem;
    private MenuOption _choice;
    
    public void Initiate(MenuItem buttonItem, MenuOption choice)
    {
        m_buttonItem = buttonItem;
        _choice = choice;
        gameObject.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        _choice.onClick();
        m_buttonItem.Answer(_choice.value);
    }
}