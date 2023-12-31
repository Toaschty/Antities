using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TimeScaleController : MonoBehaviour
{
    public Sprite[] TimeStateImages;

    private Image stateImage;
    private TimeState timeState = TimeState.Pause;
    private EntityQuery colonyQuery;

    private void Start()
    {
        stateImage = GetComponent<Image>();
        colonyQuery = Colony.GetQuery();
    }

    private void OnApplicationQuit()
    {
        colonyQuery.Dispose();
    }

    public void SwitchTimeState()
    {
        if (colonyQuery.IsEmpty)
            return;

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

    public void ResetTimeState()
    {
        // Time.timeScale = 0;
        timeState = 0;

        stateImage.sprite = TimeStateImages[0];
    }
}

enum TimeState
{
    Pause = 0,
    SpeedOne = 1,
    SpeedTwo = 2,
    SpeedThree = 3,
}