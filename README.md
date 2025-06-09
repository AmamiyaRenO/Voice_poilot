# Voice-Controlled Flappy Bird

This Unity 2020.3 project builds on [Zigurous' tutorial](https://github.com/zigurous/unity-flappy-bird-tutorial) and adds voice recognition and clap detection.

## Features
- **Speech recognition**: Uses the Vosk library. The `StreamingAssets` folder includes `vosk-model-small-en-us-0.15.zip`.
- **Clap/sound jump**: A loud clap or shout will make the bird flap.
- **Voice commands**: Say "easy mode", "normal mode" or "hard mode" to change the difficulty, or say "rush" to trigger a dash.
- **RUSH letters**: Collect the letters R, U, S and H during gameplay to activate the rush ability.

## Quick Start
1. Open the project with Unity 2020.3 or later.
2. Load `Assets/Scenes/Flappy Bird.unity`.
3. Ensure your microphone is available and follow the calibration prompts at startup.
4. Clap to flap, and try the voice commands or collect letters to test additional features.
