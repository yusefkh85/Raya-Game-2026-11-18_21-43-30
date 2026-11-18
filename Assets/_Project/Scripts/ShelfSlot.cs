using UnityEngine;

// جای هر وسیله روی قفسه. یک Trigger Collider کوچک کنار Pivot قفسه بگذارید.
// اگر وسیله‌ای داخل آن رها شود، روی قفسه قرار می‌گیرد.
// هنگام تکان شدید، وسیله ممکن است از قفسه بیفتد.
public class ShelfSlot : MonoBehaviour
{
    public MedicalItem startingItem;          // وسیله‌ای که از ابتدا روی این قفسه است (اختیاری)
    public float fallThreshold = 0.35f;       // شدت تکان از این مقدار بالاتر باشد احتمال افتادن هست
    public float fallChancePerSecond = 0.6f;  // احتمال افتادن در هر ثانیه با شدت 1

    public MedicalItem Current { get; set; }

    void Start()
    {
        if (startingItem != null)
            startingItem.PutOnShelf(this);
    }

    void FixedUpdate()
    {
        if (Current == null) return;

        float intensity = AmbulanceShake.Intensity;
        if (intensity < fallThreshold) return;

        if (Random.value < fallChancePerSecond * intensity * Time.fixedDeltaTime)
        {
            Vector3 impulse = Random.insideUnitSphere * 0.5f + Vector3.down * 0.3f;
            Current.Fall(impulse);
        }
    }

    // نزدیک‌ترین قفسه خالی را در شعاع مشخص پیدا می‌کند
    public static ShelfSlot FindFree(Vector3 position, float radius)
    {
        ShelfSlot best = null;
        float bestDist = float.MaxValue;

        foreach (var col in Physics.OverlapSphere(position, radius))
        {
            var slot = col.GetComponentInParent<ShelfSlot>();
            if (slot == null || slot.Current != null) continue;

            float d = Vector3.Distance(position, slot.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = slot;
            }
        }
        return best;
    }
}
