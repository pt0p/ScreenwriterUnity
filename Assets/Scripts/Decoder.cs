using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using static DialogApi;

public class Decoder : MonoBehaviour
{
    [Tooltip("»м€ JSON-файла (без .json) из папки Resources/dialogues.")]
    public string sceneId;
    public SceneData scene { get; private set; }

    private void Awake()
    {
        string path = $"dialogues/{sceneId}";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            Debug.LogError($"Decoder: не найден файл Resources/{path}.json");
            return;
        }
        try
        {
            var dialogs = JsonConvert.DeserializeObject<List<DialogData>>(asset.text);
            scene = new SceneData
            {
                sceneId = sceneId,
                dialogs = dialogs
            };
            Debug.Log($"Decoder: успешно загружено {dialogs.Count} диалог(ов) из {sceneId}.json");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Decoder: ошибка парсинга JSON Ч {ex.Message}");
        }
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