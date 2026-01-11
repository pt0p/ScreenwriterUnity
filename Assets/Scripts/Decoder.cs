using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using static DialogApi;

public class Decoder : MonoBehaviour
{
    private const string DialogJsonPathKey = "DialogJsonPath";
    [Tooltip("Имя JSON-файла (без .json) из папки Resources/dialogues.")]
    public string sceneId;
    public SceneData scene { get; private set; }

    private void Awake()
    {
        string jsonText = LoadDialogJson(sceneId);
        if (string.IsNullOrEmpty(jsonText))
            return;
        try
        {
            var dialogs = JsonConvert.DeserializeObject<List<DialogData>>(jsonText);
            scene = new SceneData
            {
                sceneId = sceneId,
                dialogs = dialogs
            };
            Debug.Log($"Decoder: успешно загружено {dialogs.Count} диалог(ов) из {sceneId}.json");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Decoder: ошибка парсинга JSON — {ex.Message}");
        }
    }

    private static string LoadDialogJson(string sceneId)
    {
        string customPath = PlayerPrefs.GetString(DialogJsonPathKey, string.Empty);
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
        {
            return File.ReadAllText(customPath);
        }

        string path = $"dialogues/{sceneId}";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            Debug.LogError($"Decoder: не найден файл Resources/{path}.json");
            return null;
        }

        return asset.text;
    }
}

[Serializable]
public class SceneData
{
    public string sceneId;
    public List<DialogData> dialogs;
}

[Serializable]
public class DialogData
{
    public string id;
    public string npc_name;
    public string hero_name;
    public List<Phrase> data;
}