# Beat analysis source data

The `.beats` files in `results/` are source data for the Unity song assets. They were generated
offline with [CPJKU/Beat This!](https://github.com/CPJKU/beat_this) `final0`, without DBN
postprocessing. Beat This code and its published model weights use the MIT license. The tool and
its Python environment are not shipped in the Unity player.

Each result row is the standard Beat This TSV format:

```text
seconds<TAB>beat-in-bar
```

`beat-in-bar` starts at `1` for a detected downbeat and increments until the next downbeat. Unity
imports every timestamp and detected bar length as timing data, so expressive timing, compound
meters, and tempo changes do not have to be reduced to one global BPM.

## Rebuild the environment

Use Python 3.12 on Windows. The tested dependency set is recorded in `requirements-lock.txt`.

```powershell
python -m venv beat-this-env
beat-this-env\Scripts\python.exe -m pip install -r Tools\BeatAnalysis\requirements-lock.txt
beat-this-env\Scripts\python.exe -c "import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())"
```

Convert each MP3 to 22.05 kHz mono WAV using the printed FFmpeg executable, then run:

```powershell
beat-this-env\Scripts\beat_this.exe input.wav --model final0 --gpu -1 -o output.beats
```

Do not add `--dbn`: that path adds madmom and its separately restricted model dependencies.

## Provenance

| Item | SHA-256 |
|---|---|
| `象王行（特别版）.mp3` | `922E8F129DBC8304A59B7BC87D4F2499C94D8993FF258B068990721FD0CA8F7D` |
| `青玉案·兰芥.mp3` | `E7FB17E6E1C62A17C9FA40874CF40C2654B76E09647B4F40A3035217BF45FF49` |
| `升龙诀.mp3` | `8FC5C5C7C10415F78CFDAE8B97BDA27C86C3071BB84B8B625D520EEC02248688` |
| `beat_this-final0.ckpt` | `8C328B45F59D8DD3DFF219253FF6A8D6482BE57D0133A29140E2FEBBF8EB8331` |

Observed source data:

| Song | First downbeat | Detected beats | Last detected beat | Interpretation |
|---|---:|---:|---:|---|
| 象王行（特别版） | 0.86 s | 399 | 170.52 s | Main pulse is approximately 140 BPM; ending contains broader spacing and a music tail. |
| 青玉案·兰芥 | 0.52 s | 382 | 223.82 s | Pulse changes between broad and subdivided motion; keep the timestamp grid instead of assigning one BPM. |
| 升龙诀 | 0.48 s | 365 | 145.16 s | The leading pickup before the first detected downbeat is excluded from the playable beat grid. |

These results remain `AnalysisCandidate` until the beat grid and playable entry point are checked
by listening during chart authoring.
