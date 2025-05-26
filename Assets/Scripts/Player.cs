using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Add UI namespace
using System;
using System.Collections.Generic; // 文件顶部已引入则不用重复

public class Player : MonoBehaviour
{
    public Sprite[] sprites;
    public float strength = 5f;
    public float gravity = -9.81f;
    public float tilt = 5f;
    
    // VoiceProcessor reference
    public VoiceProcessor voiceProcessor;
    
    // Microphone related parameters
    public float sensitivity = 100f;        // Sound sensitivity
    public float threshold = 0.1f;          // Sound threshold
    public float cooldownTime = 0.2f;       // Jump cooldown time
    private bool canJump = true;            // Whether the bird can jump
    private bool calibrated = false;        // Whether the microphone is calibrated

    private SpriteRenderer spriteRenderer;
    private Vector3 direction;
    private int spriteIndex;

    // Add UI display component
    public Text volumeText; // UI text for displaying volume
    private float maxVolume = 0f; // Record the maximum volume for debugging

    private float calibrationMaxPeak = 0f;
    private bool isCalibrating = false;

    public List<char> collectedLetters = new List<char>();

    public bool isRushing = false;
    public float rushDuration = 1.5f; // 冲刺持续时间
    public float rushSpeed = 15f;     // 冲刺速度
    private float rushEndTime = 0f;

    public static float RushWorldSpeedMultiplier = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (voiceProcessor == null)
        {
            // 优先查找VoskManager上的VoiceProcessor
            var voskManager = FindObjectOfType<VoskSpeechToText>();
            if (voskManager != null && voskManager.VoiceProcessor != null)
            {
                voiceProcessor = voskManager.VoiceProcessor;
                Debug.Log("使用VoskManager上的VoiceProcessor");
            }
            else
            {
                voiceProcessor = FindObjectOfType<VoiceProcessor>();
                if (voiceProcessor == null)
                {
                    Debug.LogError("VoiceProcessor not found! Please assign it in the inspector.");
                    return;
                }
                Debug.Log("使用独立的VoiceProcessor");
            }
        }

        // 确保VoiceProcessor已初始化设备
        voiceProcessor.UpdateDevices();
        
