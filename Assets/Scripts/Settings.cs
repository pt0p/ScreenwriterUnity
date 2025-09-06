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

    private void Start()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 0.5f);
        if (File.Exists(Application.dataPath + "/scene.json"))
            dialogField.text = File.ReadAllText(Application.dataPath + "/scene.json");
        else
            File.Create(Application.dataPath + "/scene.json").Close();
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
        File.WriteAllText(Application.dataPath + "/scene.json", dialogField.text);
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
        }
    }
}