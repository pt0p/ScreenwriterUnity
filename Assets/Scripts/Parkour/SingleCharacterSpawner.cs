using UnityEngine;
using UnityEngine.UI;
public class SingleCharacterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Sprite[] characterIcons;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject dialogLayout;
    [SerializeField] private GameObject dialogAnswerPanel;
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Text dialogName;
    [SerializeField] private Text dialogLine;
    [SerializeField] private GameObject dialogAnswerPrefab;
    [SerializeField] private GameObject buttonE;
    [SerializeField] private GameObject buttonF;
    [SerializeField] private int sceneId;
    [SerializeField] private bool onStart = true;
    [SerializeField] private ItemsDatabase itemsDatabase;

    private void Start()
    {
        if (onStart) Spawn();
    }

    public void Spawn()
    {
        //if (characterPrefabs.Length == 0 || spawnPoint == null) return;
        //int prefabIndex = Random.Range(0, characterPrefabs.Length);
        //GameObject npc = Instantiate(characterPrefabs[prefabIndex], spawnPoint.position, Quaternion.identity);
        //DialogController dc = npc.GetComponent<DialogController>();
        //dc.sceneId = sceneId;
        //dc.dialogLayout = dialogLayout;
        //dc.dialogAnswerPanel = dialogAnswerPanel;
        //dc.dialogLine = dialogLine;
        //dc.dialogAnswerPrefab = dialogAnswerPrefab;
        //dc.dialogName = dialogName;
        //dc.characterPortrait = characterPortrait;
        //dc.characterIcon = characterIcons[prefabIndex];
        //dc.buttonE = buttonE;
        //dc.buttonF = buttonF;
        //dc.itemsDatabase = itemsDatabase;
    }
}