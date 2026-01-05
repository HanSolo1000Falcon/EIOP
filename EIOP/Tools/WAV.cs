using System;
using System.IO;

namespace EIOP.Tools;

public class WAV
{
    public WAV(byte[] wavFile)
    {
        using MemoryStream stream = new(wavFile);
        using BinaryReader reader = new(stream);
        reader.ReadBytes(22);
        ChannelCount = reader.ReadInt16();
        Frequency    = reader.ReadInt32();
        reader.ReadBytes(6);
        int bitDepth = reader.ReadInt16();

        string dataID = new(reader.ReadChars(4));
        while (dataID != "data")
        {
            int chunkSize = reader.ReadInt32();
            reader.ReadBytes(chunkSize);
            dataID = new string(reader.ReadChars(4));
        }

        int    dataSize  = reader.ReadInt32();
        byte[] byteArray = reader.ReadBytes(dataSize);

        int bytesPerSample = bitDepth / 8;
        SampleCount = dataSize / bytesPerSample / ChannelCount;
        float[] data = new float[SampleCount * ChannelCount];

        int offset = 0;
        for (int i = 0; i < data.Length; i++)
            switch (bytesPerSample)
            {
                case 2:
                {
                    short value = BitConverter.ToInt16(byteArray, offset);
                    data[i] =  value / 32768f;
                    offset  += 2;

                    break;
                }

                case 1:
                    data[i] = (byteArray[offset] - 128) / 128f;
                    offset++;

                    break;
            }

        if (ChannelCount == 2)
        {
            LeftChannel  = new float[SampleCount];
            RightChannel = new float[SampleCount];
            for (int i = 0; i < SampleCount; i++)
            {
                LeftChannel[i]  = data[i * 2];
                RightChannel[i] = data[i * 2 + 1];
            }
        }
        else
        {
            LeftChannel  = data;
            RightChannel = null;
        }
    }

    public float[] LeftChannel  { get; }
    public float[] RightChannel { get; }
    public int     ChannelCount { get; }
    public int     SampleCount  { get; }
    public int     Frequency    { get; private set; }
}