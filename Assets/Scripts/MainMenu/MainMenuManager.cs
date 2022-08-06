using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private bool _playButtonClicked = false;
    private (int, int)[] _resolutions = new (int, int)[] { (1024, 768), (1280, 720), (1440, 900), (1664, 978), (1680, 1050), (1920, 1080), (2560, 1440) };
    private const string _resolutionPrefKey = "Resolution";
    private const string _fullScreenModePrefKey = "FullScreenMode";
    private const int _defaultResolutionIndex = 2;
    private const int _defaultFullScreenModeIndex = 1;
    [SerializeField] private GameObject _videoSettingsModal;
    [SerializeField] private Dropdown _resolutionDropdown;
    [SerializeField] private Dropdown _fullScreenModeDropdown;
    public static readonly string SelectUnitsControlPrefKey = "SelectUnitsControl";
    public static readonly string SelectMultipleControlPrefKey = "SelectMultipleControl";
    public static readonly string ShootControlPrefKey = "ShootControl";
    public static readonly string MoveForwardControlPrefKey = "MoveForwardControl";
    public static readonly string MoveBackwardControlPrefKey = "MoveBackwardControl";
    public static readonly string FirstAbilityControlPrefKey = "1stAbilityControl";
    public static readonly string SecondAbilityControlPrefKey = "2ndAbilityControl";
    public static readonly string ThirdAbilityControlPrefKey = "3rdAbilityControl";
    public static readonly string FourthAbilityControlPrefKey = "4thAbilityControl";
    public static readonly string FifthAbilityControlPrefKey = "5thAbilityControl";
    public static readonly string HornControlPrefKey = "HornControl";
    public static readonly string CameraControlPrefKey = "CameraControl";
    public static readonly string StandingsControlPrefKey = "StandingsControl";
    [SerializeField] private GameObject _controlsModal;
    [SerializeField] private Text _selectUnitsButtonText;
    [SerializeField] private Text _selectMultipleButtonText;
    [SerializeField] private Text _shootButtonText;
    [SerializeField] private Text _moveForwardButtonText;
    [SerializeField] private Text _moveBackwardButtonText;
    [SerializeField] private Text _1stAbilityButtonText;
    [SerializeField] private Text _2ndAbilityButtonText;
    [SerializeField] private Text _3rdAbilityButtonText;
    [SerializeField] private Text _4thAbilityButtonText;
    [SerializeField] private Text _5thAbilityButtonText;
    [SerializeField] private Text _hornButtonText;
    [SerializeField] private Text _cameraButtonText;
    [SerializeField] private Text _standingsButtonText;
    private readonly string _selectUnitsButtonDefaultText = KeyCode.Mouse0.GetKeyName();
    private readonly string _selectMultipleButtonDefaultText = KeyCode.LeftAlt.GetKeyName();
    private readonly string _shootButtonDefaultText = KeyCode.Mouse1.GetKeyName();
    private readonly string _moveForwardButtonDefaultText = KeyCode.W.GetKeyName();
    private readonly string _moveBackwardButtonDefaultText = KeyCode.Q.GetKeyName();
    private readonly string _1stAbilityButtonDefaultText = KeyCode.Alpha1.GetKeyName();
    private readonly string _2ndAbilityButtonDefaultText = KeyCode.Alpha2.GetKeyName();
    private readonly string _3rdAbilityButtonDefaultText = KeyCode.Alpha3.GetKeyName();
    private readonly string _4thAbilityButtonDefaultText = KeyCode.Alpha4.GetKeyName();
    private readonly string _5thAbilityButtonDefaultText = KeyCode.Alpha5.GetKeyName();
    private readonly string _hornButtonDefaultText = KeyCode.H.GetKeyName();
    private readonly string _cameraButtonDefaultText = KeyCode.Space.GetKeyName();
    private readonly string _standingsButtonDefaultText = KeyCode.Tab.GetKeyName();
    [SerializeField] private Text[] _controlsButtonsTexts;
    private List<KeyCode> _validKeys;
    private bool _awaitingKeyPress = false;
    private Text _pressedButtonText;
    private string _pressedButtonPreviousText;
    private AudioSource _warningAudioSource;
    [SerializeField] private GameObject _overlayPreventingButtonClicks;

    // TODO - Tips (randomly chosen from a pool)

    private void Awake()
    {
        _warningAudioSource = GetComponent<AudioSource>();
        _validKeys = new List<KeyCode>() { KeyCode.BackQuote, KeyCode.Minus, KeyCode.Equals, KeyCode.LeftBracket,
            KeyCode.RightBracket, KeyCode.Backslash, KeyCode.Semicolon, KeyCode.Quote, KeyCode.Comma, KeyCode.Period,
            KeyCode.Slash, KeyCode.Space, KeyCode.Backspace, KeyCode.Tab, KeyCode.CapsLock, KeyCode.Return,
            KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt };
        for (KeyCode i = KeyCode.A; i <= KeyCode.Z; ++i)
        {
            _validKeys.Add(i);
        }
        for (KeyCode i = KeyCode.Alpha0; i < KeyCode.Alpha9; ++i)
        {
            _validKeys.Add(i);
        }
        for (KeyCode i = KeyCode.Mouse0; i < KeyCode.Mouse6; ++i)
        {
            _validKeys.Add(i);
        }
    }

    private void Start()
    {
        int index = GetPreferredResolutionIndex();
        FullScreenMode fullScreenMode = GetPreferredFullScreenModeIndex() == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(_resolutions[index].Item1, _resolutions[index].Item2, fullScreenMode);
    }

    private void Update()
    {
        if (_awaitingKeyPress)
        {
            if (Input.anyKeyDown)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _awaitingKeyPress = false;
                    _overlayPreventingButtonClicks.SetActive(false);
                    _pressedButtonText.text = _pressedButtonPreviousText;
                }
                else
                {
                    KeyCode keyPressed = KeyCode.None;
                    foreach (KeyCode key in _validKeys)
                    {
                        if (Input.GetKeyDown(key))
                        {
                            keyPressed = key;
                            _awaitingKeyPress = false;
                            _overlayPreventingButtonClicks.SetActive(false);
                            break;
                        }
                    }
                    if (keyPressed == KeyCode.None)
                    {
                        _warningAudioSource.Play();
                    }
                    else
                    {
                        foreach (Text text in _controlsButtonsTexts)
                        {
                            if (text.text == keyPressed.GetKeyName())
                            {
                                // Another control had the pressed key assigned => these 2 controls swap keys
                                text.text = _pressedButtonPreviousText;
                                break;
                            }
                        }
                        _pressedButtonText.text = keyPressed.GetKeyName();
                    }
                }
            }
        }
    }

    public void Play()
    {
        if (_playButtonClicked)
        {
            return;
        }
        _playButtonClicked = true;
        SceneManager.LoadScene("ConnectToServerScene");
    }

    public void OpenVideoSettingsModal()
    {
        _videoSettingsModal.SetActive(true);
        _resolutionDropdown.value = GetPreferredResolutionIndex();
        _fullScreenModeDropdown.value = GetPreferredFullScreenModeIndex();
    }

    public void ApplyVideoSettings()
    {
        PlayerPrefs.SetInt(_resolutionPrefKey, _resolutionDropdown.value);
        PlayerPrefs.SetInt(_fullScreenModePrefKey, _fullScreenModeDropdown.value);
        int index = _resolutionDropdown.value;
        FullScreenMode fullScreenMode = _fullScreenModeDropdown.value == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(_resolutions[index].Item1, _resolutions[index].Item2, fullScreenMode);
    }

    private int GetPreferredResolutionIndex()
    {
        return PlayerPrefs.HasKey(_resolutionPrefKey) ? PlayerPrefs.GetInt(_resolutionPrefKey) : _defaultResolutionIndex;
    }

    private int GetPreferredFullScreenModeIndex()
    {
        return PlayerPrefs.HasKey(_fullScreenModePrefKey) ? PlayerPrefs.GetInt(_fullScreenModePrefKey) : _defaultFullScreenModeIndex;
    }

    public void OpenControlsModal()
    {
        _controlsModal.SetActive(true);
        if (PlayerPrefs.HasKey(SelectUnitsControlPrefKey))
        {
            // If a custom control has been saved, then all controls have been saved
            _selectUnitsButtonText.text = ((KeyCode)PlayerPrefs.GetInt(SelectUnitsControlPrefKey)).GetKeyName();
            _selectMultipleButtonText.text = ((KeyCode)PlayerPrefs.GetInt(SelectMultipleControlPrefKey)).GetKeyName();
            _shootButtonText.text = ((KeyCode)PlayerPrefs.GetInt(ShootControlPrefKey)).GetKeyName();
            _moveForwardButtonText.text = ((KeyCode)PlayerPrefs.GetInt(MoveForwardControlPrefKey)).GetKeyName();
            _moveBackwardButtonText.text = ((KeyCode)PlayerPrefs.GetInt(MoveBackwardControlPrefKey)).GetKeyName();
            _1stAbilityButtonText.text = ((KeyCode)PlayerPrefs.GetInt(FirstAbilityControlPrefKey)).GetKeyName();
            _2ndAbilityButtonText.text = ((KeyCode)PlayerPrefs.GetInt(SecondAbilityControlPrefKey)).GetKeyName();
            _3rdAbilityButtonText.text = ((KeyCode)PlayerPrefs.GetInt(ThirdAbilityControlPrefKey)).GetKeyName();
            _4thAbilityButtonText.text = ((KeyCode)PlayerPrefs.GetInt(FourthAbilityControlPrefKey)).GetKeyName();
            _5thAbilityButtonText.text = ((KeyCode)PlayerPrefs.GetInt(FifthAbilityControlPrefKey)).GetKeyName();
            _hornButtonText.text = ((KeyCode)PlayerPrefs.GetInt(HornControlPrefKey)).GetKeyName();
            _cameraButtonText.text = ((KeyCode)PlayerPrefs.GetInt(CameraControlPrefKey)).GetKeyName();
            _standingsButtonText.text = ((KeyCode)PlayerPrefs.GetInt(StandingsControlPrefKey)).GetKeyName();
        }
        else
        {
            _selectUnitsButtonText.text = _selectUnitsButtonDefaultText;
            _selectMultipleButtonText.text = _selectMultipleButtonDefaultText;
            _shootButtonText.text = _shootButtonDefaultText;
            _moveForwardButtonText.text = _moveForwardButtonDefaultText;
            _moveBackwardButtonText.text = _moveBackwardButtonDefaultText;
            _1stAbilityButtonText.text = _1stAbilityButtonDefaultText;
            _2ndAbilityButtonText.text = _2ndAbilityButtonDefaultText;
            _3rdAbilityButtonText.text = _3rdAbilityButtonDefaultText;
            _4thAbilityButtonText.text = _4thAbilityButtonDefaultText;
            _5thAbilityButtonText.text = _5thAbilityButtonDefaultText;
            _hornButtonText.text = _hornButtonDefaultText;
            _cameraButtonText.text = _cameraButtonDefaultText;
            _standingsButtonText.text = _standingsButtonDefaultText;
        }
    }

    public void SetAwaitingKeyPress(Text buttonText)
    {
        _awaitingKeyPress = true;
        _overlayPreventingButtonClicks.SetActive(true);
        _pressedButtonText = buttonText;
        _pressedButtonPreviousText = buttonText.text;
        buttonText.text = "_";
    }

    public void ApplyControlsSettings()
    {
        PlayerPrefs.SetInt(SelectUnitsControlPrefKey, (int)_selectUnitsButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(SelectMultipleControlPrefKey, (int)_selectMultipleButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(ShootControlPrefKey, (int)_shootButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(MoveForwardControlPrefKey, (int)_moveForwardButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(MoveBackwardControlPrefKey, (int)_moveBackwardButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(FirstAbilityControlPrefKey, (int)_1stAbilityButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(SecondAbilityControlPrefKey, (int)_2ndAbilityButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(ThirdAbilityControlPrefKey, (int)_3rdAbilityButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(FourthAbilityControlPrefKey, (int)_4thAbilityButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(FifthAbilityControlPrefKey, (int)_5thAbilityButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(HornControlPrefKey, (int)_hornButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(CameraControlPrefKey, (int)_cameraButtonText.text.GetKeyCode());
        PlayerPrefs.SetInt(StandingsControlPrefKey, (int)_standingsButtonText.text.GetKeyCode());        
    }

    public void ExitApplication()
    {
        Application.Quit();
    }
}
