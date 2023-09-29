using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TerrainToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject TerrainSettings;
    public GameObject Tooltip;

    private bool currentState = false;

    public void ToggleMenu()
    {
        currentState = !currentState;

        TerrainSettings.SetActive(currentState);

        // Deactive tooltip
        Tooltip.SetActive(!currentState);

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!currentState)
            Tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!currentState)
            Tooltip.SetActive(false);
    }
}
