using UnityEngine;
using System;

/// <summary>
/// Manages global visual styles for blocks.
/// 10 distinct styles that rotate when 2+ lines are cleared.
/// </summary>
public class StyleManager : MonoBehaviour
{
    public static StyleManager Instance { get; private set; }

    public event Action OnStyleChanged;

    [SerializeField] private int _currentStyleIndex = 0;
    public int CurrentStyleIndex => _currentStyleIndex;

    public const int MAX_STYLES = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NextStyle()
    {
        _currentStyleIndex = (_currentStyleIndex + 1) % MAX_STYLES;
        
        // Clear the texture cache when style changes
        TextureUtils.ClearCache();
        
        OnStyleChanged?.Invoke();
        Debug.Log($"Style changed to: {_currentStyleIndex}");
    }

    public void ResetStyle()
    {
        _currentStyleIndex = 0;
        TextureUtils.ClearCache();
        OnStyleChanged?.Invoke();
    }
}
