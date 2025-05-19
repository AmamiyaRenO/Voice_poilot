using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class VoskDialogText : MonoBehaviour 
{
    public VoskSpeechToText VoskSpeechToText;
    public Text DialogText;

	Regex forward_regex = new Regex(@"forward");
	Regex backward_regex = new Regex(@"backward");
	Regex left_regex = new Regex(@"left");
	Regex right_regex = new Regex(@"right");

	int posX = 0;
	int posY = 0;

    void Awake()
    {
        VoskSpeechToText.OnTranscriptionResult += OnTranscriptionResult;
		ResetState();
    }

	void ResetState()
	{

	}


	void Say(string response)
	{
		string command = $"Add-Type –AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak('{response}')";
		System.Diagnostics.Process.Start("powershell", $"-Command \"{command}\"");
	}

	void AddFinalResponse(string response) {
		Say(response);
		DialogText.text = response + "\n";
		ResetState();
	}

	void AddResponse(string response) {
        Say(response);
		DialogText.text = response + "\n";
		DialogText.text += $"Current Position: ({posX}, {posY})\n";
	}

    private void OnTranscriptionResult(string obj)
    {
		// Save to file

        Debug.Log(obj);
        var result = new RecognitionResult(obj);
        foreach (RecognizedPhrase p in result.Phrases)
        {

			if (forward_regex.IsMatch(p.Text)) {
				posY += 1;
				AddResponse("Moved forward.");
				return;
			}
		
			if (backward_regex.IsMatch(p.Text)) {
				posY -= 1;
				AddResponse("Moved backward.");
				return;
			}

			if (left_regex.IsMatch(p.Text)) {
				posX -= 1;
				AddResponse("Moved left.");
				return;
			}

			if (right_regex.IsMatch(p.Text)) {
				posX += 1;
				AddResponse("Moved right.");
				return;
			}
        }
		if (result.Phrases.Length > 0 && result.Phrases[0].Text != "") {
			AddResponse("Command not recognized.");
		}
    }
}
