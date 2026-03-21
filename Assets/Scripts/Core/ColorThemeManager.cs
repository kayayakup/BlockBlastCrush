using UnityEngine;
using System.Collections.Generic;

public class ColorThemeManager : MonoBehaviour
{
    public static ColorThemeManager Instance { get; private set; }

    [Header("Settings")]
    public float cycleSpeed = 0.01f;
    public float saturation = 0.35f;
    public float lightness = 0.25f;

    private float _hue = 0f;
    
    public Color CurrentBGColor { get; private set; }
    public Color CurrentGridBGColor { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        _hue += Time.deltaTime * cycleSpeed;
        if (_hue > 1f) _hue -= 1f;

        // Current Background Color
        CurrentBGColor = Color.HSVToRGB(_hue, saturation, lightness);

        ApplyColors();
    }

    private void ApplyColors()
    {
        // 1. Update Camera Background Color (Environment)
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = CurrentBGColor;
        }
    }
}
