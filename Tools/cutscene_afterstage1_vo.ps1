# Placeholder voiceover for Cutscene_AfterStage1 (the General over
# the base loudspeakers), made with the Windows text-to-speech voice
# until a real actor records it.
#
# Run from the repo root (Windows PowerShell):
#     powershell -File Tools/cutscene_afterstage1_vo.ps1
#
# Writes raw takes to Temp/CutsceneVO/. Then run
# Tools/cutscene_afterstage1_audio.py, which trims them, lowers
# them, puts them through the loudspeakers and writes the final
# lines to Assets/Cutscenes/AfterStage1/Audio/. To swap in real
# recordings, put gen_01.wav..gen_03.wav (any sample rate, mono or
# stereo) in Temp/CutsceneVO/ and rerun the Python script.

$out = Join-Path $PSScriptRoot "..\Temp\CutsceneVO"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Speech

# Lines from STORY_AND_GAME_STRUCTURE.md, section 7.1
$lines = @(
    @{ name = "gen_01"; ssml = 'He <emphasis level="strong">freed</emphasis> my lions?' },
    @{ name = "gen_02"; ssml = '<prosody rate="-25%" pitch="-8%">Fine.</prosody>' },
    @{ name = "gen_03"; ssml = 'Release <break time="150ms"/> the <emphasis level="moderate">soldiers.</emphasis>' }
)

foreach ($line in $lines) {
    $voice = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $voice.SelectVoice("Microsoft David Desktop")

    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(
        44100,
        [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,
        [System.Speech.AudioFormat.AudioChannel]::Mono
    )

    $voice.SetOutputToWaveFile((Join-Path $out ($line.name + ".wav")), $format)

    # The General speaks slowly and grandly, like a king
    $voice.SpeakSsml(
        '<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">' +
        '<prosody rate="-12%" pitch="-12%">' + $line.ssml + '</prosody></speak>'
    )

    $voice.Dispose()
}

Write-Host "Wrote raw voiceover to $out"
