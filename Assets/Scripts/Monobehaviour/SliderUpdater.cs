using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderUpdater : MonoBehaviour
{
    public TMPro.TMP_Text SliderText;

    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void UpdateText()
    {
        SliderText.text = slider.value.ToString("F3");
    }
}
