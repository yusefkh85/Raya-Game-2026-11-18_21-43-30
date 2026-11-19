using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FirstPersonFloppyHands : MonoBehaviour
{
    [Header("تنظیمات دوربین و حس حرکت")]
    public Transform playerCamera;
    public float mouseSensitivity = 2.5f;
    public float cameraTiltAmount = 1.5f;
    private float xRotation = 0f;
    private float currentTilt = 0f;
    private float defaultCamY;
    private float camLandOffset = 0f;

    [Header("حرکت و پرش (Human Fall Flat Style)")]
    public float moveSpeed = 5.5f;
    public float airControlMultiplier = 0.65f; // کنترل حرکت در هوا (نسبت به روی زمین)
    public float jumpForce = 5.8f;        // قدرت پرش
    public float extraGravity = 12f;      // جاذبه اضافه برای فرود سنگین و بامزه

    [Header("تنظیمات دست‌های ژله‌ای")]
    public float reachForce = 95f;
    public float idleHangForce = 18f;
    public float wiggleSpeed = 10f;

    [Header("گرفتن و پرتاب وسایل")]
    public float grabRange = 2.6f;
    public float throwForce = 9f;
    public string[] grabbableTags = { "Item", "MedicalItem" };

    private Rigidbody rb;
    private CapsuleCollider col;
    private Rigidbody leftUpper, leftForearm, rightUpper, rightForearm;
    private Transform leftShoulder, rightShoulder, leftGrabPoint, rightGrabPoint;
    private Rigidbody leftHeldItem, rightHeldItem;
    private Vector3 moveInput;
    private float rawH, rawV;
    private bool canGrabSomething = false;
    private bool isGrounded = true;
    private bool wasGrounded = true;
    private const int IgnoreRaycastLayer = 2; // لایه‌ی پیش‌فرض یونیتی "Ignore Raycast"

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.mass = 5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var mesh = GetComponent<MeshRenderer>();
        if (mesh != null) mesh.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>().transform;

        defaultCamY = playerCamera.localPosition.y;

        leftShoulder = CreateAnchor("LeftShoulder", new Vector3(-0.38f, -0.25f, 0.2f));
        rightShoulder = CreateAnchor("RightShoulder", new Vector3(0.38f, -0.25f, 0.2f));
        leftGrabPoint = CreateAnchor("LeftGrabPoint", new Vector3(-0.22f, -0.15f, 1.15f));
        rightGrabPoint = CreateAnchor("RightGrabPoint", new Vector3(0.22f, -0.15f, 1.15f));

        BuildTwoPartArm("Left", leftShoulder, out leftUpper, out leftForearm);
        BuildTwoPartArm("Right", rightShoulder, out rightUpper, out rightForearm);
    }

    Transform CreateAnchor(string objName, Vector3 localPos)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(playerCamera);
        go.transform.localPosition = localPos;
        return go.transform;
    }

    void BuildTwoPartArm(string side, Transform shoulderAnchor, out Rigidbody upperRb, out Rigidbody forearmRb)
    {
        GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        upper.name = side + "_UpperArm";
        upper.transform.position = shoulderAnchor.position + Vector3.down * 0.2f;
        upper.transform.localScale = new Vector3(0.13f, 0.22f, 0.13f);
        upper.layer = IgnoreRaycastLayer; // تا روی تشخیص زمین و پرتوی گرفتن اجسام تاثیر نذاره
        Physics.IgnoreCollision(col, upper.GetComponent<Collider>(), true);

        upperRb = upper.AddComponent<Rigidbody>();
        upperRb.mass = 0.2f;
        upperRb.linearDamping = 8f;
        upperRb.angularDamping = 6f;

        SpringJoint shoulderJoint = upper.AddComponent<SpringJoint>();
        shoulderJoint.connectedBody = rb;
        shoulderJoint.autoConfigureConnectedAnchor = false;
        shoulderJoint.anchor = new Vector3(0f, 0.8f, 0f);
        shoulderJoint.connectedAnchor = shoulderAnchor.localPosition + Vector3.up * 0.7f;
        shoulderJoint.spring = 120f;
        shoulderJoint.damper = 8f;
        shoulderJoint.maxDistance = 0.02f;

        GameObject forearm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        forearm.name = side + "_Forearm";
        forearm.transform.position = upper.transform.position + Vector3.down * 0.35f;
        forearm.transform.localScale = new Vector3(0.12f, 0.24f, 0.12f);
        forearm.layer = IgnoreRaycastLayer;
        Physics.IgnoreCollision(col, forearm.GetComponent<Collider>(), true);
        Physics.IgnoreCollision(upper.GetComponent<Collider>(), forearm.GetComponent<Collider>(), true);

        GameObject glove = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glove.transform.SetParent(forearm.transform);
        glove.transform.localPosition = new Vector3(0f, -0.95f, 0f);
        glove.transform.localScale = new Vector3(1.65f, 0.65f, 1.65f);
        glove.GetComponent<Renderer>().material.color = new Color(0.2f, 0.65f, 0.95f);
        Destroy(glove.GetComponent<Collider>());

        forearmRb = forearm.AddComponent<Rigidbody>();
        forearmRb.mass = 0.12f;
        forearmRb.linearDamping = 6f;
        forearmRb.angularDamping = 5f;

        CharacterJoint elbowJoint = forearm.AddComponent<CharacterJoint>();
        elbowJoint.connectedBody = upperRb;
        elbowJoint.anchor = new Vector3(0f, 0.85f, 0f);
        elbowJoint.autoConfigureConnectedAnchor = false;
        elbowJoint.connectedAnchor = new Vector3(0f, -0.85f, 0f);
    }

    void Update()
    {
        // ۱. بررسی تماس با زمین (برای پرش)
        CheckGrounded();

        // ۲. چرخش دوربین با موس
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -75f, 75f);

        rawH = Input.GetAxisRaw("Horizontal");
        rawV = Input.GetAxisRaw("Vertical");
        moveInput = (transform.right * rawH + transform.forward * rawV).normalized;

        // کج شدن نرم دوربین هنگام حرکت + نشست دوربین هنگام فرود از پرش
        float targetTilt = -rawH * cameraTiltAmount;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, 8f * Time.deltaTime);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        transform.Rotate(Vector3.up * mouseX);

        camLandOffset = Mathf.Lerp(camLandOffset, 0f, 10f * Time.deltaTime);
        Vector3 camPos = playerCamera.localPosition;
        camPos.y = defaultCamY + camLandOffset;
        playerCamera.localPosition = camPos;

        // ۳. پرش فان با دکمه Space
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        // ۴. بررسی قابلیت برداشتن وسیله
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        canGrabSomething = Physics.Raycast(ray, out RaycastHit hit, grabRange) && IsGrabbable(hit.collider);

        // کنترل دست چپ (کلیک چپ)
        if (Input.GetMouseButtonDown(0) && leftHeldItem == null)
            TryGrab(leftGrabPoint, ref leftHeldItem);
        if (Input.GetMouseButtonUp(0) && leftHeldItem != null)
            Release(ref leftHeldItem, Input.GetKey(KeyCode.Q));

        // کنترل دست راست (کلیک راست)
        if (Input.GetMouseButtonDown(1) && rightHeldItem == null)
            TryGrab(rightGrabPoint, ref rightHeldItem);
        if (Input.GetMouseButtonUp(1) && rightHeldItem != null)
            Release(ref rightHeldItem, Input.GetKey(KeyCode.Q));
    }

    void CheckGrounded()
    {
        // تشخیص برخورد کف کپسول با زمین
        float rayLength = (col.height * 0.5f) + 0.15f;
        isGrounded = Physics.SphereCast(transform.position, col.radius * 0.85f, Vector3.down, out _, rayLength);

        // لحظه فرود اومدن روی زمین (Landing Impact)
        // فقط وقتی واقعاً سقوط بوده (سرعت رو به پایین محسوس داشته) فعال میشه
        // تا نوسان لحظه‌ای isGrounded (مثلاً روی ناهمواری‌های کوچیک زمین) باعث پرش دوربین نشه
        if (isGrounded && !wasGrounded && rb.linearVelocity.y < -2f)
        {
            camLandOffset = -0.18f; // سر کمی میره پایین (حس وزن کاراکتر)
            leftForearm.AddForce(Vector3.down * 4f, ForceMode.Impulse);
            rightForearm.AddForce(Vector3.down * 4f, ForceMode.Impulse);
        }
        wasGrounded = isGrounded;
    }

    void Jump()
    {
        // صفر کردن سرعت عمودی قبلی و اعمال نیروی پرش
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        // پرتاب بامزه‌ی دست‌ها به بالا هنگام پرش (حس Human Fall Flat)
        leftForearm.AddForce((Vector3.up * 3.2f + playerCamera.right * -0.8f), ForceMode.Impulse);
        rightForearm.AddForce((Vector3.up * 3.2f + playerCamera.right * 0.8f), ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // حرکت روی زمین (در هوا کمی کنترل کمتر، ولی بدون لغزش - سرعت مستقیم ست می‌شود)
        float controlMultiplier = isGrounded ? 1f : airControlMultiplier;
        Vector3 targetVel = moveInput * moveSpeed * controlMultiplier;
        targetVel.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVel;

        // جاذبه اضافه در هوا برای اینکه پرش حالت معلقِ فضایی نداشته باشه
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }

        leftUpper.AddForce((leftShoulder.position - leftUpper.position) * 140f);
        rightUpper.AddForce((rightShoulder.position - rightUpper.position) * 140f);

        // منطق دست چپ
        if (Input.GetMouseButton(0) || leftHeldItem != null)
        {
            leftForearm.AddForce((leftGrabPoint.position - leftForearm.position) * reachForce);
        }
        else
        {
            Vector3 lIdle = playerCamera.TransformPoint(new Vector3(-0.35f, -0.45f, 0.55f));
            leftForearm.AddForce((lIdle - leftForearm.position) * idleHangForce);
            if (moveInput.sqrMagnitude > 0.1f && isGrounded)
            {
                float swing = Mathf.Sin(Time.time * wiggleSpeed) * 7f;
                leftForearm.AddForce(playerCamera.forward * swing + playerCamera.up * Mathf.Abs(swing) * 0.4f);
            }
        }

        // منطق دست راست
        if (Input.GetMouseButton(1) || rightHeldItem != null)
        {
            rightForearm.AddForce((rightGrabPoint.position - rightForearm.position) * reachForce);
        }
        else
        {
            Vector3 rIdle = playerCamera.TransformPoint(new Vector3(0.35f, -0.45f, 0.55f));
            rightForearm.AddForce((rIdle - rightForearm.position) * idleHangForce);
            if (moveInput.sqrMagnitude > 0.1f && isGrounded)
            {
                float swing = Mathf.Sin(Time.time * wiggleSpeed) * 7f;
                rightForearm.AddForce(-playerCamera.forward * swing + playerCamera.up * Mathf.Abs(swing) * 0.4f);
            }
        }
    }

    void TryGrab(Transform targetPoint, ref Rigidbody heldSlot)
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, grabRange))
        {
            if (hit.rigidbody != null && IsGrabbable(hit.collider))
            {
                heldSlot = hit.rigidbody;
                heldSlot.isKinematic = true;
                heldSlot.transform.SetParent(targetPoint);
                heldSlot.transform.localPosition = Vector3.zero;
                heldSlot.transform.localRotation = Quaternion.identity;
            }
        }
    }

    // یک شیء قابل برداشتن است اگر تگش داخل لیست grabbableTags باشد
    bool IsGrabbable(Collider target)
    {
        foreach (string tag in grabbableTags)
        {
            if (target.CompareTag(tag))
                return true;
        }
        return false;
    }

    void Release(ref Rigidbody heldSlot, bool throwHard)
    {
        heldSlot.transform.SetParent(null);
        heldSlot.isKinematic = false;
        if (throwHard)
            heldSlot.AddForce(playerCamera.forward * throwForce, ForceMode.Impulse);
        heldSlot = null;
    }

    void OnGUI()
    {
        float size = canGrabSomething ? 12f : 6f;
        GUI.color = canGrabSomething ? Color.green : new Color(1f, 1f, 1f, 0.75f);
        GUI.DrawTexture(new Rect(Screen.width / 2f - size / 2f, Screen.height / 2f - size / 2f, size, size), Texture2D.whiteTexture);
    }

    public void Bump(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        leftForearm.AddForce(Random.insideUnitSphere * force.magnitude * 2.5f, ForceMode.Impulse);
        rightForearm.AddForce(Random.insideUnitSphere * force.magnitude * 2.5f, ForceMode.Impulse);
    }
}