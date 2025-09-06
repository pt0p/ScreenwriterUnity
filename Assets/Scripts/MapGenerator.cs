using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class MapGenerator : MonoBehaviour
{
    private int width = 11;
    private int height = 11;
    [SerializeField] private float cellSize = 1.5f;
    [SerializeField] private GameObject[] buildingPrefabs;
    [SerializeField] private Mesh[] posters;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject borderPrefab;
    [SerializeField] private GameObject cornerPrefab;
    [SerializeField] private BoxCollider characterGenerator;
    [SerializeField] private Camera camera;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private RectTransform maskRect;

    private bool[,] map;

    void Awake()
    {
        width = PlayerPrefs.GetInt("MapWidth", 13);
        height = PlayerPrefs.GetInt("MapHeight", 9);
        if (PlayerPrefs.GetInt("GameMode", 1) == 0) characterGenerator.size = new Vector3((width - 2) * cellSize, 2f, (height - 2) * cellSize);
    }

    void Start()
    {
        camera.orthographicSize = Mathf.Max(width, height) * 14;
        RectTransform rawRect = rawImage.rectTransform;
        float maskHeight = maskRect.rect.height;
        rawRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maskHeight * 3);
        rawRect.anchoredPosition = Vector2.zero;
        map = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                map[x, z] = false;
            }
        }

        List<Vector3Int> activeCells = new List<Vector3Int>();
        Vector3Int startCell = GetCenterCell();
        map[startCell.x, startCell.z] = true;
        activeCells.Add(startCell);

        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(2, 0, 0),
            new Vector3Int(-2, 0, 0),
            new Vector3Int(0, 0, 2),
            new Vector3Int(0, 0, -2)
        };

        while (activeCells.Count > 0)
        {
            Vector3Int currentCell = activeCells[Random.Range(0, activeCells.Count)];
            List<Vector3Int> neighbors = new List<Vector3Int>();

            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = currentCell + dir;
                if (IsInBounds(neighbor) && !map[neighbor.x, neighbor.z]) neighbors.Add(neighbor);
            }

            if (neighbors.Count > 0)
            {
                Vector3Int chosenNeighbor = neighbors[Random.Range(0, neighbors.Count)];
                Vector3Int wallBetween = (currentCell + chosenNeighbor) / 2;
                map[wallBetween.x, wallBetween.z] = true;
                map[chosenNeighbor.x, chosenNeighbor.z] = true;
                activeCells.Add(chosenNeighbor);
            }
            else activeCells.Remove(currentCell);
        }

        Vector3Int center = GetCenterCell();
        bool siriusPlaced = false;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 position = new Vector3((x - center.x) * cellSize, 0f, -(z - center.z) * cellSize);
                if (x == 0 || x == width - 1 || z == 0 || z == height - 1)
                {
                    Quaternion rotation = Quaternion.identity;
                    if (x == 0 && z == 0) Instantiate(cornerPrefab, position, Quaternion.Euler(0f, 180f, 0f), transform);
                    else if (x == 0 && z == height - 1) Instantiate(cornerPrefab, position, Quaternion.Euler(0f, 90f, 0f), transform);
                    else if (x == width - 1 && z == 0) Instantiate(cornerPrefab, position, Quaternion.Euler(0f, -90f, 0f), transform);
                    else if (x == width - 1 && z == height - 1) Instantiate(cornerPrefab, position, Quaternion.identity, transform);
                    else
                    {
                        if (x == 0) rotation = Quaternion.Euler(0f, 90f, 0f);
                        else if (x == width - 1) rotation = Quaternion.Euler(0f, -90f, 0f);
                        else if (z == 0) rotation = Quaternion.Euler(0f, 0f, 0f);
                        else if (z == height - 1) rotation = Quaternion.Euler(0f, 180f, 0f);
                        Instantiate(borderPrefab, position, rotation, transform);
                    }
                }
                else if (x == width - 2 && z == height - 2 && !siriusPlaced)
                {
                    GameObject building = Instantiate(buildingPrefabs[4], position, Quaternion.identity, transform);
                    foreach (Transform child in building.transform)
                    {
                        if (child.name.StartsWith("Build"))
                        {
                            float randomCoor = Random.Range(1f, 1.4f);
                            Vector3 originalScale = child.localScale;
                            child.localScale = new Vector3(randomCoor, randomCoor, randomCoor);
                            float randomRotationY = Random.Range(-2f, 2f) * 90f;
                            child.localRotation = Quaternion.Euler(0f, randomRotationY, 0f);
                            MeshFilter poster = child.transform.Find("Poster").GetComponent<MeshFilter>();
                            if (poster != null) poster.mesh = posters[Random.Range(0, posters.Length)];
                        }
                    }
                }
                else if (map[x, z])
                {
                    GameObject floor = Instantiate(floorPrefab, position, Quaternion.identity, transform);
                    foreach (Transform grass in floor.transform)
                    {
                        float randomX = Random.Range(-4f, 4f);
                        float randomZ = Random.Range(-8f, 8f);
                        grass.localPosition = new Vector3(randomX, 0f, randomZ);
                    }
                }
                else
                {
                    int n;
                    if (!siriusPlaced && Random.Range(0f, 1f) < 0.1f)
                    {
                        n = 4;
                        siriusPlaced = true;
                    }
                    else n = Random.Range(0, buildingPrefabs.Length - 1);
                    GameObject building = Instantiate(buildingPrefabs[n], position, Quaternion.identity, transform);
                    if (n == 3)
                    {
                        Transform tree = building.transform.Find("Street_Tree");
                        float randomY = Random.Range(3f, 9f);
                        float scaleX = 6f * randomY / 7.5f;
                        float scaleZ = scaleX * 2f;
                        tree.localScale = new Vector3(scaleX, randomY, scaleZ);
                    }
                    foreach (Transform child in building.transform)
                    {
                        if (child.name.StartsWith("Build"))
                        {
                            float randomCoor = Random.Range(1f, 1.4f);
                            Vector3 originalScale = child.localScale;
                            child.localScale = new Vector3(randomCoor, randomCoor, randomCoor);
                            float randomRotationY = Random.Range(-2f, 2f) * 90f;
                            child.localRotation = Quaternion.Euler(0f, randomRotationY, 0f);
                            if (n == 0 && Random.Range(0f, 1f) < 0.5f) child.transform.Find("Building").gameObject.SetActive(false);
                            MeshFilter poster = child.transform.Find("Poster").GetComponent<MeshFilter>();
                            if (poster != null) poster.mesh = posters[Random.Range(0, posters.Length)];
                        }
                    }
                }
            }
        }
    }

    Vector3Int GetCenterCell()
    {
        return new Vector3Int(width / 2, 0, height / 2);
    }

    bool IsInBounds(Vector3Int pos)
    {
        return pos.x > 0 && pos.x < width - 1 && pos.z > 0 && pos.z < height - 1;
    }
}