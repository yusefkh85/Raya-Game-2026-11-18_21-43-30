using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameStartIntro : MonoBehaviour
{
    [Header("دوربین‌ها و آمبولانس")]
    public Camera menuCamera;          // دوربین بیرونی که آمبولانس رو نشون میده
    public Camera playerCamera;
    public AmbulanceShake ambulanceShake;

    [Header("کنترل کاراکتر و منو")]
    public GameObject menuUI;
    public FloppyPlayer FloppyPlayer;

    [Header("تنظیمات پرواز نرم دوربین")]
    public bool smoothTransition = true;
    public float transitionDuration = 2.0f;

    private Rigidbody playerRb;
    private bool started;

    // کاراکتر و وسایل موقع حرکت آمبولانس باید همراهش برن، وگرنه جا می‌مونن
    class Passenger
    {
        public Transform t;
        public Transform oldParent;
        public Rigidbody rb;
        public bool wasKinematic;
    }
    private readonly List<Passenger> passengers = new List<Passenger>();

    void Awake()
    {
        if (FloppyPlayer != null)
            playerRb = FloppyPlayer.GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (menuCamera != null) menuCamera.gameObject.SetActive(true);
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);

        // تو صفحه‌ی Start کاراکتر سر جاشه، موس آزاده و آمبولانس ثابته
        if (FloppyPlayer != null)
            FloppyPlayer.enabled = false;

        if (playerRb != null)
        {
            playerRb.isKinematic = true;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[GameStartIntro v3] آماده. menuUI=" + (menuUI != null ? menuUI.name : "NULL")
            + " | menuCamera=" + (menuCamera != null ? menuCamera.name : "NULL")
            + " | playerCamera=" + (playerCamera != null ? playerCamera.name : "NULL")
            + " | ambulanceShake=" + (ambulanceShake != null ? ambulanceShake.name : "NULL")
            + " | FloppyPlayer=" + (FloppyPlayer != null ? FloppyPlayer.name : "NULL"), this);
    }

    // به OnClick دکمه‌ی Start وصل کن
    public void OnStartButtonClicked()
    {
        if (started) return;
        started = true;
        Debug.Log("[GameStartIntro v3] دکمه‌ی Start زده شد", this);

        HideMenu();
        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        // ۱) آمبولانس راه میفته (کاراکتر و وسایل هم باهاش میرن)
        BoardPassengers();
        Debug.Log("[GameStartIntro v3] مسافرها: " + passengers.Count + " | آمبولانس راه افتاد", this);
        if (ambulanceShake != null) ambulanceShake.StartDriving();

        // ۲) دوربین بیرونی به سمت دوربین بازیکن پرواز می‌کنه
        if (menuCamera != null && playerCamera != null && smoothTransition)
            yield return StartCoroutine(FlyIntoAmbulance());
        else
            yield return new WaitForSeconds(Mathf.Max(0.1f, transitionDuration));

        Debug.Log("[GameStartIntro v3] پرواز تموم شد، سوییچ دوربین", this);
        // ۳) آمبولانس وایمیسته (تکان‌ها ادامه دارن و بیشتر میشن) و دوربین عوض میشه
        if (ambulanceShake != null) ambulanceShake.StopDrivingAndRampUpShake();
        ActivatePlayer();
    }

    // اگه این اسکریپت روی خود منو (یا زیرمجموعه‌اش) باشه، SetActive(false) کوروتین رو می‌کشت
    // و دوربین هیچ‌وقت حرکت نمی‌کرد؛ برای همین تو اون حالت فقط منو رو نامرئی می‌کنیم.
    void HideMenu()
    {
        if (menuUI == null) return;

        // محافظ: اگه Menu UI به‌اشتباه یکی از دوربین‌ها (یا والدشون) باشه، خاموشش نمی‌کنیم
        bool holdsMenuCam = menuCamera != null && menuCamera.transform.IsChildOf(menuUI.transform);
        bool holdsPlayerCam = playerCamera != null && playerCamera.transform.IsChildOf(menuUI.transform);
        if (holdsMenuCam || holdsPlayerCam)
        {
            Debug.LogError("GameStartIntro: فیلد Menu UI باید Canvas (یا پنل منو) باشه، نه دوربین! منو مخفی نشد.", this);
            return;
        }

        if (transform.IsChildOf(menuUI.transform))
        {
            CanvasGroup cg = menuUI.GetComponent<CanvasGroup>();
            if (cg == null) cg = menuUI.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        else
        {
            menuUI.SetActive(false);
        }
    }

    void BoardPassengers()
    {
        if (ambulanceShake == null) return;
        Transform amb = ambulanceShake.transform;

        if (FloppyPlayer != null)
            Board(FloppyPlayer.transform, playerRb, amb);

        MedicalTool[] tools = FindObjectsByType<MedicalTool>(FindObjectsSortMode.None);
        for (int i = 0; i < tools.Length; i++)
        {
            Rigidbody r = tools[i].GetComponentInParent<Rigidbody>();
            if (r != null && r.transform != amb && !r.transform.IsChildOf(amb))
                Board(r.transform, r, amb);
            else
                Board(tools[i].transform, null, amb);
        }
    }

    void Board(Transform t, Rigidbody r, Transform amb)
    {
        if (t == null || t == amb || t.IsChildOf(amb)) return;   // از قبل داخل آمبولانسه

        Passenger p = new Passenger();
        p.t = t;
        p.oldParent = t.parent;
        p.rb = r;
        if (r != null)
        {
            p.wasKinematic = r.isKinematic;
            r.isKinematic = true;   // موقع حرکت، وسایل سر نخورن
        }
        t.SetParent(amb, true);
        passengers.Add(p);
    }

    void ReleasePassengers()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            Passenger p = passengers[i];
            if (p.t == null) continue;
            p.t.SetParent(p.oldParent, true);
            if (p.rb != null) p.rb.isKinematic = p.wasKinematic;
        }
        passengers.Clear();
    }

    IEnumerator FlyIntoAmbulance()
    {
        Vector3 startPos = menuCamera.transform.position;
        Quaternion startRot = menuCamera.transform.rotation;
        float startFov = menuCamera.fieldOfView;
        float targetFov = playerCamera.fieldOfView;

        float duration = Mathf.Max(0.1f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;   // حتی اگه Time.timeScale صفر باشه گیر نکنه
            float rawT = Mathf.Clamp01(elapsed / duration);
            float t = rawT * rawT * (3f - 2f * rawT);   // SmoothStep

            // هدف هر فریم دوباره خونده میشه چون آمبولانس (و دوربین داخلش) در حال حرکته
            Vector3 targetPos = playerCamera.transform.position;
            Quaternion targetRot = playerCamera.transform.rotation;

            menuCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            menuCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            menuCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

            yield return null;
        }

        // آخرین فریم: دقیقا روی دوربین کاراکتر قفل میشه تا پرشی نباشه
        menuCamera.transform.position = playerCamera.transform.position;
        menuCamera.transform.rotation = playerCamera.transform.rotation;
    }

    void ActivatePlayer()
    {
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);

        ReleasePassengers();

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
            playerRb.linearVelocity = Vector3.zero;
        }

        if (FloppyPlayer != null)
            FloppyPlayer.enabled = true;   // OnEnable جهت نگاه رو از دوربین می‌گیره

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[GameStartIntro v3] ActivatePlayer: playerCamera فعاله؟ " + (playerCamera != null && playerCamera.gameObject.activeInHierarchy)
            + " | FloppyPlayer فعاله؟ " + (FloppyPlayer != null && FloppyPlayer.enabled), this);
    }
}
