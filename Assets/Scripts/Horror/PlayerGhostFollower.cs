using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static PlayerGhostRecorder;

public class PlayerGhostFollower : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float delay = 3f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float reappearDelay = 5f;
    [SerializeField] private float disappearInterval = 7f;

    private PlayerGhostRecorder recorder;
    private Queue<GhostFrame> replayQueue = new Queue<GhostFrame>();
    private Material ghostMaterial;
    private Renderer[] renderers;
    private GhostScreamer ghostScreamer;
    private InventoryManager inventoryManager;
    private LanternController lanternController;

    void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        lanternController = FindObjectOfType<LanternController>();
        ghostScreamer = GetComponent<GhostScreamer>();
        if (player == null)
        {
            enabled = false;
            return;
        }
        recorder = player.GetComponent<PlayerGhostRecorder>();
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0) ghostMaterial = renderers[0].material;
        StartCoroutine(ReplayMovement());
        StartCoroutine(AppearanceCycle());
    }

    IEnumerator ReplayMovement()
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            replayQueue = recorder.GetRecordedFrames();
        }
    }

    void Update()
    {
        if (inventoryManager.items.Count == 0 || lanternController.isLit)
        {
            Disappear();
            return;
        }
        if (replayQueue.Count > 0)
        {
            GhostFrame frame = replayQueue.Dequeue();
            transform.position = Vector3.Lerp(transform.position, frame.position, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, frame.rotation, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator AppearanceCycle()
    {
        while (true)
        {
            yield return new WaitForSeconds(disappearInterval);
            Disappear();
            yield return new WaitForSeconds(reappearDelay);
            if (inventoryManager.items.Count != 0 && !lanternController.isLit) ghostScreamer.SetGhostActive(true);
            else ghostScreamer.SetGhostActive(false);
        }
    }

    void Disappear()
    {
        if (ghostMaterial != null)
        {
            ghostMaterial.SetFloat("_StartTime", Time.time);
            ghostMaterial.SetFloat("_Delay", reappearDelay);
            ghostScreamer.SetGhostActive(false);
        }
    }
}