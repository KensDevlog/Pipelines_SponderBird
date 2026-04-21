using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;


public class GameManager : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void JS_RecordSessionStart(string playerId);

    [DllImport("__Internal")]
    private static extern void JS_RecordSessionEnd(string playerId, float durationSeconds);

    private float _startTime;
    private string _playerId;
    
    public static GameManager Instance { get; private set; }

    public enum GameState { Idle, Playing, GameOver }
    public GameState State { get; private set; } = GameState.Idle;

    [Header("Scene References")]
    [SerializeField] private BirdController bird;
    [SerializeField] private PipeSpawner pipeSpawner;

    [Header("UI Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gameOverPanel;

    private Vector3 birdStartPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (bird == null) Debug.LogError("[GameManager] Bird reference not set!");
        if (pipeSpawner == null) Debug.LogError("[GameManager] PipeSpawner reference not set!");

        birdStartPosition = bird != null ? bird.transform.position : new Vector3(-3, 0, 0);
        
        _playerId = SystemInfo.deviceUniqueIdentifier; // or your own auth ID
        _startTime = Time.realtimeSinceStartup;
        
#if !UNITY_EDITOR
        JS_RecordSessionStart(_playerId);
#endif
    }

    void OnApplicationQuit()
    {
        float duration = Time.realtimeSinceStartup - _startTime;

#if !UNITY_EDITOR
        JS_RecordSessionEnd(_playerId, duration);
#endif

        ShowStartScreen();
    }

    private void Update()
    {
        if (State == GameState.Idle)
        {
            bool anyInput = Input.GetKeyDown(KeyCode.Space)
                         || Input.GetMouseButtonDown(0)
                         || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

            if (anyInput)
            {
                StartGame();
            }
        }
    }

    private void ShowStartScreen()
    {
        State = GameState.Idle;

        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (bird != null) bird.ResetBird(birdStartPosition);
        if (pipeSpawner != null) pipeSpawner.ClearAllPipes();
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
    }

    private void StartGame()
    {
        State = GameState.Playing;

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (bird != null) bird.Activate();
        if (pipeSpawner != null) pipeSpawner.SetSpawning(true);
    }

    public void OnBirdDied()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;

        if (pipeSpawner != null) pipeSpawner.SetSpawning(false);
        if (ScoreManager.Instance != null) ScoreManager.Instance.SaveHighScore();

        Invoke(nameof(ShowGameOver), 0.6f);
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
