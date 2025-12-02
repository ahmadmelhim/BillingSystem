using System.Globalization;

namespace BillingSystem.Web.Services;

public class LanguageService
{
    public event Action? OnLanguageChanged;
    
    private string _currentLanguage = "en";
    
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                CultureInfo.CurrentCulture = new CultureInfo(value);
                CultureInfo.CurrentUICulture = new CultureInfo(value);
                OnLanguageChanged?.Invoke();
            }
        }
    }

    public string this[string key] => GetText(key);
    
    public bool IsArabic => CurrentLanguage == "ar";
    
    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == "en" ? "ar" : "en";
    }
    
    public string GetText(string key)
    {
        // TODO: Implement proper localization
        // For now, return the key itself
        return key;
    }
    
    public void SetLanguage(string language)
    {
        CurrentLanguage = language;
    }
}
