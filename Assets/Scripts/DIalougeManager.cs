using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;

    [Header("Input")]
    public KeyCode nextLineKey = KeyCode.E;
    public KeyCode closeDialogueKey = KeyCode.J; // bấm J để tắt dialogue

    [Header("Auto-hide UI")]
    public string uiTag = "UI"; // tag của các UI cần ẩn khi hội thoại kích hoạt

    private DialogueData currentDialogue;
    private int currentLineIndex;
    private bool isDialogueActive = false;

    // Cho các script khác (VD: PlayerMovement) đọc trạng thái để chặn di chuyển
    public static bool IsDialogueActive => Instance != null && Instance.isDialogueActive;

    // Lưu lại những UI đã bị ẩn bởi hệ thống, để chỉ bật lại đúng những cái đó
    private List<GameObject> hiddenUIObjects = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (!isDialogueActive) return;

        // Vẫn giữ phím E để chuyển sang câu thoại tiếp theo trong lúc đang ở trong vùng trigger
        if (Input.GetKeyDown(nextLineKey))
        {
            NextLine();
        }

        // Bấm J: tắt dialogue ngay lập tức dù đang ở câu thoại nào (đồng thời cho phép di chuyển lại)
        if (Input.GetKeyDown(closeDialogueKey))
        {
            CloseDialogueUI();
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

        HideOtherUI();

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

        ShowOtherUI();
    }

    // Gọi hàm này từ NPCDialogueTrigger khi player rời khỏi vùng trigger,
    // để tắt hội thoại ngay lập tức dù đang ở câu thoại nào
    public void EndDialogueFromTrigger()
    {
        if (!isDialogueActive) return;
        EndDialogue();
    }

    // Cho phép tắt UI hội thoại bất cứ lúc nào (gọi từ Update, hoặc từ nút UI, script khác...)
    public void CloseDialogueUI()
    {
        if (!isDialogueActive) return;
        EndDialogue();
    }

    // Ẩn tất cả GameObject có tag uiTag (trừ dialoguePanel), lưu lại để bật lại sau
    void HideOtherUI()
    {
        hiddenUIObjects.Clear();

        GameObject[] uiObjects = GameObject.FindGameObjectsWithTag(uiTag);
        foreach (var obj in uiObjects)
        {
            if (obj == dialoguePanel) continue; // không ẩn chính panel hội thoại
            if (!obj.activeSelf) continue;      // đã tắt sẵn từ trước thì bỏ qua, không cần bật lại sau

            obj.SetActive(false);
            hiddenUIObjects.Add(obj);
        }
    }

    // Bật lại đúng những UI đã bị ẩn bởi HideOtherUI()
    void ShowOtherUI()
    {
        foreach (var obj in hiddenUIObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        hiddenUIObjects.Clear();
    }
}