using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    public GameObject[] Menus;

    private int currentMenu = -1;
    private int previousMenu = 0;

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

    private void Update()
    {
        // Quick Menu Toggle
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (currentMenu == -1)
            {
                ToggleMenu(previousMenu);
            }
            else
            {
                previousMenu = currentMenu;
                ToggleMenu(currentMenu);
            }
        }
    }
}
