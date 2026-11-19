using UnityEngine;
using System;

public class PatientHealth : MonoBehaviour
{
    [Range(0, 100)] public float health = 100f;
    public event Action<float> OnHealthChanged;

    private PatientStateManager stateManager;

    void Awake() => stateManager = GetComponent<PatientStateManager>();

    public void ChangeHealth(float amount)
    {
        health = Mathf.Clamp(health + amount, 0f, 100f);
        OnHealthChanged?.Invoke(health);

        // قانون تغییر حالت
        if (health <= 0)
            stateManager.SetState(PatientState.Dead);
        else if (health < 40)
            stateManager.SetState(PatientState.Sick);
        else
            stateManager.SetState(PatientState.Happy);
    }
}