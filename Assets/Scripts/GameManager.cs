using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Save Point")]
    // true  = khi vào scene, Player sẽ load vị trí save point gần nhất
    // false = vào scene ở vị trí mặc định (spawn gốc), không dùng save point
    private bool useSavePoint = false;

    [Header("Scene")]
    // Lưu tên scene hiện tại để reload lại đúng scene đó
    private string currentSceneName;

    void Awake()
    {
        // ---- Singleton: chỉ giữ 1 GameManager duy nhất xuyên suốt các scene ----
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
    }

    // ---- Player.cs gọi hàm này trong Start() để quyết định có load save point hay không ----
    public bool ShouldUseSavePoint()
    {
        return useSavePoint;
    }

    // Dùng khi chuyển scene bình thường (ví dụ qua cửa/cổng) và MUỐN giữ lại vị trí save point
    public void ReloadSceneWithSavePoint()
    {
        useSavePoint = true;

        string sceneToLoad = string.IsNullOrEmpty(currentSceneName)
            ? SceneManager.GetActiveScene().name
            : currentSceneName;

        SceneManager.LoadScene(sceneToLoad);
    }

    // Chuyển sang 1 scene khác theo tên, có thể chọn có dùng save point hay không
    public void LoadScene(string sceneName, bool loadFromSavePoint = true)
    {
        useSavePoint = loadFromSavePoint;
        SceneManager.LoadScene(sceneName);
    }
}