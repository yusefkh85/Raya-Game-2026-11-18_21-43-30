using UnityEngine;

public class MedicalTool : MonoBehaviour
{
    public string itemName;              // مثلا: "Pill" یا "Defibrillator" (باید با correctItems تو PatientNeedsManager یکی باشه)
    public float useCooldown = 0.5f;     // جلوگیری از اسپم کردن دکمه
    public bool canBePickedUp = true;    // false = دستگاه ثابت (با E روش کار میکنه ولی برداشته نمیشه)
    public bool consumeOnUse = false;    // true = بعد از دادن به بیمار از بین میره (مثلا قرص)

    private PatientNeedsManager patientNeeds;
    private float nextUseTime;

    void Start()
    {
        patientNeeds = FindFirstObjectByType<PatientNeedsManager>();
    }

    // توسط FloppyPlayer با دکمه E صدا زده میشه (وسیله‌ی تو دست یا وسیله‌ای که بهش نگاه می‌کنی).
    // قبلا OnMouseDown بود؛ با موس قفل‌شده‌ی اول‌شخص درست کار نمی‌کرد، برد نداشت
    // و همزمان با کلیکِ برداشتن اجرا می‌شد.
    public bool Use()
    {
        if (Time.time < nextUseTime) return false;

        if (patientNeeds == null)
            patientNeeds = FindFirstObjectByType<PatientNeedsManager>();
        if (patientNeeds == null)
        {
            Debug.LogWarning("MedicalTool: no PatientNeedsManager found in the scene.", this);
            return false;
        }

        nextUseTime = Time.time + useCooldown;
        bool accepted = patientNeeds.UseItem(itemName);   // false = بیمار الان نیازی نداره
        // اینجا میتونی صدا یا انیمیشن استفاده از دستگاه رو هم اجرا کنی
        return accepted;
    }
}
