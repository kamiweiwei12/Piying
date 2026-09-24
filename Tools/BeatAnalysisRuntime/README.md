# Runtime beat analyzer

The Windows player bundles a pinned build of
[mosynthkey/beat_this_cpp](https://github.com/mosynthkey/beat_this_cpp) at commit
`07ab790a9ec2eda8093d52d249e3ec4f0510ee72`. It decodes MP3 with miniaudio,
resamples with r8brain, runs the Beat This `final0` ONNX model using ONNX Runtime 1.18.0,
and writes the standard two-column `.beats` format.

The upstream snapshot needs the small Windows compatibility patch in
`windows-runtime.patch`: two missing declarations/includes, acceptance of valid decoded
frames when a variable-bit-rate MP3 reports a slightly longer estimated length, and an
`--output-decoded` option. That option writes the already-decoded source as float WAV so Unity
can build an `AudioClip` without another MP3 package or decoder.

## Fixed dependencies

| Component | Version / commit |
|---|---|
| beat_this_cpp | `07ab790a9ec2eda8093d52d249e3ec4f0510ee72` |
| pocketfft | `c90e55b3d529f8efa40ed01a20de22405f45fc65` |
| miniaudio | `9634bedb5b5a2ca38c1ee7108a9358a4e233f14d` |
| r8brain-free-src | `9e73d2dd59fd5b95108fdb4f590083e35758b45f` |
| ONNX Runtime | `1.18.0` Windows x64 |

The official ONNX Runtime archive SHA-256 is
`A91AF21CA8F9BDFA5A1AAC3FDD0591384B4E2866D41612925F1758D5522829E7`.
Binary and model hashes are stored beside the player files in `MANIFEST.txt`.

## Baseline

Against the existing Python Beat This `final0` results:

| Song | C++ beats | Python beats | nearest match within 50 ms |
|---|---:|---:|---:|
| 象王行（特别版） | 399 | 399 | 399 / 399 |
| 青玉案·兰芥 | 382 | 382 | 381 / 382 |

Both analyses completed in about six seconds on the development machine. The player runs the
analyzer as a hidden child process and caches output by audio SHA-256 and analyzer version.

All third-party license texts shipped with the player are under
`Assets/StreamingAssets/YingYunBeatAnalyzer/Windows-x64/Licenses/`.
