# Placeholder voiceover for Cutscene_AfterStage2 (the General and
# Dobbs in the control room), made with the Windows text-to-speech voice
# until a real actor records it.
#
# Run from the repo root (Windows PowerShell):
#     powershell -File Tools/cutscene_afterstage2_vo.ps1
#
# Writes raw takes to Temp/CutsceneVO/. Then run
# Tools/cutscene_afterstage2_audio.py, which trims them, lowers
# them, puts them in the room and writes the final
# lines to Assets/Cutscenes/AfterStage2/Audio/. To swap in real
# recordings, put s2_gen_01, s2_dobbs_01 and s2_gen_02 (.wav, any rate, mono or
# stereo) in Temp/CutsceneVO/ and rerun the Python script.

$out = Join-Path $PSScriptRoot "..\Temp\CutsceneVO"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Speech

# Lines from STORY_AND_GAME_STRUCTURE.md, section 7 (Cutscene_AfterStage2)
$lines = @(
    @{ name = "s2_gen_01"; ssml = 'Wake <break time="120ms"/> the <emphasis level="strong">Mecha-Lion.</emphasis>'; dobbs = $false },
    @{ name = "s2_dobbs_01"; ssml = 'Sir, <break time="150ms"/> about the fuel gauge...'; dobbs = $true },
    @{ name = "s2_gen_02"; ssml = '<prosody rate="+10%">Not now, <emphasis level="strong">Dobbs!</emphasis></prosody>'; dobbs = $false }
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

    # The General speaks slowly and grandly, like a king; Dobbs
    # quick, high and nervous
    $prosody = if ($line.dobbs) { 'rate="+8%" pitch="+30%"' } else { 'rate="-12%" pitch="-12%"' }
    $voice.SpeakSsml(
        '<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">' +
        '<prosody ' + $prosody + '>' + $line.ssml + '</prosody></speak>'
    )

    $voice.Dispose()
}

Write-Host "Wrote raw voiceover to $out"
