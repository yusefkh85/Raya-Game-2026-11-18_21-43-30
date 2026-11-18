using UnityEngine;

// بدن پرستار ثابت است. فقط سر (با موس) می‌چرخد و پرستار می‌تواند روی ریل
// داخل کابین به چپ و راست لغزش کند تا به تمام اتاق دسترسی داشته باشد.
// اسکریپت روی ریشه پرستار (Body) قرار می‌گیرد. تمام اشیاء باید فرزند Cabin باشند.
public class NurseController : MonoBehaviour
{
    [Header("References")]
    public Transform body;          // بدن ثابت پرستار (فرزند کابین)
    public Transform head;          // مفصل سر (فرزند body)
    public Camera playerCamera;     // دوربین (فرزند head)

    [Header("Rail (لغزش چپ و راست)")]
    public Transform railStart;     // نقطه انتهای چپ کابین (فرزند کابین)
    public Transform railEnd;       // نقطه انتهای راست کابین (فرزند کابین)
    public float slideSpeed = 1.2f; // سرعت لغزش روی ریل (متر بر ثانیه)
    public bool allowSlide = true;

    [Header("Head look")]
    public float mouseSensitivity = 2f;
    public float maxYaw = 85f;
    public float minPitch = -45f;
    public float maxPitch = 60f;

    [Header("Camera shake (از AmbulanceShake)")]
    public float cameraShakeAmount = 0.04f;

    float yaw;
    float pitch;
    float railT = 0.5f;          // 0 = چپ، 1 = راست
    Vector3 camBaseLocalPos;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerCamera != null)
            camBaseLocalPos = playerCamera.transform.localPosition;
        ApplyRailPosition();
    }

    void Update()
    {
        // Esc برای آزاد کردن موس
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(1) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleHeadLook();
        HandleRailSlide();
        HandleCameraShake();
    }

    void HandleHeadLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw = Mathf.Clamp(yaw + mx, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch - my, minPitch, maxPitch);

        head.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleRailSlide()
    {
        if (!allowSlide) return;

        float input = Input.GetAxisRaw("Horizontal"); // A/D یا فلش‌ها
        float railLength = Vector3.Distance(railStart.localPosition, railEnd.localPosition);
        if (railLength < 0.001f) return;

        railT = Mathf.Clamp01(railT + input * slideSpeed * Time.deltaTime / railLength);
        ApplyRailPosition();
    }

    void ApplyRailPosition()
    {
        body.localPosition = Vector3.Lerp(railStart.localPosition, railEnd.localPosition, railT);
    }

    void HandleCameraShake()
    {
        if (playerCamera == null) return;
        float intensity = AmbulanceShake.Intensity;
        playerCamera.transform.localPosition =
            camBaseLocalPos + Random.insideUnitSphere * (cameraShakeAmount * intensity);
    }
}
