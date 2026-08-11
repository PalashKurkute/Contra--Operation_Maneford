# Placeholder voiceover for Cutscene_Intro (Pip), made with the
# Windows text-to-speech voice until a real actor records it.
#
# Run from the repo root (Windows PowerShell):
#     powershell -File Tools/cutscene_intro_vo.ps1
#
# Writes raw takes to Temp/CutsceneVO/. Then run
# Tools/cutscene_intro_audio.py, which trims them, adds the radio
# sound and writes the final lines to Assets/Cutscenes/Intro/Audio/.
# To swap in real recordings, put pip_01.wav..pip_04.wav (any
# sample rate, mono or stereo) in Temp/CutsceneVO/ and rerun the
# Python script.

$out = Join-Path $PSScriptRoot "..\Temp\CutsceneVO"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Speech

# Lines from STORY_AND_GAME_STRUCTURE.md, section 7.1
$lines = @(
    @{ name = "pip_01"; ssml = 'Day nine. <break time="250ms"/> The Maneford Reserve stopped answering our calls.' },
    @{ name = "pip_02"; ssml = 'We sent drones.' },
    @{ name = "pip_03"; ssml = 'They came back with <break time="550ms"/> questions.' },
    @{ name = "pip_04"; ssml = 'So now <break time="120ms"/> we''re sending Rook.' }
)

foreach ($line in $lines) {
    $voice = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $voice.SelectVoice("Microsoft Zira Desktop")

    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(
        44100,
        [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,
        [System.Speech.AudioFormat.AudioChannel]::Mono
    )

    $voice.SetOutputToWaveFile((Join-Path $out ($line.name + ".wav")), $format)

    # Pip talks fast and a little high
    $voice.SpeakSsml(
        '<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">' +
        '<prosody rate="+8%" pitch="+6%">' + $line.ssml + '</prosody></speak>'
    )

    $voice.Dispose()
}

Write-Host "Wrote raw voiceover to $out"
