using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Saved mouse sensitivity used by the Controls menu and Camera Look.</summary>
public sealed class MouseSensitivitySettingsControl : MonoBehaviour
{
    public const string PlayerPrefsKey = "DOMS.MouseSensitivity";
    public const float DefaultValue = 1f;
    public const float MinimumValue = .1f;
    public const float MaximumValue = 3f;

    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;

    private static float currentValue = DefaultValue;
    private static bool valueLoaded;

    public static float Value
    {
        get
        {
            EnsureLoaded();
            return currentValue;
        }
    }

    private void Awake()
    {
        EnsureLoaded();

        // This row was copied from the Settings Generator's audio slider. Its
        // SliderUGUI and localization components keep writing percentage values
        // such as 0% and 3%, which conflicts with a .10 to 3.00 sensitivity.
        DisableCopiedSettingsComponents();

        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);
        if (valueText == null)
            valueText = FindValueText();

        if (slider == null)
            return;

        slider.minValue = MinimumValue;
        slider.maxValue = MaximumValue;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(currentValue);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        SetTitleText();
        UpdateValueText();
    }

    private void Start()
    {
        // The copied localization component can initialize after this component
        // on some scene loads, so set the title one final time after all Awakes.
        SetTitleText();
        UpdateValueText();
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    public static void SetValue(float value)
    {
        currentValue = Mathf.Clamp(value, MinimumValue, MaximumValue);
        valueLoaded = true;
        PlayerPrefs.SetFloat(PlayerPrefsKey, currentValue);
        PlayerPrefs.Save();
    }

    private void OnSliderValueChanged(float value)
    {
        SetValue(value);
        UpdateValueText();
    }

    private void UpdateValueText()
    {
        if (valueText != null)
            valueText.text = currentValue.ToString("0.00");
    }

    private void DisableCopiedSettingsComponents()
    {
        foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
        {
            if (component == null || component == this)
                continue;

            if (component.GetType().FullName == "Kamgam.UGUIComponentsForSettings.SliderUGUI" ||
                component.GetType().FullName == "Kamgam.SettingsGenerator.SliderUGUIResolver")
                component.enabled = false;
        }

        foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component != null && component.GetType().FullName == "Kamgam.LocalizationForSettings.LocalizeTMPro")
                component.enabled = false;
        }
    }

    private void SetTitleText()
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != valueText)
                text.text = "Mouse Sensitivity";
        }
    }

    private TMP_Text FindValueText()
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            if (text.text.Contains("%") || text.gameObject.name.IndexOf("value", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return text;

        return null;
    }

    private static void EnsureLoaded()
    {
        if (valueLoaded)
            return;

        currentValue = Mathf.Clamp(PlayerPrefs.GetFloat(PlayerPrefsKey, DefaultValue), MinimumValue, MaximumValue);
        valueLoaded = true;
    }
}
