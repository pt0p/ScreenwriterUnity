using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CountdownTimer : MonoBehaviour
{
    public float timeRemaining = 60f;
    [SerializeField] private Text timerText;
    [SerializeField] private GameObject timerPanel;
    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    void Update()
    {
        if (inventoryManager.items.Count == 0 || timeRemaining < 0 || SceneManager.GetActiveScene().name != "Horror")
        {
            timerPanel.SetActive(false);
            return;
        }
        timerPanel.SetActive(true);
        timeRemaining -= Time.deltaTime;
        if (timeRemaining > 0)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        if (timeRemaining > 0 && timeRemaining <= 10) timerText.color = Color.red;
    }
}
