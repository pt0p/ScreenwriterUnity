using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class DialogApi
{
    private const string DialogJsonPathKey = "DialogJsonPath";
    private static DialogApi _instance;
    private List<Phrase> _phrases = new List<Phrase>();
    private Phrase _currentPhrase;

    private string _npcName;
    private string _heroName;
    private bool _dialogLoaded = false;
    private string _dialogId;

    private DialogApi() { }

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
            throw new InvalidOperationException($"Файл не найден: Resources/{path}.json");
        return asset.text;
    }

    public static DialogApi GetInstance()
    {
        if (_instance == null)
            _instance = new DialogApi();
        return _instance;
    }

    /// Загружает файл сцены и выбирает диалог по id.
    public void SetDialog(string sceneId, string dialogId)
    {
        string jsonText = LoadDialogJson(sceneId);
        var dialogs = JArray.Parse(jsonText);
        var dialog = dialogs.FirstOrDefault(d => d.Value<string>("id") == dialogId);
        if (dialog == null)
            throw new ArgumentException($"Диалог с id={dialogId} не найден в файле {sceneId}.json");
        _dialogId = dialogId;
        _npcName = dialog.Value<string>("npc_name") ?? "???";
        _heroName = dialog.Value<string>("hero_name") ?? "???";
        _phrases.Clear();
        var dataArray = dialog["data"] as JArray;
        if (dataArray == null)
            throw new FormatException($"В диалоге {dialogId} отсутствует поле 'data'.");
        foreach (var node in dataArray)
        {
            var phrase = new Phrase
            {
                id = node.Value<int>("id"),
                text = node.Value<string>("line"),
                itemId = node.Value<int?>("goal_achieve") ?? -1
            };
            var variantsList = new List<Variant>();
            var toArray = node["to"] as JArray;
            if (toArray != null)
            {
                foreach (var v in toArray)
                {
                    variantsList.Add(new Variant
                    {
                        id = v.Value<int>("id"),
                        line = v.Value<string>("line"),
                        info = v.Value<string>("info"),
                        nextNodeId = v.Value<int>("id")
                    });
                }
            }
            phrase.variants = variantsList.ToArray();
            _phrases.Add(phrase);
        }
        _dialogLoaded = true;
        SetPhrase(_phrases[0].id);
    }

    private void EnsureDialogLoaded()
    {
        if (!_dialogLoaded)
            throw new InvalidOperationException("Диалог не загружен. Вызовите SetDialog() перед использованием API.");
    }

    /// Возвращает текущую фразу.
    public Phrase GetPhrase()
    {
        EnsureDialogLoaded();
        return _currentPhrase;
    }

    /// Устанавливает текущую фразу по её id.
    public void SetPhrase(int id)
    {
        EnsureDialogLoaded();
        _currentPhrase = _phrases.Find(p => p.id == id)
            ?? throw new ArgumentException($"Фраза с id={id} не найдена в диалоге {_dialogId}.");
    }

    /// Применяет выбранный вариант ответа и переходит к следующей фразе.
    public string GetAndApplyVariant(int variant)
    {
        EnsureDialogLoaded();
        if (_currentPhrase == null || _currentPhrase.variants == null || variant >= _currentPhrase.variants.Length)
            throw new ArgumentOutOfRangeException(nameof(variant),
                $"Неверный индекс варианта: {variant}. Допустимо: 0..{_currentPhrase.variants.Length - 1}");
        var selected = _currentPhrase.variants[variant];
        SetPhrase(selected.nextNodeId);
        return selected.line;
    }

    /// Переходит к следующей фразе.
    public void NextPhrase()
    {
        EnsureDialogLoaded();
        if (_currentPhrase == null) return;
        int currentIndex = _phrases.FindIndex(p => p.id == _currentPhrase.id);
        if (currentIndex >= 0 && currentIndex < _phrases.Count - 1)
            SetPhrase(_phrases[currentIndex + 1].id);
    }

    /// Возвращает имя главного персонажа.
    public string GetMainCharacterName()
    {
        EnsureDialogLoaded();
        return _heroName;
    }

    /// Возвращает имя NPC.
    public string GetNpcName()
    {
        EnsureDialogLoaded();
        return _npcName;
    }

    [Serializable]
    public class Phrase
    {
        public int id;
        public string text;
        public Variant[] variants;
        public int itemId;
    }

    [Serializable]
    public class Variant
    {
        public int id;
        public string line;
        public string info;
        public int nextNodeId;
    }
}