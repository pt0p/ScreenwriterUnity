using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static DialogApi;

public class DialogController : MonoBehaviour
{
    public string dialogId;
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
    private Coroutine currentCoroutine;
    private ParkourEvents parkourEvents;
    private DialogApi dialogApi;

    private void Start()
    {
        dialogApi = GetInstance();
        SceneData scene = FindObjectOfType<Decoder>().scene;
        dialogApi.SetDialog(scene.sceneId, dialogId);
        parkourEvents = FindObjectOfType<ParkourEvents>();
        inventoryManager = FindObjectOfType<InventoryManager>();
        ConfigureDialogLayout();
    }

    private void ConfigureDialogLayout()
    {
        if (dialogLayout == null) return;

        DisableVerticalFit(dialogLayout);

        var dialogPanel = dialogLayout.transform.Find("DialogPanel");
        if (dialogPanel != null)
            DisableVerticalFit(dialogPanel.gameObject);

        if (dialogAnswerPanel != null)
            DisableVerticalFit(dialogAnswerPanel);
    }

    private void DisableVerticalFit(GameObject target)
    {
        var fitter = target.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private InventoryItem GetItemById(int id)
    {
        if (itemsDatabase == null || itemsDatabase.items == null) return null;
        if (id < 0 || id >= itemsDatabase.items.Count) return null;
        return itemsDatabase.items[id];
    }

    private IEnumerator ShowWindow(int phraseId, float delay)
    {
        if (!DialogOpen) yield break;
        yield return new WaitForSeconds(delay);
        if (!DialogOpen) yield break;
        foreach (Transform child in dialogAnswerPanel.transform)
            Destroy(child.gameObject);

        var phrase = dialogApi.GetPhrase();
        if (phrase == null) yield break;
        characterPortrait.sprite = characterIcon;
        characterPortrait.gameObject.SetActive(true);
        dialogLayout.SetActive(true);
        dialogName.text = dialogApi.GetNpcName();
        dialogLine.text = phrase.text;
        dialogAnswerPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        if (phrase.variants == null || phrase.variants.Length == 0)
        {
            var btn = Instantiate(dialogAnswerPrefab, dialogAnswerPanel.transform);
            btn.GetComponent<AnswersButtonController>().btnIdx = -2;
            btn.transform.GetChild(0).GetComponent<Text>().text = "Закончить диалог";
        }
        else
        {
            for (int i = 0; i < phrase.variants.Length; i++)
            {
                var variant = phrase.variants[i];
                var btn = Instantiate(dialogAnswerPrefab, dialogAnswerPanel.transform);
                btn.GetComponent<AnswersButtonController>().btnIdx = i;
                string textToShow;
                if (!string.IsNullOrEmpty(variant.info)) textToShow = variant.info;
                else
                {
                    Debug.LogWarning($"[DialogController] У варианта (id={variant.id}) отсутствует 'info'. " +
                        "Используется сокращённый вариант 'line'.");
                    textToShow = GetShortenedLine(variant.line, 6);
                }
                btn.transform.GetChild(0).GetComponent<Text>().text = textToShow;
            }
        }
    }

    public void ButtonClicked(int id)
    {
        if (!DialogOpen) return;
        foreach (Transform child in dialogAnswerPanel.transform)
            Destroy(child.gameObject);
        if (id == -2)
        {
            dialogLayout.SetActive(false);
            DialogOpen = false;
            buttonF.SetActive(false);
            buttonE.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            dialogApi.SetPhrase(1);
            return;
        }
        if (id == -3)
        {
            dialogAnswerPanel.SetActive(false);
            characterPortrait.gameObject.SetActive(true);
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(ShowWindow(dialogApi.GetPhrase().id, 0));
            return;
        }
        var currentPhrase = dialogApi.GetPhrase();
        if (currentPhrase == null) return;
        var variants = currentPhrase.variants;
        if (variants == null || id < 0 || id >= variants.Length) return;
        var response = variants[id];
        dialogLine.text = response.line;
        dialogName.text = dialogApi.GetMainCharacterName();
        dialogApi.GetAndApplyVariant(id);
        var nextPhrase = dialogApi.GetPhrase();
        if (nextPhrase.itemId != -1)
        {
            InventoryItem item = GetItemById(nextPhrase.itemId);
            if (item != null && !inventoryManager.HasItem(item.name))
            {
                if (parkourEvents != null)
                    parkourEvents.ShowPart2();
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
            foreach (Transform child in dialogAnswerPanel.transform)
                Destroy(child.gameObject);
            return;
        }
        if (Input.GetKey(KeyCode.E) && !DialogOpen)
        {
            DialogOpen = true;
            buttonE.SetActive(false);
            buttonF.SetActive(true);
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(ShowWindow(dialogApi.GetPhrase().id, 0));
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

    private string GetShortenedLine(string line, int wordLimit = 6)
    {
        if (string.IsNullOrWhiteSpace(line))
            return "...";

        var words = line.Split(' ');
        if (words.Length <= wordLimit)
            return line.Trim();

        return string.Join(" ", words, 0, wordLimit) + "...";
    }
}
