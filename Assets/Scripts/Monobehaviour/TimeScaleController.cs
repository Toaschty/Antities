using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TimeScaleController : MonoBehaviour
{
    public Sprite[] TimeStateImages;

    private Image stateImage;
    private TimeState timeState = TimeState.Pause;

    private void Start()
    {
        stateImage = GetComponent<Image>();
    }

    public void SwitchTimeState()
    {
        // Toggle to next state
        timeState++;

        if (timeState > TimeState.SpeedThree)
            timeState = TimeState.Pause;

        // Set sprite according to current state
        stateImage.sprite = TimeStateImages[(int)timeState];

        // Set timescale according to current state
        Time.timeScale = (int)timeState;

        EventSystem.current.SetSelectedGameObject(null);
    }
}

enum TimeState
{
    Pause = 0,
    SpeedOne = 1,
    SpeedTwo = 2,
    SpeedThree = 3,
}