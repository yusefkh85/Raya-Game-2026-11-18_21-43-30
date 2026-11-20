using UnityEngine;
using System;

public enum PatientState { Happy, Angry, Sick, Dead }

public class PatientStateManager : MonoBehaviour
{
    [Header("Patient models (assign yourself)")]
    public GameObject happyModel;
    public GameObject angryModel;
    public GameObject sickModel;
    public GameObject deadModel;

    public PatientState CurrentState { get; private set; } = PatientState.Happy;

    void Awake()
    {
        // همه خاموش، بعد مدل حالت شروع (Happy) روشن میشه.
        // (قبلا همه خاموش می‌موندن و چون SetState(Happy) بخاطر یکی بودن حالت return می‌کرد،
        //  بیمار تا اولین نیاز اصلا دیده نمیشد.)
        HideAll();
        ShowModel(CurrentState);
    }

    public void SetState(PatientState newState)
    {
        if (CurrentState == newState) return;
        if (CurrentState == PatientState.Dead) return;   // مرده دیگه برنمی‌گرده

        HideAll();
        ShowModel(newState);
        CurrentState = newState;
    }

    void HideAll()
    {
        if (happyModel) happyModel.SetActive(false);
        if (angryModel) angryModel.SetActive(false);
        if (sickModel) sickModel.SetActive(false);
        if (deadModel) deadModel.SetActive(false);
    }

    void ShowModel(PatientState state)
    {
        switch (state)
        {
            case PatientState.Happy: if (happyModel) happyModel.SetActive(true); break;
            case PatientState.Angry: if (angryModel) angryModel.SetActive(true); break;
            case PatientState.Sick: if (sickModel) sickModel.SetActive(true); break;
            case PatientState.Dead: if (deadModel) deadModel.SetActive(true); break;
        }
    }
}
