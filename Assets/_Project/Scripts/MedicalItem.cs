using UnityEngine;

public enum MedicalItemType { Syringe, Defibrillator, Bandage, BloodBag }

// وسیله پزشکی. وقتی روی قفسه است Kinematic و فرزند ShelfSlot است.
// با تکان آمبوالنس ممکن است بیفتد (Fall) و باید پرستار آن را برگرداند.
[RequireComponent(typeof(Rigidbody))]
public class MedicalItem : MonoBehaviour
{
    public MedicalItemType type;

    public Rigidbody Rb { get; private set; }
    public ShelfSlot Shelf { get; private set; }
    public bool IsHeld { get; set; }
    public bool IsOnShelf => Shelf != null;

    void Awake()
    {
        Rb = GetComponent<Rigidbody>();
    }

    public void PutOnShelf(ShelfSlot slot)
    {
        Shelf = slot;
        slot.Current = this;

        transform.SetParent(slot.transform, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Rb.isKinematic = true;
    }

    // از قفسه جدا می‌شود (هم برای گرفتن و هم برای افتادن)
    public void Detach()
    {
        if (Shelf != null)
        {
            Shelf.Current = null;
            Shelf = null;
        }
        transform.SetParent(null, true);
        Rb.isKinematic = false;
    }

    public void Fall(Vector3 impulse)
    {
        if (!IsOnShelf) return;
        Detach();
        Rb.AddForce(impulse, ForceMode.Impulse);
    }
}
