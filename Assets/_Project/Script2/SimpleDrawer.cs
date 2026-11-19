using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SimpleDrawer : MonoBehaviour
{
    [Header("تنظیمات حرکت کشو")]
    [Tooltip("جهت و میزان باز شدن کشو (در آمبولانس شما محور X مثبت یا منفی است)")]
    public Vector3 openOffset = new Vector3(0.35f, 0f, 0f);
    public float slideSpeed = 7f;

    [Header("تنظیمات تعامل با کلید E")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;
    public string openPromptText = "[E] Open Drawer";
    public string closePromptText = "[E] Close Drawer";

    private Vector3 closedLocalPos;
    private bool isOpen = false;
    private bool isPlayerLooking = false;
    private Camera playerCam;
    private float lastToggleTime = 0f;

    void Start()
    {
        closedLocalPos = transform.localPosition;
        playerCam = Camera.main;
    }

    void Update()
    {
        // ۱. حرکت نرم کشو به سمت حالت باز یا بسته
        Vector3 targetPos = isOpen ? closedLocalPos + openOffset : closedLocalPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, slideSpeed * Time.deltaTime);

        // ۲. بررسی اینکه آیا دوربین بازیکن روبه‌روی همین کشوست یا نه
        CheckPlayerLook();

        // ۳. باز و بسته کردن با کلید E وقتی بازیکن به کشو نگاه می‌کند
        if (isPlayerLooking && Input.GetKeyDown(interactKey))
        {
            ToggleDrawer();
        }
    }

    void CheckPlayerLook()
    {
        isPlayerLooking = false;

        if (playerCam == null)
        {
            playerCam = Camera.main;
            if (playerCam == null) return;
        }

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            // اگر اشعه دوربین به خود این کشو یا زیرمجموعه‌هایش خورد
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                isPlayerLooking = true;
            }
        }
    }

    public void ToggleDrawer()
    {
        if (Time.time - lastToggleTime < 0.35f) return;
        lastToggleTime = Time.time;
        isOpen = !isOpen;
    }

    // اگر دست‌های آبی پرستار هم به کشو برخورد کنند، کشو واکنش نشان می‌دهد
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Forearm") || collision.gameObject.name.Contains("UpperArm"))
        {
            ToggleDrawer();
        }
    }

    // نمایش خودکار پیغام [E] روی صفحه بدون نیاز به ساخت UI دستی
    void OnGUI()
    {
        if (!isPlayerLooking) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        string prompt = isOpen ? closePromptText : openPromptText;
        float width = 200f;
        float height = 38f;
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height / 2f) + 35f; // دقیقاً کمی پایین‌تر از نشانگر وسط صفحه

        GUI.Box(new Rect(x, y, width, height), prompt, style);
    }
}