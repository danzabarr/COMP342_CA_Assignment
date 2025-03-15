using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class BarIndicator : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI text;

    public string format = "0.00";

    public void SetValue(float value)
    {
        slider.value = value;
        text.text = value.ToString(format);
    }

    public void SetPercentage(float percentage)
    {
        slider.value = slider.minValue + (slider.maxValue - slider.minValue) * percentage;
        text.text = (percentage * 100).ToString(format) + "%";
    }
}
