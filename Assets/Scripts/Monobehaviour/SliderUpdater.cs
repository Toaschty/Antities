using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderUpdater : MonoBehaviour
{
    public TMPro.TMP_Text SliderText;
    public bool Integer;
    public bool Percent;

    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void UpdateText()
    {
        if (Integer)
            SliderText.text = slider.value.ToString();
        else
            SliderText.text = slider.value.ToString("F3");

        if (Percent)
            SliderText.text += " %";
    }
}
