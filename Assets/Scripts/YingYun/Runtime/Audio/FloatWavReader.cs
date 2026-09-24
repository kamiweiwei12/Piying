using System;
using System.IO;
using System.Text;

namespace YingYun.Rhythm.Audio
{
    public sealed class DecodedAudioData
    {
        public DecodedAudioData(float[] samples, int channels, int sampleRate)
        {
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            Channels = channels;
            SampleRate = sampleRate;
        }

        public float[] Samples { get; }
        public int Channels { get; }
        public int SampleRate { get; }
        public int FrameCount => Samples.Length / Channels;
    }

    public static class FloatWavReader
    {
        private const ushort IeeeFloat = 3;

        public static DecodedAudioData Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream, Encoding.ASCII, false))
            {
                if (ReadFourCc(reader) != "RIFF") throw new InvalidDataException("Audio cache is not RIFF WAV.");
                reader.ReadUInt32();
                if (ReadFourCc(reader) != "WAVE") throw new InvalidDataException("Audio cache is not WAVE.");

                ushort format = 0;
                ushort channels = 0;
                int sampleRate = 0;
                ushort bitsPerSample = 0;
                long dataOffset = -1;
                uint dataLength = 0;

                while (stream.Position + 8 <= stream.Length)
                {
                    string chunkId = ReadFourCc(reader);
                    uint chunkLength = reader.ReadUInt32();
                    long nextChunk = stream.Position + chunkLength + (chunkLength & 1u);
                    if (nextChunk > stream.Length + 1) throw new InvalidDataException("WAV chunk exceeds file length.");

                    if (chunkId == "fmt ")
                    {
                        if (chunkLength < 16) throw new InvalidDataException("WAV fmt chunk is incomplete.");
                        format = reader.ReadUInt16();
                        channels = reader.ReadUInt16();
                        sampleRate = reader.ReadInt32();
                        reader.ReadUInt32();
                        reader.ReadUInt16();
                        bitsPerSample = reader.ReadUInt16();
                    }
                    else if (chunkId == "data")
                    {
                        dataOffset = stream.Position;
                        dataLength = chunkLength;
                    }

                    stream.Position = nextChunk;
                    if (dataOffset >= 0 && format != 0) break;
                }

                if (format != IeeeFloat || bitsPerSample != 32)
                    throw new InvalidDataException($"Expected 32-bit IEEE float WAV, got format {format} / {bitsPerSample} bit.");
                if (channels == 0 || sampleRate <= 0 || dataOffset < 0 || dataLength == 0 || dataLength % 4 != 0)
                    throw new InvalidDataException("WAV audio metadata is invalid.");

                stream.Position = dataOffset;
                int sampleCount = checked((int)(dataLength / 4));
                var samples = new float[sampleCount];
                for (int index = 0; index < sampleCount; index++) samples[index] = reader.ReadSingle();
                if (sampleCount % channels != 0) throw new InvalidDataException("WAV sample count is not channel aligned.");
                return new DecodedAudioData(samples, channels, sampleRate);
            }
        }

        private static string ReadFourCc(BinaryReader reader) => new string(reader.ReadChars(4));
    }
}
