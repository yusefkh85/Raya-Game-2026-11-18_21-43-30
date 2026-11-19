using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(PatientHealth), typeof(PatientStateManager))]
public class PatientNeedsManager : MonoBehaviour
{
    [Header("اتصال به UI پیامک")]
    public TextMeshProUGUI smsText; // متن پیامک روی صفحه

    [Header("تنظیمات زمان")]
    public float timeToFulfillNeed = 10f; // چند ثانیه وقت داره تا نیازش رو برطرف کنی
    public float wrongItemDamage = 15f;   // چقدر جون کم میشه اگه آیتم اشتباه بدی
    public float correctItemHeal = 10f;   // چقدر جون زیاد میشه اگه آیتم درست بدی

    // لیست نیازها (این‌ها رو خودت توی کد یا اینسپکتور تنظیم میکنی)
    // نکته: هر نیاز با آیتم هم‌ایندکسش جواب داده میشه؛ طول دو آرایه باید برابر باشه
    public string[] possibleNeeds = { "I have a fever,", "I am in pain,", "I am bleeding,", "I have gone into cardiac arrest." };
    public string[] correctItems = { "Firefighting", "Pill", "Adhesive bandage", "Defibrillator" };

    private int currentNeedIndex = -1;
    private float timer;
    private bool hasActiveNeed = false;

    private PatientHealth patientHealth;
    private PatientStateManager stateManager;

    void Start()
    {
        patientHealth = GetComponent<PatientHealth>();
        stateManager = GetComponent<PatientStateManager>();

        if (smsText != null) smsText.gameObject.SetActive(false);   // قبلا از اول روشن بود

        if (possibleNeeds.Length == 0 || correctItems.Length < possibleNeeds.Length)
            Debug.LogWarning("PatientNeedsManager: طول possibleNeeds و correctItems برابر نیست؛ فقط نیازهایی که آیتم دارن استفاده میشن.", this);

        StartCoroutine(NeedLoop());
    }

    IEnumerator NeedLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f)); // فاصله بین نیازها

            // تا نیاز قبلی تموم نشده نیاز جدید نمیاد (قبلا نیاز جدید بی‌صدا جای قبلی رو می‌گرفت)
            while (hasActiveNeed) yield return null;

            if (stateManager.CurrentState == PatientState.Dead) yield break;
            GenerateNewNeed();
        }
    }

    void GenerateNewNeed()
    {
        int count = Mathf.Min(possibleNeeds.Length, correctItems.Length);
        if (count == 0) return;

        currentNeedIndex = Random.Range(0, count);
        if (smsText != null)
        {
            smsText.text = "📱 پیام از بیمار: " + possibleNeeds[currentNeedIndex];
            smsText.gameObject.SetActive(true);
        }

        hasActiveNeed = true;
        timer = timeToFulfillNeed;
        stateManager.SetState(PatientState.Angry); // وقتی نیاز داره عصبانی میشه
    }

    void Update()
    {
        if (!hasActiveNeed) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // زمان تموم شد و بازیکن کاری نکرد
            patientHealth.ChangeHealth(-wrongItemDamage);
            EndNeed();
        }
    }

    // این متد توسط MedicalTool.Use() صدا زده میشه
    public void UseItem(string itemName)
    {
        if (!hasActiveNeed) return;

        if (Normalize(itemName) == Normalize(correctItems[currentNeedIndex]))
        {
            patientHealth.ChangeHealth(correctItemHeal);   // آیتم درست! جونش زیاد میشه
        }
        else
        {
            patientHealth.ChangeHealth(-wrongItemDamage);  // آیتم اشتباه!
        }
        EndNeed();
    }

    static string Normalize(string s)
    {
        return s == null ? "" : s.Trim().ToLowerInvariant();
    }

    void EndNeed()
    {
        hasActiveNeed = false;
        if (smsText != null) smsText.gameObject.SetActive(false);

        if (stateManager.CurrentState != PatientState.Dead)
        {
            if (patientHealth.health < 40) stateManager.SetState(PatientState.Sick);
            else stateManager.SetState(PatientState.Happy);
        }
    }
}
