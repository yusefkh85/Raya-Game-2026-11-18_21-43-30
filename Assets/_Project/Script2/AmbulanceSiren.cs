using UnityEngine;

public class AmbulanceSiren : MonoBehaviour
{
    [Header("منابع نور آژیر")]
    public Light redLight;
    public Light blueLight;

    [Header("مش‌های درخشان روی سقف (اختیاری)")]
    public Renderer redMeshRenderer;
    public Renderer blueMeshRenderer;

    [Header("تنظیمات چشمک زدن")]
    public float flashSpeed = 8f;
    public float maxLightIntensity = 2500f;
    public float maxEmissionIntensity = 12f;

    private Material redMat;
    private Material blueMat;

    void Start()
    {
        if (redMeshRenderer != null) redMat = redMeshRenderer.material;
        if (blueMeshRenderer != null) blueMat = blueMeshRenderer.material;
    }

    void Update()
    {
        // الگوی موجی برای چشمک زدن متناوب قرمز و آبی با ریتم سریع آمبولانس
        float wave = Mathf.Sin(Time.time * flashSpeed);
        float doublePulse = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed * 2.5f));

        bool isRedPhase = wave >= 0f;
        float redFactor = isRedPhase ? doublePulse : 0f;
        float blueFactor = !isRedPhase ? doublePulse : 0f;

        if (redLight != null)
            redLight.intensity = redFactor * maxLightIntensity;

        if (blueLight != null)
            blueLight.intensity = blueFactor * maxLightIntensity;

        // تغییر هم‌زمان درخشش شیشه چراغ‌ها روی سقف آمبولانس
        if (redMat != null)
            redMat.SetColor("_EmissiveColor", Color.red * (redFactor * maxEmissionIntensity));

        if (blueMat != null)
            blueMat.SetColor("_EmissiveColor", Color.blue * (blueFactor * maxEmissionIntensity));
    }
}