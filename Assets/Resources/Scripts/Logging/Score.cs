using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    private StealthArea m_stealthArea;

    // Buttons
    // For ending the current area.
    private Button m_endAreaBtn;
    
    public void Initiate(StealthArea stealthArea)
    {
        m_stealthArea = stealthArea;
        
        // Assign the buttons reference
        // m_endAreaBtn = transform.GetChild(0).Find("end_btn").GetComponent<Button>();
        //
        // m_endAreaBtn.onClick.AddListener(EndArea);
    }
    //
    // // End the area
    // private void EndArea()
    // {
    //     m_stealthArea.EndArea();
    // }

}
