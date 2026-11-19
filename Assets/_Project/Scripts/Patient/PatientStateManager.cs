using UnityEngine;
using System;

public enum PatientState { Happy, Angry, Sick, Dead }

public class PatientStateManager : MonoBehaviour
{
    [Header("مدل‌های بیمار — خودت پر کن")]
    public GameObject happyModel;
    public GameObject angryModel;
    public GameObject sickModel;
    public GameObject deadModel;

    public PatientState CurrentState { get; private set; }

    void Awake()
    {
        // غیرفعال کردن همه در شروع بازی
        if (happyModel) happyModel.SetActive(false);
        if (angryModel) angryModel.SetActive(false);
        if (sickModel) sickModel.SetActive(false);
        if (deadModel) deadModel.SetActive(false);
    }

    public void SetState(PatientState newState)
    {
        if (CurrentState == newState) return;

        // خاموش کردن همه
        if (happyModel) happyModel.SetActive(false);
        if (angryModel) angryModel.SetActive(false);
        if (sickModel) sickModel.SetActive(false);
        if (deadModel) deadModel.SetActive(false);

        // روشن کردن حالت جدید
        switch (newState)
        {
            case PatientState.Happy: if (happyModel) happyModel.SetActive(true); break;
            case PatientState.Angry: if (angryModel) angryModel.SetActive(true); break;
            case PatientState.Sick: if (sickModel) sickModel.SetActive(true); break;
            case PatientState.Dead: if (deadModel) deadModel.SetActive(true); break;
        }

        CurrentState = newState;
    }
}