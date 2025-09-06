using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float runSpeedMultiplier = 1.5f;
    [SerializeField] private float sensitivityX = 10f;
    [SerializeField] private float sensitivityY = 10f;
    [SerializeField] private float cameraLimit = 60f;
    [SerializeField] private GameObject dialogLayout;
    [SerializeField] private GameObject screamer;
    [SerializeField] private GameObject ghost;
    [SerializeField] private Camera cam;
    private Vector3 externalPlatformMotion = Vector3.zero;
    private Vector3 slidingForce = Vector3.zero;
    private InventoryManager inventoryManager;
    private CountdownTimer countdownTimer;
    private float _angle;
    private float _verticalVelocity;
    private Vector3 siriusPosition;
    private CharacterController _cc;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _cc = GetComponent<CharacterController>();
        inventoryManager = FindObjectOfType<InventoryManager>();
        countdownTimer = FindObjectOfType<CountdownTimer>();
    }

    private void Update()
    {
        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift)) currentSpeed *= runSpeedMultiplier;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 forward = transform.forward * vertical;
        Vector3 right = transform.right * horizontal;
        Vector3 moveDirection = Vector3.Normalize(forward + right) * currentSpeed;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            _angle += Input.GetAxis("Mouse Y") * -sensitivityY * Time.deltaTime;
            _angle = Mathf.Clamp(_angle, -cameraLimit, cameraLimit);
            cam.transform.eulerAngles = new Vector3(_angle, cam.transform.eulerAngles.y, cam.transform.eulerAngles.z);
            transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X") * sensitivityX, 0));
        }

        if (_cc.isGrounded)
        {
            _verticalVelocity = -1f;
            if (Input.GetKeyDown(KeyCode.Space)) _verticalVelocity = jumpForce;
        }
        else _verticalVelocity -= gravity * Time.deltaTime;

        if (!dialogLayout.activeSelf && !screamer.activeSelf)
        {
            Vector3 finalMove = moveDirection + slidingForce;
            finalMove.y = _verticalVelocity;
            finalMove += externalPlatformMotion / Time.deltaTime;
            _cc.Move(finalMove * Time.deltaTime);
            externalPlatformMotion = Vector3.zero;
            slidingForce = Vector3.zero;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("LibEnter"))
        {
            siriusPosition = other.transform.position + new Vector3(0f, 0f, 3f);
            if (inventoryManager.items.Count != 0 && (countdownTimer.timeRemaining <= 0 || PlayerPrefs.GetInt("GameMode", 1) == 0))
            {
                if (ghost != null) ghost.SetActive(false);
                Teleport(new Vector3(-503f, -499f, -499f));
                other.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
        }
        if (other.CompareTag("LibExit"))
        {
            if (ghost != null) ghost.SetActive(true);
            Teleport(siriusPosition);
        }
    }


    void Teleport(Vector3 position)
    {
        if (_cc != null) _cc.enabled = false;
        gameObject.transform.position = position;
        if (_cc != null) _cc.enabled = true;
    }

    public void ApplyPlatformMotion(Vector3 delta)
    {
        externalPlatformMotion += new Vector3(delta.x, 0f, delta.z);
    }

    public void ApplySlide(Vector3 force)
    {
        slidingForce = force;
    }
}