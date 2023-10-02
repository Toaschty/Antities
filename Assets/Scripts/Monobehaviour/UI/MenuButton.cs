using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour
{
    public int MenuID;
    public GameObject Menu;

    private MenuManager menuManager;

    private void Awake()
    {
        menuManager = GameObject.FindGameObjectWithTag("MenuManager").GetComponent<MenuManager>();
    }

    public void ToggleMenu()
    {
        menuManager.ToggleMenu(MenuID);
        EventSystem.current.SetSelectedGameObject(null);
    }
}

