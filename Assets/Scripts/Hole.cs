using UnityEngine;
using UnityEngine.SceneManagement;

// Gắn script này vào GameObject có Collider2D (bật Is Trigger) đóng vai trò cái "hố"
public class Hole : MonoBehaviour
{
    [Header("Scene chuyển tới")]
    [SerializeField] private string nextSceneName = "Map2";
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        SceneManager.LoadScene(nextSceneName);
    }
}