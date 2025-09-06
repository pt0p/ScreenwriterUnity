using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject dialogLayout;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject screamer;

    void Start()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.volume = PlayerPrefs.GetFloat("Volume", 0.5f);
        }
    }

    void Update()
    {
        if (gameOverPanel == null || !gameOverPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !inventoryPanel.activeSelf && !mapPanel.activeSelf && !screamer.activeSelf) Pause(pausePanel);
            else if (Input.GetKeyDown(KeyCode.I) && !pausePanel.activeSelf && !mapPanel.activeSelf && !screamer.activeSelf) Pause(inventoryPanel);
            else if (Input.GetKeyDown(KeyCode.M) && !pausePanel.activeSelf && !inventoryPanel.activeSelf && PlayerPrefs.GetInt("GameMode", 1) == 0 && !screamer.activeSelf) Pause(mapPanel);
        }
    }

    public void Pause(GameObject panel)
    {
        Cursor.lockState = panel.activeSelf ? CursorLockMode.Locked : CursorLockMode.None;
        Time.timeScale = panel.activeSelf ? 1 : 0;
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        float volume = PlayerPrefs.GetFloat("Volume", 0.5f);
        foreach (AudioSource audioSource in audioSources)
        {
            audioSource.volume = panel.activeSelf ? volume : volume / 2;
        }
        panel.SetActive(!panel.activeSelf);
        if (dialogLayout.activeSelf) Cursor.lockState = CursorLockMode.None;
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
        Time.timeScale = 1;
    }

    public void SwitchMode()
    {
        if (PlayerPrefs.GetInt("GameMode", 1) == 0) LoadScene(2);
        else LoadScene(3);
    }
}