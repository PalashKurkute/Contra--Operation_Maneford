# Placeholder voiceover for Cutscene_Ending (the General, Rook and
# Tala at the crash site at dawn), made with the Windows text-to-
# speech voices until real actors record it.
#
# Run from the repo root (Windows PowerShell):
#     powershell -File Tools/cutscene_ending_vo.ps1
#
# Writes raw takes to Temp/CutsceneVO/. Then run
# Tools/cutscene_ending_audio.py, which trims them, places them in
# the open field and writes the final lines to
# Assets/Cutscenes/Ending/Audio/. To swap in real recordings, put
# en_gen_01, en_rook_01, en_rook_02, en_gen_02 and en_tala_01 (.wav,
# any rate, mono or stereo) in Temp/CutsceneVO/ and rerun the Python
# script.

$out = Join-Path $PSScriptRoot "..\Temp\CutsceneVO"
New-Item -ItemType Directory -Force $out | Out-Null

Add-Type -AssemblyName System.Speech

# The General is beaten and tired now; Rook flat and quiet; Tala calm
$general = 'rate="-18%" pitch="-14%"'
$rook = 'rate="-8%" pitch="-4%"'
$tala = 'rate="-12%" pitch="-10%"'

$lines = @(
    @{ name = "en_gen_01"; voice = "Microsoft David Desktop"; prosody = $general;
       ssml = 'Go on, then. <break time="350ms"/> Finish it.' },
    @{ name = "en_rook_01"; voice = "Microsoft David Desktop"; prosody = $rook;
       ssml = 'Nobody''s finishing anything.' },
    @{ name = "en_rook_02"; voice = "Microsoft David Desktop"; prosody = $rook;
       ssml = 'They were never your army. <break time="400ms"/> They were just lions.' },
    @{ name = "en_gen_02"; voice = "Microsoft David Desktop"; prosody = 'rate="-25%" pitch="-16%"';
       ssml = 'They were the only ones <break time="450ms"/> who ever bowed.' },
    @{ name = "en_tala_01"; voice = "Microsoft Zira Desktop"; prosody = $tala;
       ssml = 'It''s over, <break time="200ms"/> Leopold.' }
)

foreach ($line in $lines) {
    $voice = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $voice.SelectVoice($line.voice)

    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(
        44100,
        [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,
        [System.Speech.AudioFormat.AudioChannel]::Mono
    )

    $voice.SetOutputToWaveFile((Join-Path $out ($line.name + ".wav")), $format)
    $voice.SpeakSsml(
        '<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="en-US">' +
        '<prosody ' + $line.prosody + '>' + $line.ssml + '</prosody></speak>'
    )

    $voice.Dispose()
}

Write-Host "Wrote raw voiceover to $out"
