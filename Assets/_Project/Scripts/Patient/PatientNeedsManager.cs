using UnityEngine;
using System.Collections;
using TMPro;

// one patient request: the text message and the item that fixes it
[System.Serializable]
public class PatientNeed
{
    public string message;
    public string correctItem;   // must match MedicalTool.itemName
}

// links an item name to the picture shown next to the message
[System.Serializable]
public class ItemIcon
{
    public string itemName;      // must match MedicalTool.itemName
    public GameObject icon;      // the Image/sprite object for that item
}

[RequireComponent(typeof(PatientHealth), typeof(PatientStateManager))]
public class PatientNeedsManager : MonoBehaviour
{
    [Header("Message UI")]
    public GameObject messagePanel;          // optional: parent object holding the text + icons (shown only during a need)
    public TextMeshProUGUI smsText;          // the message text
    public string messagePrefix = "";        // optional text before the message

    [Header("Item pictures (one per item, shown next to the message)")]
    public ItemIcon[] itemIcons =
    {
        new ItemIcon { itemName = "Hammer" },
        new ItemIcon { itemName = "Defibrillator" },
        new ItemIcon { itemName = "Injection" },
        new ItemIcon { itemName = "Pill" },
        new ItemIcon { itemName = "Bandage" },
    };

    [Header("Timing and health")]
    public float timeToFulfillNeed = 10f; // seconds to give the right item
    public float wrongItemDamage = 15f;   // HP lost for a wrong item or running out of time
    public float correctItemHeal = 10f;   // HP gained for the right item

    [Header("Needs (message -> correct item)")]
    public PatientNeed[] needs =
    {
        new PatientNeed { message = "I have a fever,",                  correctItem = "Injection" },
        new PatientNeed { message = "I am in pain,",                    correctItem = "Pill" },
        new PatientNeed { message = "I am bleeding,",                   correctItem = "Bandage" },
        new PatientNeed { message = "I have gone into cardiac arrest.", correctItem = "Defibrillator" },
        new PatientNeed { message = "Please check my reflexes,",        correctItem = "Hammer" },
    };

    private int currentNeedIndex = -1;
    private float timer;
    private bool hasActiveNeed = false;

    private PatientHealth patientHealth;
    private PatientStateManager stateManager;

    void Start()
    {
        patientHealth = GetComponent<PatientHealth>();
        stateManager = GetComponent<PatientStateManager>();

        HideMessage();

        if (needs == null || needs.Length == 0)
            Debug.LogWarning("PatientNeedsManager: the needs list is empty.", this);

        StartCoroutine(NeedLoop());
    }

    IEnumerator NeedLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f));

            // wait until the previous need is finished
            while (hasActiveNeed) yield return null;

            if (stateManager.CurrentState == PatientState.Dead) yield break;
            GenerateNewNeed();
        }
    }

    void GenerateNewNeed()
    {
        if (needs == null || needs.Length == 0) return;

        currentNeedIndex = Random.Range(0, needs.Length);
        PatientNeed need = needs[currentNeedIndex];
        ShowMessage(need.message, need.correctItem);

        hasActiveNeed = true;
        timer = timeToFulfillNeed;
        stateManager.SetState(PatientState.Angry);   // the patient gets angry while waiting
    }

    void Update()
    {
        if (!hasActiveNeed) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // time ran out
            patientHealth.ChangeHealth(-wrongItemDamage);
            EndNeed();
        }
    }

    // called by MedicalTool.Use(); returns false if the patient needs nothing right now
    public bool UseItem(string itemName)
    {
        if (!hasActiveNeed) return false;

        if (Normalize(itemName) == Normalize(needs[currentNeedIndex].correctItem))
            patientHealth.ChangeHealth(correctItemHeal);    // right item
        else
            patientHealth.ChangeHealth(-wrongItemDamage);   // wrong item

        EndNeed();
        return true;
    }

    static string Normalize(string s)
    {
        return s == null ? "" : s.Trim().ToLowerInvariant();
    }

    void EndNeed()
    {
        hasActiveNeed = false;
        HideMessage();

        if (stateManager.CurrentState != PatientState.Dead)
        {
            if (patientHealth.health < 40) stateManager.SetState(PatientState.Sick);
            else stateManager.SetState(PatientState.Happy);
        }
    }

    // ───────── message + item picture ─────────
    void ShowMessage(string text, string itemName)
    {
        if (messagePanel != null) messagePanel.SetActive(true);
        if (smsText != null)
        {
            smsText.text = messagePrefix + text;
            smsText.gameObject.SetActive(true);
        }
        ShowIcon(itemName);
    }

    void HideMessage()
    {
        if (smsText != null) smsText.gameObject.SetActive(false);
        ShowIcon(null);
        if (messagePanel != null) messagePanel.SetActive(false);
    }

    // shows only the picture of the needed item (null = hide all)
    void ShowIcon(string itemName)
    {
        if (itemIcons == null) return;
        string wanted = Normalize(itemName);
        for (int i = 0; i < itemIcons.Length; i++)
        {
            ItemIcon entry = itemIcons[i];
            if (entry == null || entry.icon == null) continue;
            entry.icon.SetActive(wanted.Length > 0 && Normalize(entry.itemName) == wanted);
        }
    }
}
