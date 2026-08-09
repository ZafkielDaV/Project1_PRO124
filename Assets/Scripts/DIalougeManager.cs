using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive = false;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        // Vẫn giữ phím E để chuyển sang câu thoại tiếp theo trong lúc đang ở trong vùng trigger
        if (isDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            NextLine();
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] DialogueData rỗng hoặc chưa gán.");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        var line = currentDialogue.lines[currentLineIndex];
        nameText.text = line.speakerName;
        contentText.text = line.content;
    }

    void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Length)
            EndDialogue();
        else
            ShowCurrentLine();
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
    }

    // Gọi hàm này từ NPCDialogueTrigger khi player rời khỏi vùng trigger,
    // để tắt hội thoại ngay lập tức dù đang ở câu thoại nào
    public void EndDialogueFromTrigger()
    {
        if (!isDialogueActive) return;
        EndDialogue();
    }
}