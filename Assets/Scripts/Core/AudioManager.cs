// ── AudioManager.cs ───────────────────────────────────────────────────────────
// Place in: Assets/Scripts/Core/AudioManager.cs
//
// All AudioClips are assigned via the Unity Inspector.
// Attach this script to a GameObject and drag your audio files
// into the corresponding fields in the Inspector.
//
// Sounds:
//   PlayPickup()    – block picked up from tray
//   PlayPlace()     – block placed on grid correctly
//   PlayReturn()    – block returned to tray (invalid placement)
//   PlayLineClear() – line(s) cleared
//   PlayScore()     – score awarded
//   PlaySpawn()     – new tray batch appeared
//   PlayGameOver()  – game over
// ─────────────────────────────────────────────────────────────────────────────
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── Inspector-assigned clips ──────────────────────────────────────────────
    [Header("Block Sounds")]
    [SerializeField] private AudioClip clipPickup;
    [SerializeField] private AudioClip clipPlace;
    [SerializeField] private AudioClip clipReturn;

    [Header("Grid Sounds")]
    [SerializeField] private AudioClip clipLineClear1;
    [SerializeField] private AudioClip clipLineClear2;

    [Header("UI / Feedback")]
    [SerializeField] private AudioClip clipScore;
    [SerializeField] private AudioClip clipSpawn;
    [SerializeField] private AudioClip clipGameOver;

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float volumePickup    = 0.80f;
    [Range(0f, 1f)] [SerializeField] private float volumePlace     = 0.85f;
    [Range(0f, 1f)] [SerializeField] private float volumeReturn    = 0.80f;
    [Range(0f, 1f)] [SerializeField] private float volumeClear     = 1.00f;
    [Range(0f, 1f)] [SerializeField] private float volumeScore     = 0.75f;
    [Range(0f, 1f)] [SerializeField] private float volumeSpawn     = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float volumeGameOver  = 0.85f;

    // ── Audio Sources ─────────────────────────────────────────────────────────
    private AudioSource _sfxSource;
    private AudioSource _bgSource;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    public void Initialize()
    {
        if (_sfxSource != null) return; // already initialised

        _sfxSource             = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.volume      = 1f;

        _bgSource              = gameObject.AddComponent<AudioSource>();
        _bgSource.playOnAwake  = false;
        _bgSource.volume       = 0.85f;

        bool savedMute = PlayerPrefs.GetInt("BlockPuzzle_Muted", 0) == 1;
        _sfxSource.mute = savedMute;
        _bgSource.mute = savedMute;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlayPickup()
    {
        if (clipPickup != null) _sfxSource.PlayOneShot(clipPickup, volumePickup);
    }

    public void ToggleMute()
    {
        bool muted = !_sfxSource.mute;
        _sfxSource.mute = muted;
        _bgSource.mute = muted;
        PlayerPrefs.SetInt("BlockPuzzle_Muted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsMuted => _sfxSource.mute;

    public void PlayPlace()
    {
        if (clipPlace != null) _sfxSource.PlayOneShot(clipPlace, volumePlace);
    }

    public void PlayReturn()
    {
        if (clipReturn != null) _sfxSource.PlayOneShot(clipReturn, volumeReturn);
    }

    public void PlayLineClear(int lines = 1)
    {
        var clip = (lines >= 2 && clipLineClear2 != null) ? clipLineClear2 : clipLineClear1;
        if (clip != null) _sfxSource.PlayOneShot(clip, volumeClear);
    }

    public void PlayScore()
    {
        if (clipScore != null) _sfxSource.PlayOneShot(clipScore, volumeScore);
    }

    public void PlaySpawn()
    {
        if (clipSpawn != null) _sfxSource.PlayOneShot(clipSpawn, volumeSpawn);
    }

    public void PlayGameOver()
    {
        if (clipGameOver != null) _bgSource.PlayOneShot(clipGameOver, volumeGameOver);
    }
}
