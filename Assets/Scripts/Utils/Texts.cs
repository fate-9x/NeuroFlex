using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class Texts : MonoBehaviour
{

    private void Start() {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == "es");
    }

    public string getQuestionAddition(int cantidad)
    {

        var table = LocalizationSettings.StringDatabase.GetTable("AdditionQuest");
        if (table == null)
        {
            Debug.Log("Table not found");
            return "";
        }

        int random = Random.Range(0, table.Count);

        var localizedString = table[random.ToString()];
        if (localizedString == null)
        {
            Debug.Log("Key not found");
            return "";
        }

        return localizedString.GetLocalizedString().Replace("{cantidad}", cantidad.ToString());
    }

    public string getQuestionSubtraction(int cantidad, int posicion1, int posicion2)
    {

        var table = LocalizationSettings.StringDatabase.GetTable("SubtractionQuest");
        if (table == null)
        {
            Debug.Log("Table not found");
            return "";
        }

        int random = Random.Range(0, table.Count);

        var localizedString = table[random.ToString()];
        if (localizedString == null)
        {
            Debug.Log("Key not found");
            return "";
        }

        string question = localizedString.GetLocalizedString();

        question = question.Replace("{cantidad}", cantidad.ToString());
        question = question.Replace("{posicion1}", posicion1.ToString());
        question = question.Replace("{posicion2}", posicion2.ToString());

        return question;
    }

    public string getWelcome(string key)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("Welcome");
        if (table == null)
        {
            Debug.Log("Table not found");
            return "";
        }

        var localizedString = table[key];
        if (localizedString == null)
        {
            Debug.Log("Key not found");
            return "";
        }

        return localizedString.GetLocalizedString();
    }

    public string getTutorial(string key)
    {
        var table = LocalizationSettings.StringDatabase.GetTable("Tutorial");
        if (table == null)
        {
            Debug.Log("Table not found");
            return "";
        }

        var localizedString = table[key];
        if (localizedString == null)
        {
            Debug.Log("Key not found");
            return "";
        }

        return localizedString.GetLocalizedString();
    }

}