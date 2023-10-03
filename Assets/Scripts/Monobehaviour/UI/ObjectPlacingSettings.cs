using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacingSettings : MonoBehaviour, IMenu
{
    public GameObject[] Objects;

    public void SelectColony()
    {
        DeselectAll();

        Objects[0].transform.GetChild(0).gameObject.SetActive(true);
    }

    public void SelectFood()
    {
        DeselectAll();
        Objects[1].transform.GetChild(0).gameObject.SetActive(true);
    }

    public void SelectTree()
    {
        DeselectAll();
        Objects[2].transform.GetChild(0).gameObject.SetActive(true);
    }

    private void DeselectAll()
    {
        foreach (GameObject obj in Objects)
            obj.transform.GetChild(0).gameObject.SetActive(false);
    }

    public void OpenMenu()
    {
    }

    public void CloseMenu()
    {
    }
}
