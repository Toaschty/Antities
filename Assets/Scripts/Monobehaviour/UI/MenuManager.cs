using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject[] Menus;

    private int currentMenu = -1;

    public void ToggleMenu(int id)
    {
        // Check if currently open menu should be closed
        if (currentMenu != -1)
        {
            Menus[currentMenu].SetActive(false);
            Menus[currentMenu].GetComponent<IMenu>().CloseMenu();
        }

        if (currentMenu == id)
        {
            Menus[id].SetActive(false);
            Menus[currentMenu].GetComponent<IMenu>().CloseMenu();
            currentMenu = -1;
        }
        else
        {
            Menus[id].SetActive(true);
            Menus[id].GetComponent<IMenu>().OpenMenu();
            currentMenu = id;
        }
    }
}
