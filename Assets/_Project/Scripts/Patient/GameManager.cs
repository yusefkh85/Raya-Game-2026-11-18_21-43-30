using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI recordText;

    [Header("تنظیمات بازی")]
    public float patientSurvivalTime = 0f;
    public float distanceToHospital = 1000f; // مثلا ۱۰۰۰ متر
    public float ambulanceSpeed = 20f; // سرعت آمبولانس

    private int currentScore = 0;
    private int highScore = 0;
    private PatientHealth patientHealth;

    void Start()
    {
        patientHealth = FindObjectOfType<PatientHealth>();
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (recordText) recordText.text = "رکورد: " + highScore;
    }

    void Update()
    {
        // اگر بیمار زنده است
        if (patientHealth != null && patientHealth.health > 0)
        {
            // ۱. زمان زنده موندن
            patientSurvivalTime += Time.deltaTime;
            
            // ۲. مسافت طی شده
            if (distanceToHospital > 0)
            {
                distanceToHospital -= ambulanceSpeed * Time.deltaTime;
                if (distanceToHospital < 0) distanceToHospital = 0;
            }

            // محاسبه امتیاز: هر ثانیه ۱۰ امتیاز + مسافت
            currentScore = Mathf.RoundToInt((patientSurvivalTime * 10) + (1000f - distanceToHospital));
            
            UpdateUI();
        }
        else
        {
            // اگر بیمار مرد، رکورد ذخیره بشه
            if (currentScore > highScore)
            {
                PlayerPrefs.SetInt("HighScore", currentScore);
                PlayerPrefs.Save();
            }
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "امتیاز: " + currentScore;
        if (distanceText) distanceText.text = "مسافت: " + Mathf.RoundToInt(distanceToHospital) + " متر";
    }
}