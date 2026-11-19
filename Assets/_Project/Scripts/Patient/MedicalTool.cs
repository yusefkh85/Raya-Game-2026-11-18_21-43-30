using UnityEngine;

public class MedicalTool : MonoBehaviour
{
    public string itemName; // مثلا بنویس: "آتش‌نشانی" یا "قرص" یا "دستگاه شوک"
    private PatientNeedsManager patientNeeds;

    void Start()
    {
        // پیدا کردن بیمار توی صحنه
        patientNeeds = FindObjectOfType<PatientNeedsManager>();
    }

    // وقتی با موس روی دستگاه کلیک میشه
    void OnMouseDown()
    {
        if (patientNeeds != null)
        {
            patientNeeds.UseItem(itemName);
            // اینجا میتونی صدای کلیک یا انیمیشن استفاده از دستگاه رو هم اجرا کنی
        }
    }
}