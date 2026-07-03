using UnityEngine;
using UnityEngine.Localization.Settings;
using DamageNumbersPro;

public class DamageNumberLanguageRefresh : MonoBehaviour
{
    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshAll;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshAll;
    }

    void RefreshAll(UnityEngine.Localization.Locale locale)
    {
        foreach (DamageNumber dn in DamageNumber.activeInstances)
        {
            if (dn != null)
            {
                dn.UpdateText();
            }
        }
    }
}