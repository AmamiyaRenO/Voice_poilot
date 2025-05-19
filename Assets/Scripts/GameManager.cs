using UnityEngine;
using UnityEngine.UI;

public enum Difficulty { Easy, Normal, Hard }

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Player player;
    [SerializeField] public Spawner spawner;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject gameOver;

    public int score { get; private set; } = 0;

    // 难度相关参数
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;
    public float easyGap = 20.0f;
    public float normalGap = 3.0f;
    public float hardGap = 2.0f;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }
        SetDifficulty(Difficulty.Normal); // 默认普通难度
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        Pause();
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
    }

    public void Play()
    {
        score = 0;
        scoreText.text = score.ToString();

        playButton.SetActive(false);
        gameOver.SetActive(false);

        Time.timeScale = 1f;
        player.enabled = true;

        Pipes[] pipes = FindObjectsOfType<Pipes>();

        for (int i = 0; i < pipes.Length; i++) {
            Destroy(pipes[i].gameObject);
        }
    }

    public void GameOver()
    {
        playButton.SetActive(true);
        gameOver.SetActive(true);

        Pause();
    }

    public void IncreaseScore()
    {
        score++;
        scoreText.text = score.ToString();
    }

    public void SetDifficulty(Difficulty diff)
    {
        CurrentDifficulty = diff;
        float gap = normalGap;
        switch (diff)
        {
            case Difficulty.Easy:
                gap = easyGap;
                break;
            case Difficulty.Normal:
                gap = normalGap;
                break;
            case Difficulty.Hard:
                gap = hardGap;
                break;
        }
        if (spawner != null)
        {
            spawner.SetGap(gap);
        }
        Debug.Log($"难度切换为: {diff}, gap={gap}");

        // 清空所有已生成的障碍物
        Pipes[] pipes = FindObjectsOfType<Pipes>();
        for (int i = 0; i < pipes.Length; i++) {
            Destroy(pipes[i].gameObject);
        }
    }
}
