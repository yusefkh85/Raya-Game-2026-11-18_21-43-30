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

    public bool IsGameOver { get { return gameOver; } }

    private int currentScore = 0;
    private int highScore = 0;
    private float startDistance;
    private bool gameOver = false;
    private PatientHealth patientHealth;

    void Start()
    {
        patientHealth = FindFirstObjectByType<PatientHealth>();
        startDistance = distanceToHospital;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (recordText) recordText.text = "HighScore : " + highScore;
    }

    void Update()
    {
        if (gameOver || patientHealth == null) return;

        if (patientHealth.health > 0)
        {
            // ۱. زمان زنده موندن
            patientSurvivalTime += Time.deltaTime;

            // ۲. مسافت طی شده
            if (distanceToHospital > 0)
            {
                distanceToHospital -= ambulanceSpeed * Time.deltaTime;
                if (distanceToHospital < 0) distanceToHospital = 0;
            }

            // محاسبه امتیاز: هر ثانیه ۱۰ امتیاز + مسافت طی‌شده
            currentScore = Mathf.RoundToInt((patientSurvivalTime * 10) + (startDistance - distanceToHospital));

            UpdateUI();
        }
        else
        {
            GameOver();
        }
    }

    // فقط یک بار موقع مرگ بیمار اجرا میشه
    // (قبلا هر فریم بعد از مرگ PlayerPrefs.Save() صدا زده میشد چون highScore آپدیت نمی‌شد → لگ)
    void GameOver()
    {
        gameOver = true;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            if (recordText) recordText.text = "HighScore : " + highScore;
        }
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + currentScore;
        if (distanceText) distanceText.text = "Distance: " + Mathf.RoundToInt(distanceToHospital) + " M";
    }
}
