using UnityEngine;
using System.Collections;

public class GameStartIntro : MonoBehaviour
{
    [Header("دوربین‌ها و آمبولانس")]
    public Camera menuCamera;
    public Camera playerCamera;
    public AmbulanceShake ambulanceShake;

    [Header("کنترل کاراکتر و منو")]
    public GameObject menuUI;
    public FloppyPlayer FloppyPlayer;

    [Header("تنظیمات پرواز نرم دوربین")]
    public bool smoothTransition = true;
    public float transitionDuration = 2.0f;

    private Rigidbody playerRb;

    void Awake()
    {
        if (FloppyPlayer != null)
        {
            playerRb = FloppyPlayer.GetComponent<Rigidbody>();
        }
    }

    void Start()
    {
        if (menuCamera != null) menuCamera.gameObject.SetActive(true);
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);

        // در صفحه Start کاراکتر سر جایش داخل آمبولانس ثابت می‌ماند و کنترل موس غیرفعال است
        if (FloppyPlayer != null)
            FloppyPlayer.enabled = false;

        if (playerRb != null)
        {
            playerRb.isKinematic = true;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStartButtonClicked()
    {
        if (menuUI != null) menuUI.SetActive(false);

        // پیشروی آمبولانس در خیابان متوقف می‌شود ولی تکان‌های کابین ادامه می‌یابند و بیشتر می‌شوند
        if (ambulanceShake != null)
            ambulanceShake.StopDrivingAndRampUpShake();

        if (smoothTransition && menuCamera != null && playerCamera != null)
        {
            StartCoroutine(FlyIntoAmbulance());
        }
        else
        {
            ActivatePlayer();
        }
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
            elapsed += Time.deltaTime;
            float rawT = Mathf.Clamp01(elapsed / duration);
            // فرمول استاندارد SmoothStep (شروع نرم، میانه سریع، توقف نرم) بدون وابستگی به منحنی Inspector
            float t = rawT * rawT * (3f - 2f * rawT);

            Vector3 targetPos = playerCamera.transform.position;
            Quaternion targetRot = playerCamera.transform.rotation;

            menuCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            menuCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            menuCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

            yield return null;
        }

        // در آخرین فریم، دوربین منو دقیقاً روی مختصات دوربین کاراکتر قفل می‌شود تا هیچ پرشی نباشد
        menuCamera.transform.position = playerCamera.transform.position;
        menuCamera.transform.rotation = playerCamera.transform.rotation;

        ActivatePlayer();
    }

    void ActivatePlayer()
    {
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(true);

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.constraints = RigidbodyConstraints.FreezeRotation;
            playerRb.linearVelocity = Vector3.zero;
        }

        if (FloppyPlayer != null)
            FloppyPlayer.enabled = true;

        // قفل کردن موس فقط پس از ورود کامل به دوربین بازیکن
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}