        // Subscribe to VoiceProcessor events
        voiceProcessor.OnFrameCaptured += OnAudioFrameCaptured;
        voiceProcessor.OnRecordingStart += OnRecordingStart;
        voiceProcessor.OnRecordingStop += OnRecordingStop;
    }

    private void OnDestroy()
    {
        if (voiceProcessor != null)
        {
            voiceProcessor.OnFrameCaptured -= OnAudioFrameCaptured;
            voiceProcessor.OnRecordingStart -= OnRecordingStart;
            voiceProcessor.OnRecordingStop -= OnRecordingStop;
        }
    }

    private void OnRecordingStart()
    {
        //Debug.Log("开始录音，进入校准模式");
        isCalibrating = true;
        calibrationMaxPeak = 0f;
        StartCoroutine(CalibrateThreshold());
    }

    private void OnRecordingStop()
    {
        //Debug.Log("停止录音，重置校准状态");
        calibrated = false;
    }

    private void OnAudioFrameCaptured(short[] samples)
    {
        if (isCalibrating)
        {
            // 在校准期间，只更新最大峰值
            float calibrationSample = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float normalizedSample = samples[i] / (float)short.MaxValue;
                float absSample = Mathf.Abs(normalizedSample);
                calibrationSample = Mathf.Max(calibrationSample, absSample);
            }
            float newCalibrationPeak = calibrationSample * sensitivity;
            if (newCalibrationPeak > calibrationMaxPeak)
            {
                //Debug.Log($"校准中 - 发现新的最大峰值: {newCalibrationPeak:F2} (之前: {calibrationMaxPeak:F2})");
                calibrationMaxPeak = newCalibrationPeak;
            }
            
            // 在校准期间也显示音量
            if (volumeText != null)
            {
                volumeText.text = $"Calibrating...\nCurrent Peak: {calibrationSample * sensitivity:F2}\nMax Peak: {calibrationMaxPeak:F2}";
            }
            return;
        }

        // 计算音量
        float sum = 0f;
        float maxSample = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float normalizedSample = samples[i] / (float)short.MaxValue;
            float absSample = Mathf.Abs(normalizedSample);
            sum += absSample;
            maxSample = Mathf.Max(maxSample, absSample);
        }

        float averageVolume = sum / samples.Length * sensitivity;
        float peakVolume = maxSample * sensitivity;
        maxVolume = Mathf.Max(maxVolume, averageVolume);

        if (volumeText != null)
        {
            string status = calibrated ? "Ready" : "Not Calibrated";
            volumeText.text = $"Status: {status}\nCurrent Volume: {averageVolume:F2}\nPeak Volume: {peakVolume:F2}\nMax Volume: {maxVolume:F2}\nThreshold: {threshold:F2}";
        }

        // 使用原来的鼓掌检测逻辑
        bool isClap = (peakVolume > threshold && peakVolume > averageVolume * 1.5f) || peakVolume > threshold * 1.5f;
        if (isClap && canJump && calibrated)
        {
            direction = Vector3.up * strength;
            StartCoroutine(JumpCooldown());
        }
    }

    private IEnumerator JumpCooldown()
    {
        canJump = false;
        yield return new WaitForSeconds(cooldownTime);
        canJump = true;
    }

    private IEnumerator CalibrateThreshold()
    {
        //Debug.Log("开始校准过程，持续2秒...");
        float startTime = Time.time;
        while (Time.time - startTime < 2f)
        {
            float remainingTime = 2f - (Time.time - startTime);
            if (remainingTime % 0.5f < 0.1f)
            {
                //Debug.Log($"校准剩余时间: {remainingTime:F1}秒");
            }
            yield return null;
        }
        // 使用原来的阈值计算方式
        threshold = calibrationMaxPeak * 0.9f;
        calibrated = true;
        isCalibrating = false;
        //Debug.Log($"校准完成！\n最大峰值: {calibrationMaxPeak:F2}\n设置阈值: {threshold:F2}");
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
    }

    private void OnEnable()
    {
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
        direction = Vector3.zero;
    }

    private void Update()
    {
        Vector3 rotation = transform.eulerAngles; // 只声明一次

        // rush期间不再让小鸟自己向右冲刺，只需要无敌和世界加速
        if (isRushing)
        {
            direction = Vector3.zero;
            rotation.z = 0f;
            transform.eulerAngles = rotation;
            // 不再有 transform.position += Vector3.right * rushSpeed * Time.deltaTime;
            // 让小鸟保持水平即可
            // 不return，继续执行后续逻辑（如重力、跳跃等可以根据需要保留或屏蔽）
        }

        // Keep original keyboard and mouse input as backup
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) {
            direction = Vector3.up * strength;
        }
        
        // Apply gravity and update the position
        if (!isCalibrating)  // 只在非校准期间应用重力
        {
            direction.y += gravity * Time.deltaTime;
        }
        else  // 在校准期间保持水平飞行
        {
            direction.y = 0f;
        }
        transform.position += direction * Time.deltaTime;
        
        // Tilt the bird based on the direction
        rotation.z = direction.y * tilt;
        transform.eulerAngles = rotation;
    }

    private void AnimateSprite()
    {
        spriteIndex++;

        if (spriteIndex >= sprites.Length) {
            spriteIndex = 0;
        }

        if (spriteIndex < sprites.Length && spriteIndex >= 0) {
            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isRushing) return; // 冲刺期间无敌，直接忽略碰撞

        if (other.gameObject.CompareTag("Obstacle")) {
            GameManager.Instance.GameOver();
        } else if (other.gameObject.CompareTag("Scoring")) {
            GameManager.Instance.IncreaseScore();
        }
    }

    public void CollectLetter(char letter)
    {
        if (!collectedLetters.Contains(letter))
        {
            collectedLetters.Add(letter);
            Debug.Log("收集到字母: " + letter);
            // TODO: 更新UI

            // 通知Spawner生成下一个字母
            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
            {
                spawner.NextRushLetter();
            }
        }
    }

    public void Rush()
    {
        if (collectedLetters.Contains('R') && collectedLetters.Contains('U') &&
            collectedLetters.Contains('S') && collectedLetters.Contains('H') && !isRushing)
        {
            Debug.Log("RUSH技能发动！");
            StartCoroutine(RushCoroutine());
            collectedLetters.Clear();
            // 通知Spawner重置rush字母
            Spawner spawner = FindObjectOfType<Spawner>();
            if (spawner != null)
            {
                spawner.ResetRushLetter();
            }
        }
    }

    private IEnumerator RushCoroutine()
    {
        isRushing = true;
        RushWorldSpeedMultiplier = 3f; // rush期间世界加速3倍
        rushEndTime = Time.time + rushDuration;
        // 让小鸟无敌
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 你可以加特效/音效等
        yield return new WaitForSeconds(rushDuration);

        isRushing = false;
        if (col != null) col.enabled = true;
        RushWorldSpeedMultiplier = 1f; // 恢复正常
    }
}
