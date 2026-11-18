using UnityEngine;

// تکان‌های کابین آمبوالنس: دست‌انداز، پیچ تند و ترمز ناگهانی.
// روی کابین (Cabin) با Rigidbody از نوع Kinematic قرار می‌گیرد.
// وسایل پزشکی و پرستار فرزند کابین هستند پس همراه آن تکان می‌خورند.
[RequireComponent(typeof(Rigidbody))]
public class AmbulanceShake : MonoBehaviour
{
    // شدت لحظه‌ای تکان (0 تا 1). وسایل و دوربین از آن استفاده می‌کنند.
    public static float Intensity { get; private set; }

    [Header("دست‌انداز")]
    public float bumpInterval = 2.5f;
    public float bumpStrength = 0.06f;   // متر
    public float bumpDecay = 2.5f;

    [Header("پیچ تند")]
    public float turnInterval = 6f;
    public float turnDuration = 2f;
    public float turnLateral = 0.25f;    // متر
    public float turnRoll = 6f;          // درجه

    [Header("ترمز ناگهانی")]
    public float brakeInterval = 12f;
    public float brakeDuration = 0.8f;
    public float brakeForwardPush = 0.2f; // متر
    public float brakePitch = 4f;         // درجه

    [Header("نویز ملایم همیشگی")]
    public float idleNoise = 0.02f;
    public float idleRotation = 1.2f;     // درجه

    [Header("کنترل")]
    [Range(0f, 2f)] public float globalStrength = 1f; // برای بالانس توسط گیم‌دیزاینر

    Rigidbody rb;
    Vector3 basePos;
    Quaternion baseRot;
    float seed;

    float bumpTimer, bumpEnv;
    float turnTimer, turnActive, turnDir, turnEnv;
    float brakeTimer, brakeActive, brakeEnv;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        basePos = rb.position;
        baseRot = rb.rotation;
        seed = Random.Range(0f, 1000f);
        bumpTimer = bumpInterval;
        turnTimer = turnInterval;
        brakeTimer = brakeInterval;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float t = Time.time;

        // دست‌انداز: پالس ناگهانی که کم کم فروکش می‌کند
        bumpTimer -= dt;
        if (bumpTimer <= 0f)
        {
            bumpEnv = 1f;
            bumpTimer = bumpInterval * Random.Range(0.6f, 1.4f);
        }
        bumpEnv = Mathf.MoveTowards(bumpEnv, 0f, bumpDecay * dt);

        // پیچ تند: یک منحنی سینوسی در طول turnDuration
        turnTimer -= dt;
        if (turnTimer <= 0f)
        {
            turnDir = Random.value < 0.5f ? -1f : 1f;
            turnActive = turnDuration;
            turnTimer = turnInterval * Random.Range(0.7f, 1.3f);
        }
        if (turnActive > 0f)
        {
            turnActive -= dt;
            turnEnv = Mathf.Sin(Mathf.PI * (1f - turnActive / turnDuration)) * turnDir;
        }
        else turnEnv = 0f;

        // ترمز: حرکت کوتاه به جلو و خم شدن کابین
        brakeTimer -= dt;
        if (brakeTimer <= 0f)
        {
            brakeActive = brakeDuration;
            brakeTimer = brakeInterval * Random.Range(0.7f, 1.3f);
        }
        if (brakeActive > 0f)
        {
            brakeActive -= dt;
            brakeEnv = Mathf.Sin(Mathf.PI * (1f - brakeActive / brakeDuration));
        }
        else brakeEnv = 0f;

        // نویز ملایم
        float nx = Mathf.PerlinNoise(t * 2.3f + seed, 0f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(t * 3.1f + seed, 7f) * 2f - 1f;
        float nz = Mathf.PerlinNoise(t * 2.7f + seed, 3f) * 2f - 1f;

        float s = globalStrength;
        float jitter = Mathf.Sin(t * 40f) * bumpEnv * bumpStrength * s;

        Vector3 offset = new Vector3(
            nx * idleNoise + turnEnv * turnLateral * s,
            ny * idleNoise + jitter,
            nz * idleNoise + brakeEnv * brakeForwardPush * s
        );

        float roll = turnEnv * -turnRoll * s + nx * idleRotation;
        float pitch = brakeEnv * brakePitch * s + Mathf.Sin(t * 30f) * bumpEnv * 1.5f * s + ny * idleRotation * 0.5f;

        rb.MovePosition(basePos + offset);
        rb.MoveRotation(baseRot * Quaternion.Euler(pitch, 0f, roll));

        Intensity = Mathf.Clamp01((bumpEnv + Mathf.Abs(turnEnv) + brakeEnv) * s * 0.7f);
    }
}
