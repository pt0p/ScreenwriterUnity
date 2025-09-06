using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using AYellowpaper.SerializedCollections;

public class DialogController : MonoBehaviour
{
    public int sceneId;
    public bool DialogOpen = false;

    public GameObject dialogLayout;
    public GameObject dialogAnswerPanel;
    public Image characterPortrait;
    public Text dialogName;
    public Text dialogLine;
    public Sprite characterIcon;
    public GameObject dialogAnswerPrefab;
    public GameObject buttonE;
    public GameObject buttonF;
    public ItemsDatabase itemsDatabase;

    private InventoryManager inventoryManager;
    private List<MyScene> _scenes;
    private DialogueNode currentNode;
    private Coroutine currentCoroutine;
    private ParkourEvents parkourEvents;

    private void Start()
    {
        _scenes = FindObjectOfType<Decoder>().MyScenes.scene;
        currentNode = _scenes[sceneId].data[0];
        parkourEvents = FindObjectOfType<ParkourEvents>();
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    private InventoryItem GetItemById(int id)
    {
        if (itemsDatabase == null || itemsDatabase.items == null) return null;
        if (id < 0 || id >= itemsDatabase.items.Count) return null;
        return itemsDatabase.items[id];
    }

    private IEnumerator ShowWindow(int id, float delay)
    {
        if (!DialogOpen) yield break;
        yield return new WaitForSeconds(delay);
        if (!DialogOpen) yield break;
        foreach (Transform child in dialogAnswerPanel.transform) Destroy(child.gameObject);

        var scene = _scenes[sceneId];
        currentNode = scene.data.Find(n => n.id == id);

        characterPortrait.sprite = characterIcon;
        characterPortrait.gameObject.SetActive(true);
        dialogLayout.SetActive(true);
        dialogName.text = scene.npc_name;
        dialogLine.text = currentNode.line;

        dialogAnswerPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;

        if (currentNode.to == null || currentNode.to.Count == 0)
        {
            var btn = Instantiate(dialogAnswerPrefab, dialogAnswerPanel.transform);
            btn.GetComponent<AnswersButtonController>().btnIdx = -2;
            btn.transform.GetChild(0).GetComponent<Text>().text = "Закончить диалог";
        }
        else
        {
            for (int i = 0; i < currentNode.to.Count; i++)
            {
                var btn = Instantiate(dialogAnswerPrefab, dialogAnswerPanel.transform);
                btn.GetComponent<AnswersButtonController>().btnIdx = i;
                btn.transform.GetChild(0).GetComponent<Text>().text = currentNode.to[i].info;
            }
        }
    }

    public void ButtonClicked(int id)
    {
        if (!DialogOpen) return;
        foreach (Transform child in dialogAnswerPanel.transform) Destroy(child.gameObject);
        if (id == -2)
        {
            dialogLayout.SetActive(false);
            DialogOpen = false;
            buttonF.SetActive(false);
            buttonE.SetActive(true);
            currentNode = _scenes[sceneId].data[0];
            Cursor.lockState = CursorLockMode.Locked;
            return;
        }

        if (id == -3)
        {
            dialogAnswerPanel.SetActive(false);
            characterPortrait.gameObject.SetActive(true);
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(ShowWindow(currentNode.id, 0));
            return;
        }

        var response = currentNode.to[id];
        dialogLine.text = response.line;
        dialogName.text = _scenes[sceneId].hero_name;

        currentNode = _scenes[sceneId].data.Find(n => n.id == response.id);
        GoalAchieved goalAchieved = currentNode.goal_achieved;
        if (goalAchieved.item != -1)
        {
            InventoryItem item = GetItemById(goalAchieved.item);
            if (item != null && !inventoryManager.HasItem(item.name))
            {
                if (parkourEvents != null) parkourEvents.ShowPart2();
                else
                {
                    inventoryManager.AddItem(item);
                    inventoryManager.UpdateInventory();
                    if (SceneManager.GetActiveScene().name == "Horror")
                    {
                        buttonF.SetActive(false);
                        dialogLayout.SetActive(false);
                        Cursor.lockState = CursorLockMode.Locked;
                        if (currentCoroutine != null)
                        {
                            StopCoroutine(currentCoroutine);
                            currentCoroutine = null;
                        }
                        Destroy(gameObject);
                    }
                }
            }
        }

        var btnNext = Instantiate(dialogAnswerPrefab, dialogAnswerPanel.transform);
        btnNext.GetComponent<AnswersButtonController>().btnIdx = -3;
        btnNext.transform.GetChild(0).GetComponent<Text>().text = "Продолжить диалог";
        dialogAnswerPanel.SetActive(true);
        dialogAnswerPanel.SetActive(true);
        characterPortrait.gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Input.GetKey(KeyCode.F) && dialogLayout.activeSelf)
        {
            buttonF.SetActive(false);
            dialogLayout.SetActive(false);
            buttonE.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            DialogOpen = false;
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }
            foreach (Transform child in dialogAnswerPanel.transform) Destroy(child.gameObject);
            return;
        }
        if (Input.GetKey(KeyCode.E) && !DialogOpen)
        {
            DialogOpen = true;
            buttonE.SetActive(false);
            buttonF.SetActive(true);
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(ShowWindow(currentNode.id, 0));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !dialogLayout.activeSelf) buttonE.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) buttonE.SetActive(false);
    }
}