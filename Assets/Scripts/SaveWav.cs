using UnityEngine;
using System.IO;
using System;

public static class SaveWav
{
    const int HEADER_SIZE = 44;

    public static bool Save(string path, AudioClip clip, float minThreshold = 0.01f)
    {
        if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            path += ".wav";

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AudioClip trimmed = TrimSilence(clip, minThreshold);
        if (trimmed == null)
        {
            Debug.LogWarning("저장할 오디오가 모두 무음입니다.");
            return false;
        }

        using (FileStream fs = CreateEmpty(path))
        {
            ConvertAndWrite(fs, trimmed);
            WriteHeader(fs, trimmed);
        }
        return true;
    }

    static AudioClip TrimSilence(AudioClip clip, float threshold)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        int start = 0, end = samples.Length - 1;

        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > threshold)
            {
                start = i;
                break;
            }
        }
        for (int i = samples.Length - 1; i >= 0; i--)
        {
            if (Mathf.Abs(samples[i]) > threshold)
            {
                end = i;
                break;
            }
        }

        int len = end - start + 1;
        if (len <= 0)
            return null;

        float[] trimmedSamples = new float[len];
        Array.Copy(samples, start, trimmedSamples, 0, len);
        AudioClip tClip = AudioClip.Create(clip.name + "_trimmed", len / clip.channels, clip.channels, clip.frequency, false);
        tClip.SetData(trimmedSamples, 0);
        return tClip;
    }

    static FileStream CreateEmpty(string path)
    {
        FileStream fs = new FileStream(path, FileMode.Create);
        for (int i = 0; i < HEADER_SIZE; i++) fs.WriteByte(0);
        return fs;
    }

    static void ConvertAndWrite(FileStream fs, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        byte[] bytes = new byte[samples.Length * 2];
        const int rescale = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)(samples[i] * rescale);
            Array.Copy(BitConverter.GetBytes(s), 0, bytes, i * 2, 2);
        }
        fs.Write(bytes, 0, bytes.Length);
    }

    static void WriteHeader(FileStream fs, AudioClip clip)
    {
        int hz = clip.frequency, channels = clip.channels, samples = clip.samples;
        fs.Seek(0, SeekOrigin.Begin);

        void WriteStr(string s)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            fs.Write(bytes, 0, bytes.Length);
        }

        WriteStr("RIFF");
        fs.Write(BitConverter.GetBytes((int)fs.Length - 8), 0, 4);
        WriteStr("WAVEfmt ");
        fs.Write(BitConverter.GetBytes(16), 0, 4);
        fs.Write(BitConverter.GetBytes((ushort)1), 0, 2);
        fs.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
        fs.Write(BitConverter.GetBytes(hz), 0, 4);
        fs.Write(BitConverter.GetBytes(hz * channels * 2), 0, 4);
        fs.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
        fs.Write(BitConverter.GetBytes((ushort)16), 0, 2);
        WriteStr("data");
        fs.Write(BitConverter.GetBytes(samples * channels * 2), 0, 4);
    }
}
