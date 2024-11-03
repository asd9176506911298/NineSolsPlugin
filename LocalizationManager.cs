using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO;
using System.Reflection;
using Newtonsoft.Json; // You may need to install Newtonsoft.Json from NuGet
using UnityEngine;

public class LocalizationManager
{
    private Dictionary<string, string> _localizationStrings;
    private string _currentLanguage;

    public LocalizationManager()
    {
        _currentLanguage = "zh-cn"; // Default language
        LoadLocalizationStrings(_currentLanguage);
    }

    public void SetLanguage(string languageCode)
    {
        _currentLanguage = languageCode;
        LoadLocalizationStrings(languageCode);
    }

    private void LoadLocalizationStrings(string languageCode)
    {
        _localizationStrings = new Dictionary<string, string>();
        string ASSEMBLY_DIR;
        #if DEBUG
            ASSEMBLY_DIR = "D:\\code\\BepInEx\\NineSolsPlugin\\language";// Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        #else
            ASSEMBLY_DIR = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        #endif
        string path = Path.Combine(ASSEMBLY_DIR, $"strings_{languageCode}.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _localizationStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }
    }

    public string GetString(string key)
    {
        if (_localizationStrings.TryGetValue(key, out string value))
        {
            return value;
        }
        return key; // Return key if the string is not found
    }
}
