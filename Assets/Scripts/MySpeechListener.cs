using UnityEngine;

public class MySpeechListener : MonoBehaviour
{
    public VoskSpeechToText vosk;

    void Awake()
    {
        vosk.OnTranscriptionResult += OnSpeech;
    }

    void OnSpeech(string json)
    {
        vosk.HandleDifficultyCommand(json);

        var result = new RecognitionResult(json);
        foreach (var phrase in result.Phrases)
        {
            Debug.Log("识别到语音命令: " + phrase.Text);
            // 这里可以做你自己的逻辑，比如触发角色动作
        }
    }
}