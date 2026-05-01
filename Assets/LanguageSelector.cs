using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

public class LanguageSelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    async void Start()
    {
        await LocalizationSettings.InitializationOperation.Task;

        dropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentIndex = 0;

        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            var locale = LocalizationSettings.AvailableLocales.Locales[i];

            options.Add(locale.Identifier.CultureInfo.NativeName);

            if (LocalizationSettings.SelectedLocale == locale)
                currentIndex = i;
        }

        dropdown.AddOptions(options);
        dropdown.value = currentIndex;
        dropdown.RefreshShownValue();

        dropdown.onValueChanged.AddListener(ChangeLanguage);
    }

    public void ChangeLanguage(int index)
    {
        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.Locales[index];

        PlayerPrefs.SetInt("LanguageIndex", index);
        PlayerPrefs.Save();
    }
}