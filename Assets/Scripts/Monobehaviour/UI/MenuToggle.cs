using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject TerrainSettings;
    public TMPro.TMP_Text Info;

    public string InfoText;

    private bool currentState = false;

    public void ToggleMenu()
    {
        currentState = !currentState;

        TerrainSettings.SetActive(currentState);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseMenu()
    {
        currentState = false;

        TerrainSettings.SetActive(false);

        Info.text = "";

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Info.text = InfoText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Info.text = "";
    }
}
