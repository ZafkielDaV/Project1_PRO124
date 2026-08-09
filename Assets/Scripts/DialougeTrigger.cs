using UnityEngine;

public class NPCDialogueTrigger : MonoBehaviour
{
    public DialogueData dialogueData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger Enter với: " + other.gameObject.name + " | Tag: " + other.tag);

        if (!other.CompareTag("Player")) return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("DialogueManager.Instance đang NULL! Kiểm tra DialogueManager có active trong scene không.");
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogueData);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("Trigger Exit với: " + other.gameObject.name + " | Tag: " + other.tag);

        if (!other.CompareTag("Player")) return;

        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.EndDialogueFromTrigger();
    }
}