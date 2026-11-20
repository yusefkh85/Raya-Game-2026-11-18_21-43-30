using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AmbulanceShake : MonoBehaviour
{
    public static float Intensity { get; private set; }

    [Header("حرکت در صفحه منو (Start)")]
    public bool isDrivingInMenu = true;
    public float driveSpeed = 4.5f;
    public Vector3 localForwardAxis = Vector3.forward;

    [Header("افزایش تدریجی شدت تکان‌ها در طول بازی")]
    public bool increaseShakeOverTime = true;
    [Tooltip("هر ثانیه چقدر به شدت تکان‌ها اضافه شود")]
    public float intensityGrowthRate = 0.03f;
    [Tooltip("حداکثر ضریب شدت تکان‌ها تا آمبولانس زیادی پرتاب نشود")]
    public float maxGlobalStrength = 3.0f;

    [Header("دست‌انداز")]
    public float bumpInterval = 2.5f;
    public float bumpStrength = 0.06f;
    public float bumpDecay = 2.5f;

    [Header("پیچ تند")]
    public float turnInterval = 6f;
    public float turnDuration = 2f;
    public float turnLateral = 0.25f;
    public float turnRoll = 6f;

    [Header("ترمز ناگهانی")]
    public float brakeInterval = 12f;
    public float brakeDuration = 0.8f;
    public float brakeForwardPush = 0.2f;
    public float brakePitch = 4f;

    [Header("نویز ملایم همیشگی")]
    public float idleNoise = 0.02f;
    public float idleRotation = 1.2f;

    [Header("کنترل شدت فعلی")]
    [Range(0f, 3f)] public float globalStrength = 1f;

    Rigidbody rb;
    Vector3 basePos;
    Quaternion baseRot;
    float seed;
    bool gameStarted = false;

    float bumpTimer, bumpEnv;
    float turnTimer, turnActive, turnDir, turnEnv;
    float brakeTimer, brakeActive, brakeEnv;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        isDrivingInMenu = false;   // آمبولانس تا زدن دکمه‌ی Start ثابته (GameStartIntro حرکتش میده)
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

    // وقتی دوربین روی پلیر سوییچ شد، حرکت در خیابان متوقف می‌شود اما تکان‌ها ادامه می‌یابند و بیشتر می‌شوند
    public void StopDrivingAndRampUpShake()
    {
        isDrivingInMenu = false;
        gameStarted = true;
    }

    // با زدن Start صدا زده میشه: آمبولانس شروع به حرکت می‌کنه
    public void StartDriving()
    {
        isDrivingInMenu = true;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        float t = Time.time;

        // ۱. حرکت در خیابان فقط تا قبل از سوییچ روی دوربین پلیر
        if (isDrivingInMenu)
        {
            Vector3 worldForward = baseRot * localForwardAxis.normalized;
            basePos += worldForward * driveSpeed * dt;
        }

        // ۲. افزایش تدریجی شدت تکان‌ها هر چه زمان بازی می‌گذرد
        if (gameStarted && increaseShakeOverTime)
        {
            globalStrength = Mathf.Min(maxGlobalStrength, globalStrength + intensityGrowthRate * dt);
        }

        // ۳. دست‌انداز
        bumpTimer -= dt;
        if (bumpTimer <= 0f)
        {
            bumpEnv = 1f;
            bumpTimer = bumpInterval * Random.Range(0.6f, 1.4f);
        }
        bumpEnv = Mathf.MoveTowards(bumpEnv, 0f, bumpDecay * dt);

        // ۴. پیچ تند
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

        // ۵. ترمز
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

        // ۶. نویز ملایم
        float nx = Mathf.PerlinNoise(t * 2.3f + seed, 0f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(t * 3.1f + seed, 7f) * 2f - 1f;
        float nz = Mathf.PerlinNoise(t * 2.7f + seed, 3f) * 2f - 1f;

        float s = globalStrength;
        float jitter = Mathf.Sin(t * 40f) * bumpEnv * bumpStrength * s;

        Vector3 offset = new Vector3(
            nx * idleNoise * s + turnEnv * turnLateral * s,
            ny * idleNoise * s + jitter,
            nz * idleNoise * s + brakeEnv * brakeForwardPush * s
        );

        float roll = turnEnv * -turnRoll * s + nx * idleRotation * s;
        float pitch = brakeEnv * brakePitch * s + Mathf.Sin(t * 30f) * bumpEnv * 1.5f * s + ny * idleRotation * 0.5f * s;

        rb.MovePosition(basePos + offset);
        rb.MoveRotation(baseRot * Quaternion.Euler(pitch, 0f, roll));

        Intensity = Mathf.Clamp01((bumpEnv + Mathf.Abs(turnEnv) + brakeEnv) * s * 0.7f);
    }
}