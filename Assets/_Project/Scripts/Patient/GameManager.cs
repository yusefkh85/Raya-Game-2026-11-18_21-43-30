using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("HUD texts")]
    public TextMeshProUGUI hpText;            // shows e.g. 50/100
    public TextMeshProUGUI survivalTimeText;  // shows e.g. Time: 01:25
    public TextMeshProUGUI distanceText;      // shows e.g. Distance: 850 m
    public TextMeshProUGUI averageSpeedText;  // shows e.g. Avg speed: 72 km/h

    [Header("Game settings")]
    public float maxHealth = 100f;            // only used for the "50/100" display
    public float patientSurvivalTime = 0f;
    public float distanceToHospital = 1000f;  // meters
    public float ambulanceSpeed = 20f;        // meters per second
    public bool speedInKmh = true;            // false = show m/s

    public bool IsGameOver { get { return gameOver; } }

    private int currentScore = 0;
    private int highScore = 0;
    private float startDistance;
    private bool gameOver = false;
    private PatientHealth patientHealth;

    // cached values so the texts are only rebuilt when the shown number changes (no per-frame garbage)
    private int lastHp = -1, lastSeconds = -1, lastDistance = -1, lastSpeed = -1;

    void Start()
    {
        patientHealth = FindFirstObjectByType<PatientHealth>();
        startDistance = distanceToHospital;
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateUI();
    }

    void Update()
    {
        if (gameOver || patientHealth == null) return;

        if (patientHealth.health > 0)
        {
            patientSurvivalTime += Time.deltaTime;

            if (distanceToHospital > 0)
            {
                distanceToHospital -= ambulanceSpeed * Time.deltaTime;
                if (distanceToHospital < 0) distanceToHospital = 0;
            }

            // score (not shown in the UI, still saved as the high score)
            currentScore = Mathf.RoundToInt((patientSurvivalTime * 10) + (startDistance - distanceToHospital));

            UpdateUI();
        }
        else
        {
            GameOver();
        }
    }

    // runs once when the patient dies
    void GameOver()
    {
        gameOver = true;
        UpdateUI();   // final values (HP 0/100)

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    void UpdateUI()
    {
        // 1) HP as a plain number: 50/100
        if (patientHealth != null)
        {
            int hp = Mathf.CeilToInt(patientHealth.health);
            if (hp != lastHp)
            {
                lastHp = hp;
                if (hpText) hpText.text = hp + "/" + Mathf.RoundToInt(maxHealth);
            }
        }

        // 2) survival time: mm:ss
        int seconds = Mathf.FloorToInt(patientSurvivalTime);
        if (seconds != lastSeconds)
        {
            lastSeconds = seconds;
            if (survivalTimeText)
                survivalTimeText.text = "Time: " + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        // 3) remaining distance
        int dist = Mathf.RoundToInt(distanceToHospital);
        if (dist != lastDistance)
        {
            lastDistance = dist;
            if (distanceText) distanceText.text = "Distance: " + dist + " m";
        }

        // 3b) average speed = distance covered / time alive
        float covered = startDistance - distanceToHospital;
        float avg = patientSurvivalTime > 0.1f ? covered / patientSurvivalTime : 0f;
        if (speedInKmh) avg *= 3.6f;
        int avgInt = Mathf.RoundToInt(avg);
        if (avgInt != lastSpeed)
        {
            lastSpeed = avgInt;
            if (averageSpeedText)
                averageSpeedText.text = "Avg speed: " + avgInt + (speedInKmh ? " km/h" : " m/s");
        }
    }
}
