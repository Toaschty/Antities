using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InfoDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string InfoText;

    private TMPro.TMP_Text Info;

    private void Start()
    {
        Info = GameObject.FindGameObjectWithTag("InfoDisplay").GetComponent<TMPro.TMP_Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Info.text = InfoText;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Info.text = "";
    }

    private void OnDisable()
    {
        Info.text = "";
    }

    private void OnDestroy()
    {
        Info.text = "";
    }
}
