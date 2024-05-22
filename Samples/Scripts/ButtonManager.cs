using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public Color onColor;
    public Color offColor;
    
    private bool state = false;
    public bool State
    {
        get { return state; }
        set
        {
            state = value;
            SetColor(value);
        }
    }

    Button button => GetComponent<Button>();

    private void SetColor(bool value)
    {
        if (value)
            button.GetComponent<Image>().color = onColor;
        else
            button.GetComponent<Image>().color = offColor;
    }

    public void SetState()
    {
        State = !State;
    }

    public void SetState(bool val)
    {
        State = val;
    }
}
