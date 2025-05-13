using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Add UI namespace

public class Player : MonoBehaviour
{
    public Sprite[] sprites;
    public float strength = 5f;
    public float gravity = -9.81f;
    public float tilt = 5f;
    
    // Microphone related parameters
    public float sensitivity = 100f;        // Sound sensitivity
    public float threshold = 0.1f;          // Sound threshold
    public float cooldownTime = 0.2f;       // Jump cooldown time
    private bool canJump = true;            // Whether the bird can jump
    private AudioClip microphoneInput;      // Microphone input
    private bool microphoneInitialized;     // Whether the microphone is initialized
    private string selectedDevice;          // Selected microphone device

    private SpriteRenderer spriteRenderer;
    private Vector3 direction;
    private int spriteIndex;

    // Add UI display component
    public Text volumeText; // UI text for displaying volume
    private float maxVolume = 0f; // Record the maximum volume for debugging

    private float environmentNoise = 0f;
    private bool calibrated = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        InitializeMicrophone();
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            selectedDevice = Microphone.devices[0];
            // Change sampling parameters, increase sampling rate
            microphoneInput = Microphone.Start(selectedDevice, true, 1, 48000);
            microphoneInitialized = true;
            Debug.Log("Microphone initialized: " + selectedDevice);
            Debug.Log("Microphone sample rate: " + microphoneInput.frequency);
            Debug.Log("Microphone channels: " + microphoneInput.channels);
        }
        else
        {
            Debug.LogWarning("No microphone device detected!");
        }
    }

    private void OnDestroy()
    {
        if (microphoneInitialized)
        {
            Microphone.End(selectedDevice);
        }
    }

    private (float, float) GetMicrophoneVolume()
    {
        if (!microphoneInitialized) return (0f, 0f);

        int sampleSize = 256; // Moderate window
        float[] samples = new float[sampleSize];
        int micPosition = Microphone.GetPosition(selectedDevice);

        if (micPosition < sampleSize)
            return (0f, 0f);

        microphoneInput.GetData(samples, micPosition - sampleSize);

        float sum = 0f;
        float maxSample = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float absSample = Mathf.Abs(samples[i]);
            sum += absSample;
            maxSample = Mathf.Max(maxSample, absSample);
        }

        float averageVolume = sum / samples.Length * sensitivity;
        maxVolume = Mathf.Max(maxVolume, averageVolume);

        if (volumeText != null)
        {
            volumeText.text = $"Current Volume: {averageVolume:F2}\nMax Volume: {maxVolume:F2}\nThreshold: {threshold:F2}";
        }

        return (averageVolume, maxSample * sensitivity);
    }

    private IEnumerator JumpCooldown()
    {
        canJump = false;
        yield return new WaitForSeconds(cooldownTime);
        canJump = true;
    }

    private IEnumerator CalibrateThreshold()
    {
        float maxPeak = 0f;
        float startTime = Time.time;
        while (Time.time - startTime < 2f)
        {
            (_, float peak) = GetMicrophoneVolume();
            maxPeak = Mathf.Max(maxPeak, peak);
            yield return null;
        }
        threshold = maxPeak + 0.05f; // Environment max noise + 0.05
        calibrated = true;
        Debug.Log($"Auto set threshold: {threshold:F2}");
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
        StartCoroutine(CalibrateThreshold());
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
        if (!calibrated) return;
        (float volume, float peak) = GetMicrophoneVolume();

        // Only accept clapping: high peak and high peak/average ratio, or extremely high peak
        if (((peak > threshold && peak > 2 * volume) || peak > threshold * 2) && canJump)
        {
            Debug.Log($"Jump triggered! Peak: {peak:F2}, Avg: {volume:F2}");
            direction = Vector3.up * strength;
            StartCoroutine(JumpCooldown());
        }
        // Keep original keyboard and mouse input as backup
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) {
            direction = Vector3.up * strength;
        }
        // Apply gravity and update the position
        direction.y += gravity * Time.deltaTime;
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
