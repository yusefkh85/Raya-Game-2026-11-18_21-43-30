using UnityEngine;
using System.Collections;
using TMPro;

public class PatientNeedsManager : MonoBehaviour
{
    [Header("اتصال به UI پیامک")]
    public TextMeshProUGUI smsText; // متن پیامک روی صفحه

    [Header("تنظیمات زمان")]
    public float timeToFulfillNeed = 10f; // چند ثانیه وقت داره تا نیازش رو برطرف کنی
    public float wrongItemDamage = 15f;   // چقدر جون کم میشه اگه آیتم اشتباه بدی

    // لیست نیازها (این‌ها رو خودت توی کد یا اینسپکتور تنظیم میکنی)
    public string[] possibleNeeds = { "تب دارم", "درد دارم", "خونریزی دارم", "ایست قلبی کردم" };
    public string[] correctItems = { "آتش‌نشانی", "قرص", "چسب زخم", "دستگاه شوک" };

    private int currentNeedIndex = -1;
    private float timer;
    private bool hasActiveNeed = false;
    
    private PatientHealth patientHealth;
    private PatientStateManager stateManager;

    void Start()
    {
        patientHealth = GetComponent<PatientHealth>();
        stateManager = GetComponent<PatientStateManager>();
        
        StartCoroutine(NeedLoop());
    }

    IEnumerator NeedLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f)); // فاصله بین نیازها
            
            if (stateManager.CurrentState != PatientState.Dead)
            {
                GenerateNewNeed();
            }
        }
    }

    void GenerateNewNeed()
    {
        currentNeedIndex = Random.Range(0, possibleNeeds.Length);
        smsText.text = "📱 پیام از بیمار: " + possibleNeeds[currentNeedIndex];
        smsText.gameObject.SetActive(true);
        
        hasActiveNeed = true;
        timer = timeToFulfillNeed;
        stateManager.SetState(PatientState.Angry); // وقتی نیاز داره عصبانی میشه
    }

    void Update()
    {
        if (hasActiveNeed)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                // زمان تموم شد و بازیکن کاری نکرد
                patientHealth.ChangeHealth(-wrongItemDamage);
                EndNeed();
            }
        }
    }

    // این متد توسط آیتم‌های سه بعدی صدا زده میشه
    public void UseItem(string itemName)
    {
        if (!hasActiveNeed) return;

        if (itemName == correctItems[currentNeedIndex])
        {
            // آیتم درست!
            patientHealth.ChangeHealth(10f); // جونش زیاد میشه
            EndNeed();
        }
        else
        {
            // آیتم اشتباه!
            patientHealth.ChangeHealth(-wrongItemDamage);
            EndNeed();
        }
    }

    void EndNeed()
    {
        hasActiveNeed = false;
        smsText.gameObject.SetActive(false);
        
        if (stateManager.CurrentState != PatientState.Dead)
        {
            if (patientHealth.health < 40) stateManager.SetState(PatientState.Sick);
            else stateManager.SetState(PatientState.Happy);
        }
    }
}