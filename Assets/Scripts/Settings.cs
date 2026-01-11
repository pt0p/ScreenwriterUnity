using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.UI;
public class Settings : MonoBehaviour
{
    [SerializeField] private InputField dialogField;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private InputField widthField;
    [SerializeField] private InputField heightField;
    [SerializeField] private Dropdown mode;
    [SerializeField] private GameObject mapParams;
    private const string DialogJsonPathKey = "DialogJsonPath";
    private string SceneJsonPath => Path.Combine(Application.persistentDataPath, "scene.json");

    private void Start()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 0.5f);
        var storedPath = PlayerPrefs.GetString(DialogJsonPathKey, string.Empty);
        var dialogPath = !string.IsNullOrEmpty(storedPath) && File.Exists(storedPath)
            ? storedPath
            : SceneJsonPath;
        if (File.Exists(dialogPath))
            dialogField.text = File.ReadAllText(dialogPath);
        else
            File.Create(dialogPath).Close();
        widthField.text = PlayerPrefs.GetInt("MapWidth", 13) + "";
        heightField.text = PlayerPrefs.GetInt("MapHeight", 9) + "";
        mode.value = PlayerPrefs.GetInt("GameMode", 1);
    }

    private void Update()
    {
        if (mode.value == 0) mapParams.SetActive(true);
        else mapParams.SetActive(false);
    }

    public void Save()
    {
        File.WriteAllText(SceneJsonPath, dialogField.text);
        PlayerPrefs.SetString(DialogJsonPathKey, SceneJsonPath);
        PlayerPrefs.SetInt("MapWidth", int.Parse(widthField.text));
        PlayerPrefs.SetInt("MapHeight", int.Parse(heightField.text));
        PlayerPrefs.SetInt("GameMode", mode.value);
    }

    public void OnSoundChange(float value)
    {
        PlayerPrefs.SetFloat("Volume", value);
    }

    public void Load()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Выберите файл", "", "json", false);
        if (paths.Length > 0 && File.Exists(paths[0]))
        {
            dialogField.text = File.ReadAllText(paths[0]);
            PlayerPrefs.SetString(DialogJsonPathKey, paths[0]);
        }
    }
}
