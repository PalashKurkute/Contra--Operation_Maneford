"""Turns a cutscene's recorded master into the video the game plays.

The shoot scene (Assets/Cutscenes/<Name>/<Name>_Shoot.unity) records
a ProRes master to Recordings/<Cutscene>_master.mov when you press
Play. This trims it to the cutscene's length, evens out the loudness
and encodes an H.264 MP4 that Unity's VideoPlayer plays everywhere.

Run from the repo root (needs ffmpeg on the PATH):
    python Tools/cutscene_encode.py Cutscene_Intro 24.2

Writes Assets/Cutscenes/Video/<Cutscene>.mp4.
"""

import os
import subprocess
import sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")


def main():
    if len(sys.argv) != 3:
        sys.exit("Usage: python Tools/cutscene_encode.py <Cutscene> <seconds>")

    name, seconds = sys.argv[1], sys.argv[2]
    master = os.path.join(ROOT, "Recordings", name + "_master.mov")
    out_dir = os.path.join(ROOT, "Assets", "Cutscenes", "Video")
    out = os.path.join(out_dir, name + ".mp4")

    if not os.path.exists(master):
        sys.exit("No master at " + master + " (press Play in the shoot scene first)")

    os.makedirs(out_dir, exist_ok=True)

    subprocess.run([
        "ffmpeg", "-y", "-v", "error", "-stats",
        "-i", master, "-t", seconds,
        # Picture: near-lossless for flat pixel art, a keyframe every
        # second so skipping and seeking stay cheap
        "-c:v", "libx264", "-preset", "slow", "-crf", "16",
        "-tune", "animation", "-profile:v", "high", "-level", "4.2",
        "-pix_fmt", "yuv420p", "-g", "60",
        # Sound: streaming loudness (-16 LUFS) with peaks kept clear
        "-af", "loudnorm=I=-16:TP=-1.5:LRA=11",
        "-c:a", "aac", "-b:a", "192k", "-ar", "48000",
        "-movflags", "+faststart",
        out,
    ], check=True)

    print("Wrote " + os.path.normpath(out) +
          " (%.1f MB)" % (os.path.getsize(out) / 1e6))


if __name__ == "__main__":
    main()
