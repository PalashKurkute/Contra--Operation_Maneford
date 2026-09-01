# Placeholder voiceover for Cutscene_HelicopterEscape (the General,
# climbing out of the wreck and leaving by helicopter), made with the Windows text-to-speech voice
# until a real actor records it.
#
# Run from the repo root (Windows PowerShell):
#     powershell -File Tools/cutscene_heliescape_vo.ps1
#
# Writes raw takes to Temp/CutsceneVO/. Then run
# Tools/cutscene_heliescape_audio.py, which trims them, lowers
# them, puts them in the room and writes the final
# lines to Assets/Cutscenes/HeliEscape/Audio/. To swap in real
# recordings, put he_gen_01 and he_gen_02 (.wav, any rate, mono or
# stereo) in Temp/CutsceneVO/ and rerun the Python script.

$out = Join-Path $PSScriptRoot "..\Temp\CutsceneVO"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Speech

# Lines from STORY_AND_GAME_STRUCTURE.md, section 7 (Cutscene_HelicopterEscape)
$lines = @(
    @{ name = "he_gen_01"; ssml = '<prosody rate="+5%">This is <emphasis level="strong">not</emphasis> over! <break time="200ms"/> The King <emphasis level="strong">will</emphasis> return!</prosody>' },
    @{ name = "he_gen_02"; ssml = 'After a <emphasis level="moderate">short</emphasis> break!' }
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

    # The General, slow and grand even while fleeing
    $prosody = 'rate="-12%" pitch="-12%"'
    $voice.SpeakSsml(
        '<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">' +
        '<prosody ' + $prosody + '>' + $line.ssml + '</prosody></speak>'
    )

    $voice.Dispose()
}

Write-Host "Wrote raw voiceover to $out"
