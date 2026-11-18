using UnityEngine;
using UnityEngine.Events;

// ناحیه درمان روی بدن بیمار. هر ناحیه یک نوع وسیله می‌خواهد:
// - سرنگ: لمس بدن بیمار
// - پد شوک: نگه داشتن روی قفسه سینه به مدت defibHoldTime
// - باند: لمس محل زخم
// - کیسه خون: نزدیک دست یا پایه
// Collider این ناحیه باید Is Trigger باشد. onTreated را به PatientManager وصل کنید.
[RequireComponent(typeof(Collider))]
public class TreatmentZone : MonoBehaviour
{
    public MedicalItemType requiredItem;
    public float defibHoldTime = 1.5f;
    public float cooldown = 0.5f;

    public UnityEvent<MedicalItemType> onTreated;

    float holdTimer;
    float nextUseTime;

    void OnTriggerStay(Collider other)
    {
        MedicalItem item = other.GetComponentInParent<MedicalItem>();
        if (item == null || !item.IsHeld || item.type != requiredItem) return;
        if (Time.time < nextUseTime) return;

        if (requiredItem == MedicalItemType.Defibrillator)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= defibHoldTime)
            {
                Apply();
                holdTimer = 0f;
            }
        }
        else
        {
            Apply();
        }
    }

    void OnTriggerExit(Collider other)
    {
        MedicalItem item = other.GetComponentInParent<MedicalItem>();
        if (item != null && item.type == requiredItem)
            holdTimer = 0f;
    }

    void Apply()
    {
        nextUseTime = Time.time + cooldown;
        onTreated?.Invoke(requiredItem);
    }
}
