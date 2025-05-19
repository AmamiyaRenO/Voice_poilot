using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Add UI namespace
using System;

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

        // 只在游戏开始时开始录音，如果还没开始的话
        if (!voiceProcessor.IsRecording)
        {
            try
        {
                Debug.Log("游戏开始时开始录音...");
                voiceProcessor.StartRecording();
            }
            catch (Exception e)
            {
                Debug.LogError($"开始录音时出错: {e.Message}\n{e.StackTrace}");
            }
        }
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
        Debug.Log("开始录音，进入校准模式");
        isCalibrating = true;
        calibrationMaxPeak = 0f;
        StartCoroutine(CalibrateThreshold());
    }

    private void OnRecordingStop()
    {
        Debug.Log("停止录音，重置校准状态");
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
                Debug.Log($"校准中 - 发现新的最大峰值: {newCalibrationPeak:F2} (之前: {calibrationMaxPeak:F2})");
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
        Debug.Log("开始校准过程，持续2秒...");
        float startTime = Time.time;
        while (Time.time - startTime < 2f)
        {
            float remainingTime = 2f - (Time.time - startTime);
            if (remainingTime % 0.5f < 0.1f)
            {
                Debug.Log($"校准剩余时间: {remainingTime:F1}秒");
            }
            yield return null;
        }
        // 使用原来的阈值计算方式
        threshold = calibrationMaxPeak * 0.9f;
        calibrated = true;
        isCalibrating = false;
        Debug.Log($"校准完成！\n最大峰值: {calibrationMaxPeak:F2}\n设置阈值: {threshold:F2}");
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

        // 只在组件启用且未录音时开始录音
        if (voiceProcessor != null && !voiceProcessor.IsRecording)
        {
            try
            {
                Debug.Log("Player组件启用时开始录音...");
                voiceProcessor.UpdateDevices(); // 再次确保设备已更新
                voiceProcessor.StartRecording();
            }
            catch (Exception e)
            {
                Debug.LogError($"启用时开始录音出错: {e.Message}\n{e.StackTrace}");
            }
        }
    }

    private void Update()
    {
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
        Vector3 rotation = transform.eulerAngles;
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
        if (other.gameObject.CompareTag("Obstacle")) {
            GameManager.Instance.GameOver();
        } else if (other.gameObject.CompareTag("Scoring")) {
            GameManager.Instance.IncreaseScore();
        }
    }
}
