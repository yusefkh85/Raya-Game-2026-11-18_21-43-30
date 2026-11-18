using UnityEngine;

// مکانیک Sticky Hands: با نگه داشتن کلیک چپ موس، وسیله پزشکی را می‌گیرد.
// با رها کردن: اگر داخل قفسه باشد روی آن قرار می‌گیرد، وگرنه با سرعت دست پرتاب می‌شود.
// اسکریپت روی Head قرار می‌گیرد.
public class StickyHands : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Rigidbody handAnchor;          // Rigidbody از نوع Kinematic، فرزند Head، جلوی دوربین

    [Header("Grab")]
    public float grabRange = 2.5f;
    public LayerMask grabMask = ~0;
    public float grabSpring = 600f;
    public float grabDamper = 30f;

    [Header("Release / Throw")]
    public float throwMultiplier = 1.5f;
    public float maxShelfHandSpeed = 2f;  // اگر دست سریع حرکت کند، وسیله روی قفسه نمی‌نشیند
    public float shelfSearchRadius = 0.4f;

    MedicalItem held;
    ConfigurableJoint joint;
    Vector3 lastHandPos;
    Vector3 handVelocity;

    public MedicalItem Held => held;

    void Start()
    {
        lastHandPos = handAnchor.position;
    }

    void Update()
    {
        handVelocity = (handAnchor.position - lastHandPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastHandPos = handAnchor.position;

        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (Input.GetMouseButtonDown(0) && held == null)
            TryGrab();

        if (Input.GetMouseButtonUp(0) && held != null)
            Release();
    }

    void TryGrab()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, grabRange, grabMask, QueryTriggerInteraction.Ignore))
            return;

        MedicalItem item = hit.collider.GetComponentInParent<MedicalItem>();
        if (item == null) return;

        Grab(item, hit.point);
    }

    void Grab(MedicalItem item, Vector3 hitPoint)
    {
        item.Detach();
        item.IsHeld = true;
        held = item;

        joint = item.Rb.gameObject.AddComponent<ConfigurableJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = handAnchor;
        joint.anchor = item.transform.InverseTransformPoint(hitPoint);
        joint.connectedAnchor = Vector3.zero;
        joint.configuredInWorldSpace = false;

        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

        JointDrive posDrive = new JointDrive
        {
            positionSpring = grabSpring,
            positionDamper = grabDamper,
            maximumForce = Mathf.Infinity
        };
        joint.xDrive = posDrive;
        joint.yDrive = posDrive;
        joint.zDrive = posDrive;
        joint.targetPosition = Vector3.zero;

        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = new JointDrive
        {
            positionSpring = grabSpring * 0.5f,
            positionDamper = grabDamper,
            maximumForce = Mathf.Infinity
        };

        joint.projectionMode = JointProjectionMode.PositionAndRotation;
    }

    void Release()
    {
        MedicalItem item = held;

        if (joint != null) Destroy(joint);
        joint = null;

        item.IsHeld = false;
        held = null;

        bool slowHand = handVelocity.magnitude <= maxShelfHandSpeed;
        ShelfSlot slot = slowHand
            ? ShelfSlot.FindFree(item.transform.position, shelfSearchRadius)
            : null;

        if (slot != null)
        {
            item.PutOnShelf(slot);
        }
        else
        {
            item.Rb.AddForce(handVelocity * throwMultiplier, ForceMode.Impulse);
        }
    }
}
