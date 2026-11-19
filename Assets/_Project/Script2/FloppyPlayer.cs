using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// کنترلر اول‌شخص با دست‌های ژله‌ای (سبک Human Fall Flat) — جایگزین FirstPersonFloppyHands
///
/// راه‌اندازی:
///  1) این اسکریپت رو روی آبجکت Player (همونی که Rigidbody و CapsuleCollider داره) بذار و کامپوننت قدیمی رو حذف کن.
///  2) دوربین باید بچه‌ی Player باشه و تو فیلد playerCamera قرار بگیره (اگه خالی باشه خودش پیداش می‌کنه).
///  3) وسایل قابل برداشتن: Rigidbody (غیر Kinematic) + تگ "Item" یا "MedicalItem".
///
/// کنترل‌ها:
///  WASD حرکت | Space پرش | ماوس نگاه
///  کلیک چپ = دست چپ ، کلیک راست = دست راست (نگه دار تا برداری، ول کن تا بندازی)
///  Q = پرتاب چیزی که تو دسته | E = استفاده از ابزار پزشکی (تو دست یا جلوی دوربین)
///  Escape = آزاد کردن موس
///
/// نکته‌ی مهم: دست‌ها فیزیک (Joint/Rigidbody) ندارن؛ فنر ساده‌ی ریاضی‌ان.
/// برای همین هیچ لگ و رفتار عجیب فیزیکی از اونا نمیاد.
/// </summary>
//[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FloppyPlayer : MonoBehaviour
{
    // ───────────────────────── تنظیمات ─────────────────────────
    [Header("دوربین")]
    public Transform playerCamera;
    public float mouseSensitivity = 2.5f;
    public float maxPitch = 80f;
    public bool lockCursorOnClick = true;   // کلیک تو بازی دوباره موس رو قفل می‌کنه (بعد از Escape)
    public bool hideBodyMesh = true;

    [Header("حرکت")]
    public float moveSpeed = 5.5f;
    public float groundAcceleration = 60f;  // سرعت رسیدن به سرعت هدف روی زمین
    public float airAcceleration = 25f;     // کنترل تو هوا
    public float bodyMass = 5f;
    public float maxSlopeAngle = 50f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask = Physics.DefaultRaycastLayers;

    [Header("پرش")]
    public float jumpHeight = 1.3f;              // ارتفاع پرش به متر
    public float fallGravityMultiplier = 1.8f;   // جاذبه‌ی بیشتر موقع سقوط (فرود سنگین)
    public float lowJumpMultiplier = 2.2f;       // اگه Space رو زود ول کنی پرش کوتاه‌تر میشه
    public float coyoteTime = 0.12f;             // بعد از لبه هنوز یه لحظه می‌تونی بپری
    public float jumpBufferTime = 0.12f;         // Space رو کمی زودتر از فرود بزنی هم قبول میشه
    public float maxFallSpeed = 40f;

    [Header("دست‌های ژله‌ای")]
    public float handStiffness = 140f;    // سفتی فنر
    public float handDamping = 9f;        // میرایی (کمتر = ژله‌ای‌تر)
    public float reachDistance = 1.0f;    // وقتی چیزی جلوت نیست دست تا کجا دراز میشه
    public float idleSway = 0.03f;        // تکون خوردن آروم دست‌ها
    public float walkSwing = 0.12f;       // تاب دست‌ها موقع راه رفتن
    public float walkSwingSpeed = 10f;
    public float lookInertia = 0.12f;     // لختی دست‌ها موقع چرخوندن موس
    public float moveInertia = 0.30f;     // لختی دست‌ها موقع شتاب گرفتن / پرش / فرود
    public Vector3 shoulderOffset = new Vector3(0.28f, -0.38f, 0f);  // x برای دست چپ قرینه میشه
    public Vector3 idleHandOffset = new Vector3(0.30f, -0.25f, 0.60f);
    public Vector3 holdOffset = new Vector3(0.22f, -0.12f, 1.10f);   // جایی که چیزی که گرفتی نگه داشته میشه
    public float upperArmLength = 0.38f;
    public float foreArmLength = 0.38f;
    public float armThickness = 0.10f;
    public float gloveSize = 0.17f;
    public Color armColor = new Color(0.95f, 0.80f, 0.65f);
    public Color gloveColor = new Color(0.2f, 0.65f, 0.95f);
    public Material handMaterial;         // اختیاری: متریال دلخواه برای همه‌ی اجزای دست

    [Header("گرفتن، پرتاب و استفاده")]
    public float grabRange = 2.6f;
    public float aimRadius = 0.1f;       // ضخامت پرتوی نشانه‌گیری (نشانه‌گیری راحت‌تر)
    public LayerMask aimMask = Physics.DefaultRaycastLayers;
    public float holdFollowSpeed = 18f;   // چقدر سریع وسیله به دست می‌رسه
    public float holdMaxSpeed = 20f;
    public float holdRotateSpeed = 12f;
    public float holdBreakDistance = 2.2f;// اگه وسیله گیر کنه و از دست دور بشه خودکار ول میشه
    public float throwSpeed = 9f;
    public float useRange = 2.6f;
    public string grabbableTags = "Item";

    // ───────────────────────── وضعیت داخلی ─────────────────────────
    class Hand
    {
        public int side;                 // -1 چپ ، +1 راست
        public int mouseButton;          // 0 چپ ، 1 راست
        public Vector3 shoulder, idle, hold;   // همه نسبت به دوربین (local)
        public Vector3 pos, vel;               // موقعیت و سرعت دست نسبت به دوربین
        public bool reaching;            // دکمه نگه داشته شده
        public bool armed;               // آماده‌ی گرفتن (بعد از پرتاب تا کلیک بعدی false میشه)
        public Rigidbody held;
        public Quaternion heldLocalRot = Quaternion.identity;
        public Transform upper, fore, glove;
    }

    class HeldInfo
    {
        public bool useGravity;
        public RigidbodyInterpolation interpolation;
        public CollisionDetectionMode detection;
        public Collider[] colliders;
    }

    Rigidbody rb;
    CapsuleCollider col;
    readonly Hand[] hands = new Hand[2];
    Transform handsRoot;

    float yaw, pitch;
    Vector2 lookDelta;
    float inputH, inputV;
    bool jumpHeld;
    float lastJumpPressTime = -999f, lastJumpTime = -999f, lastGroundedTime = -999f, stunUntil;
    bool isGrounded;
    Vector3 groundNormal = Vector3.up;
    Vector3 lastVelocity;
    float walkPhase;
    int relockFrame = -1;

    RaycastHit lookHit;
    bool lookHasHit;
    Rigidbody lookGrabbable;

    readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    readonly Dictionary<Rigidbody, HeldInfo> heldInfos = new Dictionary<Rigidbody, HeldInfo>();
    readonly Dictionary<Rigidbody, Collider[]> pendingRestore = new Dictionary<Rigidbody, Collider[]>();
    readonly List<Rigidbody> tmpBodies = new List<Rigidbody>();

    public bool IsGrounded { get { return isGrounded; } }

    // ───────────────────────── راه‌اندازی ─────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.mass = bodyMass;
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // بدون اصطکاک: به دیوار نمی‌چسبه و کنترل حرکت دست خودمونه
        PhysicsMaterial noFriction = new PhysicsMaterial("PlayerNoFriction");
        noFriction.dynamicFriction = 0f;
        noFriction.staticFriction = 0f;
        noFriction.bounciness = 0f;
        noFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
        noFriction.bounceCombine = PhysicsMaterialCombine.Minimum;
        col.material = noFriction;

        if (hideBodyMesh)
        {
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh != null) mesh.enabled = false;
        }

        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null) playerCamera = cam.transform;
        }
        if (playerCamera == null)
        {
            Debug.LogError("FloppyPlayer: دوربین پیدا نشد. یه Camera بذار زیر Player و تو فیلد playerCamera بکش.", this);
            enabled = false;
            return;
        }
        if (!playerCamera.IsChildOf(transform))
        {
            Debug.LogWarning("FloppyPlayer: دوربین بچه‌ی Player نبود؛ خودکار بچه‌ش شد.", this);
            playerCamera.SetParent(transform, true);
        }

        yaw = transform.eulerAngles.y;
        BuildHands();
    }

    void Start()
    {
        LockCursor(true);
    }

    void OnDisable()
    {
        // وسایل دست‌مون رو درست ول کن تا تو هوا معلق نمونن
        for (int i = 0; i < hands.Length; i++)
        {
            Hand h = hands[i];
            if (h == null) continue;
            if (h.held != null) ReleaseHand(h, false);
            h.reaching = false;
            h.armed = false;
        }
        if (col != null)
        {
            foreach (KeyValuePair<Rigidbody, Collider[]> kv in pendingRestore)
                SetIgnore(kv.Value, false);
        }
        pendingRestore.Clear();
    }

    void BuildHands()
    {
        handsRoot = new GameObject("FloppyHands").transform;
        handsRoot.SetParent(playerCamera, false);
        hands[0] = CreateHand(-1, 0, "Left");
        hands[1] = CreateHand(+1, 1, "Right");
    }

    Hand CreateHand(int side, int mouseButton, string handName)
    {
        Hand h = new Hand();
        h.side = side;
        h.mouseButton = mouseButton;
        h.shoulder = new Vector3(shoulderOffset.x * side, shoulderOffset.y, shoulderOffset.z);
        h.idle = new Vector3(idleHandOffset.x * side, idleHandOffset.y, idleHandOffset.z);
        h.hold = new Vector3(holdOffset.x * side, holdOffset.y, holdOffset.z);
        h.pos = h.idle;
        h.upper = CreatePart(handName + "_UpperArm", PrimitiveType.Capsule, armColor);
        h.fore = CreatePart(handName + "_Forearm", PrimitiveType.Capsule, armColor);
        h.glove = CreatePart(handName + "_Hand", PrimitiveType.Sphere, gloveColor);
        return h;
    }

    Transform CreatePart(string partName, PrimitiveType type, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = partName;
        go.layer = 2; // Ignore Raycast
        Collider c = go.GetComponent<Collider>();
        if (c != null) Destroy(c);   // فقط ظاهری‌ان، هیچ برخورد فیزیکی ندارن
        go.transform.SetParent(handsRoot, false);

        Renderer r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (handMaterial != null) r.sharedMaterial = handMaterial;
        else r.material.color = color;
        return go.transform;
    }

    // ───────────────────────── ورودی و دوربین ─────────────────────────
    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }
        else if (lockCursorOnClick && Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            LockCursor(true);
            relockFrame = Time.frameCount;   // همین کلیک نباید چیزی برداره
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            yaw += mx;
            pitch = Mathf.Clamp(pitch - my, -maxPitch, maxPitch);
            lookDelta = new Vector2(mx, my);

            inputH = Input.GetAxisRaw("Horizontal");
            inputV = Input.GetAxisRaw("Vertical");
            jumpHeld = Input.GetKey(KeyCode.Space);
            if (Input.GetKeyDown(KeyCode.Space)) lastJumpPressTime = Time.time;
        }
        else
        {
            lookDelta = Vector2.zero;
            inputH = 0f;
            inputV = 0f;
            jumpHeld = false;
        }
    }

    void LateUpdate()
    {
        // چرخش دوربین مستقیم از yaw/pitch: به نرخ فیزیک وابسته نیست، پس لرزش/لگ نداره
        playerCamera.rotation = Quaternion.Euler(pitch, yaw, 0f);

        UpdateAim();
        HandleHandInput();
        UpdateHands(Time.deltaTime);
    }

    // ───────────────────────── فیزیک: حرکت و پرش ─────────────────────────
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        CleanupDestroyedBodies();
        UpdateGroundState();
        HandleJump();
        MoveCharacter(dt);
        DriveHeldItems();
        ProcessPendingRestore();

        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    void UpdateGroundState()
    {
        isGrounded = false;
        groundNormal = Vector3.up;

        Vector3 s = transform.lossyScale;
        float radius = col.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        float height = Mathf.Max(col.height * Mathf.Abs(s.y), radius * 2f);
        Vector3 center = transform.TransformPoint(col.center);
        Vector3 bottomSphere = center + Vector3.down * (height * 0.5f - radius);

        float castRadius = radius * 0.9f;
        const float lift = 0.1f;
        Vector3 origin = bottomSphere + Vector3.up * lift;
        float distance = lift + (radius - castRadius) + groundCheckDistance;

        int count = Physics.SphereCastNonAlloc(origin, castRadius, Vector3.down, hitBuffer, distance, groundMask, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (h.collider == null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;           // خودمون
            if (IsHeldBody(h.rigidbody)) continue;                              // وسیله‌ی تو دست زمین حساب نمیشه
            if (Vector3.Angle(h.normal, Vector3.up) > maxSlopeAngle) continue;  // سراشیبی خیلی تند
            if (h.distance < best)
            {
                best = h.distance;
                groundNormal = h.normal;
                isGrounded = true;
            }
        }

        // درست بعد از پرش زمین حساب نشه
        if (Time.time - lastJumpTime < 0.15f) isGrounded = false;
    }

    void HandleJump()
    {
        if (isGrounded) lastGroundedTime = Time.time;

        bool coyote = Time.time - lastGroundedTime <= coyoteTime;
        bool buffered = Time.time - lastJumpPressTime <= jumpBufferTime;
        bool cooledDown = Time.time - lastJumpTime > 0.25f;

        if (coyote && buffered && cooledDown)
        {
            float jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            Vector3 v = rb.linearVelocity;
            v.y = jumpSpeed;
            rb.linearVelocity = v;

            lastJumpTime = Time.time;
            lastJumpPressTime = -999f;
            lastGroundedTime = -999f;
            isGrounded = false;
        }
    }

    void MoveCharacter(float dt)
    {
        Vector3 wish = Quaternion.Euler(0f, yaw, 0f) * new Vector3(inputH, 0f, inputV);
        if (wish.sqrMagnitude > 1f) wish.Normalize();   // حرکت اریب تندتر نباشه

        Vector3 vel = rb.linearVelocity;
        bool stunned = Time.time < stunUntil;

        if (isGrounded)
        {
            // روی سطح زمین حرکت می‌کنیم (شیب‌ها هم درست کار می‌کنن) و بقیه‌ی سرعت (مثلا پرش) دست‌نخورده می‌مونه
            float accel = groundAcceleration * (stunned ? 0.15f : 1f);
            Vector3 along = Vector3.ProjectOnPlane(vel, groundNormal);
            Vector3 across = vel - along;
            Vector3 desired = Vector3.ProjectOnPlane(wish * moveSpeed, groundNormal);
            along = Vector3.MoveTowards(along, desired, accel * dt);
            vel = along + across;
        }
        else
        {
            // تو هوا: تکانه حفظ میشه، فقط می‌تونی جهت رو کمی عوض کنی
            Vector3 horizontal = new Vector3(vel.x, 0f, vel.z);
            float limit = Mathf.Max(moveSpeed, horizontal.magnitude);
            horizontal += wish * (airAcceleration * dt);
            if (horizontal.magnitude > limit) horizontal = horizontal.normalized * limit;
            vel.x = horizontal.x;
            vel.z = horizontal.z;

            if (vel.y < 0f) vel += Physics.gravity * ((fallGravityMultiplier - 1f) * dt);
            else if (vel.y > 0f && !jumpHeld) vel += Physics.gravity * ((lowJumpMultiplier - 1f) * dt);
            vel.y = Mathf.Max(vel.y, -maxFallSpeed);
        }

        rb.linearVelocity = vel;
    }

    // ───────────────────────── نشانه‌گیری ─────────────────────────
    void UpdateAim()
    {
        lookHasHit = false;
        lookGrabbable = null;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        int count = Physics.SphereCastNonAlloc(ray, aimRadius, hitBuffer, grabRange, aimMask, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (h.collider == null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;   // بدن خودمون
            if (IsHeldBody(h.rigidbody)) continue;                      // چیزی که الان تو دسته جلوی دید رو نگیره
            if (h.distance < best)
            {
                best = h.distance;
                lookHit = h;
                lookHasHit = true;
            }
        }

        if (lookHasHit)
        {
            Rigidbody body = lookHit.rigidbody;
            if (body != null && !body.isKinematic && HasGrabbableTag(lookHit.collider, body))
                lookGrabbable = body;
        }
    }

    bool HasGrabbableTag(Collider c, Rigidbody body)
    {
        string t = grabbableTags;
        if (string.IsNullOrEmpty(t)) return false;
        if (c.CompareTag(t) || body.CompareTag(t)) return true;
        return false;
    }

    bool IsHeldBody(Rigidbody b)
    {
        return b != null && (hands[0].held == b || hands[1].held == b);
    }

    // ───────────────────────── گرفتن / ول کردن / استفاده ─────────────────────────
    void HandleHandInput()
    {
        bool canInput = Cursor.lockState == CursorLockMode.Locked && relockFrame != Time.frameCount;

        for (int i = 0; i < hands.Length; i++)
        {
            Hand h = hands[i];
            bool pressed = canInput && Input.GetMouseButtonDown(h.mouseButton);
            bool down = canInput && Input.GetMouseButton(h.mouseButton);

            if (pressed)
            {
                h.reaching = true;
                h.armed = true;
            }

            if (!down && h.reaching)
            {
                h.reaching = false;
                h.armed = false;
                if (h.held != null) ReleaseHand(h, false);
            }

            // تا وقتی دکمه نگه داشته شده و دست خالیه، به محض نشانه گرفتن یه وسیله برش می‌داره
            if (h.reaching && h.armed && h.held == null && lookGrabbable != null)
                Grab(h, lookGrabbable);
        }

        if (canInput && Input.GetKeyDown(KeyCode.Q))
        {
            for (int i = 0; i < hands.Length; i++)
                if (hands[i].held != null) ReleaseHand(hands[i], true);
        }

        if (canInput && Input.GetKeyDown(KeyCode.E))
            TryUse();
    }

    void Grab(Hand h, Rigidbody body)
    {
        h.held = body;
        h.armed = false;
        h.heldLocalRot = Quaternion.Inverse(playerCamera.rotation) * body.rotation;

        if (heldInfos.ContainsKey(body)) return;

        HeldInfo info = new HeldInfo();
        info.useGravity = body.useGravity;
        info.interpolation = body.interpolation;
        info.detection = body.collisionDetectionMode;
        info.colliders = body.GetComponentsInChildren<Collider>();
        heldInfos[body] = info;
        pendingRestore.Remove(body);

        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        SetIgnore(info.colliders, true);   // وسیله‌ی تو دست با بدن خودمون برخورد نکنه (لرزش/هل دادن نداشته باشیم)
    }

    void ReleaseHand(Hand h, bool doThrow)
    {
        Rigidbody body = h.held;
        h.held = null;
        h.armed = false;
        if (doThrow) h.vel += new Vector3(0f, 0.5f, 2.5f);   // حرکت پرتاب دست
        if (body != null) FinishHold(body, doThrow);
    }

    void FinishHold(Rigidbody body, bool doThrow)
    {
        HeldInfo info;
        if (!heldInfos.TryGetValue(body, out info)) return;
        heldInfos.Remove(body);

        body.useGravity = info.useGravity;
        body.interpolation = info.interpolation;
        body.collisionDetectionMode = info.detection;

        if (doThrow)
            body.linearVelocity = rb.linearVelocity + playerCamera.forward * throwSpeed;

        // برخورد با بازیکن وقتی دوباره فعال میشه که دیگه توی هم نباشن
        pendingRestore[body] = info.colliders;
        body.WakeUp();
    }

    void SetIgnore(Collider[] colliders, bool ignore)
    {
        if (colliders == null || col == null) return;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c != null && c != col && !c.isTrigger)
                Physics.IgnoreCollision(col, c, ignore);
        }
    }

    void ProcessPendingRestore()
    {
        if (pendingRestore.Count == 0) return;

        tmpBodies.Clear();
        foreach (KeyValuePair<Rigidbody, Collider[]> kv in pendingRestore)
            tmpBodies.Add(kv.Key);

        Bounds playerBounds = col.bounds;
        for (int i = 0; i < tmpBodies.Count; i++)
        {
            Rigidbody body = tmpBodies[i];
            Collider[] cols = pendingRestore[body];

            bool overlapping = false;
            if (body != null && cols != null)
            {
                for (int j = 0; j < cols.Length; j++)
                {
                    if (cols[j] != null && cols[j].bounds.Intersects(playerBounds)) { overlapping = true; break; }
                }
            }
            if (overlapping) continue;

            SetIgnore(cols, false);
            pendingRestore.Remove(body);
        }
    }

    void CleanupDestroyedBodies()
    {
        if (heldInfos.Count > 0)
        {
            tmpBodies.Clear();
            foreach (KeyValuePair<Rigidbody, HeldInfo> kv in heldInfos)
                if (kv.Key == null) tmpBodies.Add(kv.Key);
            for (int i = 0; i < tmpBodies.Count; i++) heldInfos.Remove(tmpBodies[i]);
        }
        if (pendingRestore.Count > 0)
        {
            tmpBodies.Clear();
            foreach (KeyValuePair<Rigidbody, Collider[]> kv in pendingRestore)
                if (kv.Key == null) tmpBodies.Add(kv.Key);
            for (int i = 0; i < tmpBodies.Count; i++) pendingRestore.Remove(tmpBodies[i]);
        }
    }

    // وسیله‌ی تو دست رو با سرعت (نه Parent کردن) دنبال دست می‌کشیم:
    // با دیوارها برخورد می‌کنه، از وسط دیوار رد نمیشه و حرکت ژله‌ای دست رو هم نشون میده
    void DriveHeldItems()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            Hand h = hands[i];
            Rigidbody body = h.held;
            if (body == null) continue;

            Vector3 handWorld = playerCamera.TransformPoint(h.pos);
            Vector3 handWorldVel = playerCamera.TransformDirection(h.vel);
            Vector3 error = handWorld - body.worldCenterOfMass;

            if (error.magnitude > holdBreakDistance)
            {
                ReleaseHand(h, false);   // گیر کرده، ول کن
                continue;
            }

            Vector3 correction = Vector3.ClampMagnitude(error * holdFollowSpeed, holdMaxSpeed);
            body.linearVelocity = correction + handWorldVel + rb.linearVelocity;

            Quaternion targetRot = playerCamera.rotation * h.heldLocalRot;
            Quaternion delta = targetRot * Quaternion.Inverse(body.rotation);
            float angle;
            Vector3 axis;
            delta.ToAngleAxis(out angle, out axis);
            if (angle > 180f) angle -= 360f;

            Vector3 w = Vector3.zero;
            if (!float.IsNaN(axis.x) && !float.IsInfinity(axis.x) && axis.sqrMagnitude > 0.0001f)
                w = axis.normalized * (angle * Mathf.Deg2Rad * holdRotateSpeed);
            body.angularVelocity = Vector3.ClampMagnitude(w, 20f);
        }
    }

    void TryUse()
    {
        // اول چیزی که تو دسته
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i].held == null) continue;
            MedicalTool heldTool = FindTool(hands[i].held.transform);
            if (heldTool != null && heldTool.Use()) return;
        }

        // بعد ابزاری که بهش نگاه می‌کنی
        if (lookHasHit && lookHit.distance <= useRange)
        {
            MedicalTool tool = FindTool(lookHit.collider.transform);
            if (tool != null) tool.Use();
        }
    }

    static MedicalTool FindTool(Transform t)
    {
        MedicalTool tool = t.GetComponentInParent<MedicalTool>();
        if (tool == null) tool = t.GetComponentInChildren<MedicalTool>();
        return tool;
    }

    // ───────────────────────── دست‌های ژله‌ای (ظاهری، بدون فیزیک) ─────────────────────────
    void UpdateHands(float dt)
    {
        dt = Mathf.Min(dt, 0.033f);
        float time = Time.time;

        // لختی: تغییر سرعت بدن (شتاب، پرش، فرود) به دست‌ها تکون میده
        Vector3 vel = rb.linearVelocity;
        Vector3 localDv = playerCamera.InverseTransformDirection(vel - lastVelocity);
        localDv = Vector3.ClampMagnitude(localDv, 12f);
        lastVelocity = vel;

        float planarSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
        float speed01 = isGrounded ? Mathf.Clamp01(planarSpeed / Mathf.Max(0.01f, moveSpeed)) : 0f;
        walkPhase += dt * walkSwingSpeed * speed01;

        for (int i = 0; i < hands.Length; i++)
        {
            Hand h = hands[i];

            // ۱) هدف دست
            Vector3 target;
            float inertia = 1f;
            if (h.held != null)
            {
                target = h.hold;
                inertia = 0.35f;
            }
            else if (h.reaching)
            {
                float d = lookHasHit ? Mathf.Max(0.35f, lookHit.distance - 0.1f) : reachDistance;
                target = new Vector3(h.side * 0.1f, -0.03f, d);
                inertia = 0.6f;
            }
            else
            {
                float seed = (h.side + 1) * 8.7f;
                float nx = Mathf.PerlinNoise(time * 0.7f + seed, 0.37f) - 0.5f;
                float ny = Mathf.PerlinNoise(time * 0.9f + seed, 5.11f) - 0.5f;
                float swing = Mathf.Sin(walkPhase) * -h.side;   // دست‌ها خلاف هم تاب می‌خورن
                target = h.idle
                       + new Vector3(nx, ny, 0f) * (idleSway * 2f)
                       + new Vector3(0f, Mathf.Abs(swing) * 0.4f, swing) * (walkSwing * speed01);
            }

            // ۲) ضربه‌های لختی
            Vector3 impulse = new Vector3(-lookDelta.x, -lookDelta.y, 0f) * lookInertia - localDv * moveInertia;
            h.vel += Vector3.ClampMagnitude(impulse, 4f) * inertia;

            // ۳) فنر (دو زیرگام برای پایداری)
            const int steps = 2;
            float sdt = dt / steps;
            for (int s = 0; s < steps; s++)
            {
                Vector3 acc = (target - h.pos) * handStiffness - h.vel * handDamping;
                h.vel += acc * sdt;
                h.pos += h.vel * sdt;
            }

            // ۴) محدودیت‌ها: دست از حد دسترسی بیرون نره و پشت دوربین نره
            Vector3 fromShoulder = h.pos - h.shoulder;
            float maxDist = grabRange + 0.3f;
            if (fromShoulder.sqrMagnitude > maxDist * maxDist)
            {
                Vector3 n = fromShoulder.normalized;
                h.pos = h.shoulder + n * maxDist;
                float outward = Vector3.Dot(h.vel, n);
                if (outward > 0f) h.vel -= n * outward;
            }
            if (h.pos.z < 0.2f)
            {
                h.pos.z = 0.2f;
                if (h.vel.z < 0f) h.vel.z = 0f;
            }

            UpdateArmVisual(h);
        }
    }

    // بازوی لاستیکی: IK دو‌تکه؛ اگه دست دورتر از طول بازو بره، بازو کش میاد
    void UpdateArmVisual(Hand h)
    {
        Vector3 shoulder = h.shoulder;
        Vector3 hand = h.pos;
        Vector3 toHand = hand - shoulder;

        float dist = Mathf.Max(toHand.magnitude, 0.05f);
        Vector3 dir = toHand.sqrMagnitude > 1e-6f ? toHand.normalized : Vector3.forward;

        float l1 = upperArmLength;
        float l2 = foreArmLength;
        float maxLen = (l1 + l2) * 0.98f;
        if (dist > maxLen)
        {
            float k = dist / maxLen;
            l1 *= k;
            l2 *= k;
        }

        float a = (l1 * l1 - l2 * l2 + dist * dist) / (2f * dist);
        float bend = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
        Vector3 pole = Vector3.ProjectOnPlane(new Vector3(h.side * 0.6f, -1f, -0.4f), dir);
        pole = pole.sqrMagnitude > 1e-4f ? pole.normalized : Vector3.down;
        Vector3 elbow = shoulder + dir * a + pole * bend;

        PlaceSegment(h.upper, shoulder, elbow, armThickness);
        PlaceSegment(h.fore, elbow, hand, armThickness * 0.9f);
        h.glove.localPosition = hand;
        h.glove.localScale = Vector3.one * gloveSize;
    }

    static void PlaceSegment(Transform seg, Vector3 a, Vector3 b, float thickness)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.001f) return;
        seg.localPosition = (a + b) * 0.5f;
        seg.localRotation = Quaternion.FromToRotation(Vector3.up, d / len);
        seg.localScale = new Vector3(thickness, len * 0.5f, thickness);   // کپسول یونیتی ارتفاعش ۲ و قطرش ۱ هست
    }

    // ───────────────────────── API برای بقیه‌ی اسکریپت‌ها ─────────────────────────
    // (مثل Bump قدیمی) ضربه به بدن + تکون دادن دست‌ها
    public void Bump(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        stunUntil = Time.time + 0.3f;   // یه لحظه کنترل کم میشه تا ضربه حس بشه
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] != null)
                hands[i].vel += Random.insideUnitSphere * (force.magnitude * 0.3f);
        }
    }

    // ───────────────────────── نشانه‌ی وسط صفحه ─────────────────────────
    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        bool canGrab = lookGrabbable != null;
        float size = canGrab ? 12f : 6f;
        GUI.color = canGrab ? Color.green : new Color(1f, 1f, 1f, 0.75f);
        GUI.DrawTexture(new Rect(Screen.width * 0.5f - size * 0.5f, Screen.height * 0.5f - size * 0.5f, size, size), Texture2D.whiteTexture);
    }
}
