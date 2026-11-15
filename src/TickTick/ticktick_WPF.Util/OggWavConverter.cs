using System;
using System.IO;
using System.Text;
using TickTickUtils;
using csogg;
using csvorbis;

namespace ticktick_WPF.Util;

public class OggWavConverter
{
	private static byte[] RIFF_HEADER = new byte[4] { 82, 73, 70, 70 };

	private static byte[] FORMAT_WAVE = new byte[4] { 87, 65, 86, 69 };

	private static byte[] FORMAT_TAG = new byte[4] { 102, 109, 116, 32 };

	private static byte[] AUDIO_FORMAT = new byte[2] { 1, 0 };

	private static byte[] SUBCHUNK_ID = new byte[4] { 100, 97, 116, 97 };

	private const int BYTES_PER_SAMPLE = 2;

	public static void Convert(Stream input, string fileName)
	{
		int num = (int)input.Length;
		byte[] array = new byte[num];
		TextWriter textWriter = new DebugWriter();
		Stream stream = new FileStream(AppPaths.PomoSoundDir + fileName + ".wav", FileMode.Create);
		stream.Seek(100L, SeekOrigin.Begin);
		SyncState syncState = new SyncState();
		StreamState streamState = new StreamState();
		Page page = new Page();
		Packet op = new Packet();
		Info info = new Info();
		Comment comment = new Comment();
		DspState dspState = new DspState();
		Block block = new Block(dspState);
		int num2 = 0;
		syncState.init();
		while (true)
		{
			int num3 = 0;
			int offset = syncState.buffer(4096);
			byte[] data = syncState.data;
			try
			{
				num2 = input.Read(data, offset, 4096);
			}
			catch (Exception value)
			{
				textWriter.WriteLine(value);
			}
			syncState.wrote(num2);
			if (syncState.pageout(page) != 1)
			{
				if (num2 < 4096)
				{
					break;
				}
				textWriter.WriteLine("Input does not appear to be an Ogg bitstream.");
			}
			streamState.init(page.serialno());
			info.init();
			comment.init();
			if (streamState.pagein(page) < 0)
			{
				textWriter.WriteLine("Error reading first page of Ogg bitstream data.");
			}
			if (streamState.packetout(op) != 1)
			{
				textWriter.WriteLine("Error reading initial header packet.");
			}
			if (info.synthesis_headerin(comment, op) < 0)
			{
				textWriter.WriteLine("This Ogg bitstream does not contain Vorbis audio data.");
			}
			int i = 0;
			while (i < 2)
			{
				while (i < 2)
				{
					switch (syncState.pageout(page))
					{
					case 1:
						streamState.pagein(page);
						for (; i < 2; info.synthesis_headerin(comment, op), i++)
						{
							switch (streamState.packetout(op))
							{
							case -1:
								textWriter.WriteLine("Corrupt secondary header.  Exiting.");
								continue;
							default:
								continue;
							case 0:
								break;
							}
							break;
						}
						continue;
					default:
						continue;
					case 0:
						break;
					}
					break;
				}
				offset = syncState.buffer(4096);
				data = syncState.data;
				try
				{
					num2 = input.Read(data, offset, 4096);
				}
				catch (Exception value2)
				{
					textWriter.WriteLine(value2);
				}
				if (num2 == 0 && i < 2)
				{
					textWriter.WriteLine("End of file before finding all Vorbis headers!");
				}
				syncState.wrote(num2);
			}
			byte[][] user_comments = comment.user_comments;
			for (int j = 0; j < comment.user_comments.Length && user_comments[j] != null; j++)
			{
				textWriter.WriteLine(comment.getComment(j));
			}
			textWriter.WriteLine("\nBitstream is " + info.channels + " channel, " + info.rate + "Hz");
			textWriter.WriteLine("Encoded by: " + comment.getVendor() + "\n");
			num = 4096 / info.channels;
			dspState.synthesis_init(info);
			block.init(dspState);
			float[][][] array2 = new float[1][][];
			int[] array3 = new int[info.channels];
			while (num3 == 0)
			{
				while (num3 == 0)
				{
					switch (syncState.pageout(page))
					{
					case -1:
						textWriter.WriteLine("Corrupt or missing data in bitstream; continuing...");
						continue;
					default:
						streamState.pagein(page);
						while (true)
						{
							switch (streamState.packetout(op))
							{
							case -1:
								continue;
							default:
							{
								if (block.synthesis(op) == 0)
								{
									dspState.synthesis_blockin(block);
								}
								int num4;
								while ((num4 = dspState.synthesis_pcmout(array2, array3)) > 0)
								{
									float[][] array4 = array2[0];
									bool flag = false;
									int num5 = ((num4 < num) ? num4 : num);
									for (i = 0; i < info.channels; i++)
									{
										int num6 = i * 2;
										int num7 = array3[i];
										for (int k = 0; k < num5; k++)
										{
											int num8 = (int)((double)array4[i][num7 + k] * 32767.0);
											if (num8 > 32767)
											{
												num8 = 32767;
												flag = true;
											}
											if (num8 < -32768)
											{
												num8 = -32768;
												flag = true;
											}
											if (num8 < 0)
											{
												num8 |= 0x8000;
											}
											array[num6] = (byte)num8;
											array[num6 + 1] = (byte)((uint)num8 >> 8);
											num6 += 2 * info.channels;
										}
									}
									stream.Write(array, 0, 2 * info.channels * num5);
									dspState.synthesis_read(num5);
								}
								continue;
							}
							case 0:
								break;
							}
							break;
						}
						if (page.eos() != 0)
						{
							num3 = 1;
						}
						continue;
					case 0:
						break;
					}
					break;
				}
				if (num3 == 0)
				{
					offset = syncState.buffer(4096);
					data = syncState.data;
					try
					{
						num2 = input.Read(data, offset, 4096);
					}
					catch (Exception value3)
					{
						textWriter.WriteLine(value3);
					}
					syncState.wrote(num2);
					if (num2 == 0)
					{
						num3 = 1;
					}
				}
			}
			streamState.clear();
			block.clear();
			dspState.clear();
			info.clear();
		}
		syncState.clear();
		textWriter.WriteLine("Done.");
		stream.Seek(0L, SeekOrigin.Begin);
		WriteHeader(stream, (int)(stream.Length - 100), info.rate, 16, (ushort)info.channels);
		stream.Seek(0L, SeekOrigin.Begin);
		stream.Dispose();
		input.Dispose();
	}

	private static void WriteHeader(Stream stream, int length, int audioSampleRate, ushort audioBitsPerSample, ushort audioChannels)
	{
		BinaryWriter binaryWriter = new BinaryWriter(stream);
		binaryWriter.Write(Encoding.ASCII.GetBytes("RIFF"));
		int value = 36 + length;
		binaryWriter.Write(value);
		binaryWriter.Write(new char[8] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
		binaryWriter.Write(16);
		binaryWriter.Write((short)1);
		binaryWriter.Write((short)audioChannels);
		binaryWriter.Write(audioSampleRate);
		binaryWriter.Write(audioSampleRate * (audioBitsPerSample * audioChannels / 8));
		binaryWriter.Write((short)(audioBitsPerSample * audioChannels / 8));
		binaryWriter.Write((short)audioBitsPerSample);
		binaryWriter.Write(new char[4] { 'd', 'a', 't', 'a' });
		binaryWriter.Write(length);
	}

	public static void WriteHeader(Stream targetStream, int byteStreamSize, int channelCount, int sampleRate)
	{
		int source = sampleRate * channelCount * 2;
		int source2 = channelCount * 2;
		targetStream.Write(RIFF_HEADER, 0, RIFF_HEADER.Length);
		targetStream.Write(PackageInt(byteStreamSize + 42, 4), 0, 4);
		targetStream.Write(FORMAT_WAVE, 0, FORMAT_WAVE.Length);
		targetStream.Write(FORMAT_TAG, 0, FORMAT_TAG.Length);
		targetStream.Write(PackageInt(16, 4), 0, 4);
		targetStream.Write(AUDIO_FORMAT, 0, AUDIO_FORMAT.Length);
		targetStream.Write(PackageInt(channelCount), 0, 2);
		targetStream.Write(PackageInt(sampleRate, 4), 0, 4);
		targetStream.Write(PackageInt(source, 4), 0, 4);
		targetStream.Write(PackageInt(source2), 0, 2);
		targetStream.Write(PackageInt(16), 0, 2);
		targetStream.Write(SUBCHUNK_ID, 0, SUBCHUNK_ID.Length);
		targetStream.Write(PackageInt(byteStreamSize, 4), 0, 4);
	}

	private static byte[] PackageInt(int source, int length = 2)
	{
		if (length != 2 && length != 4)
		{
			throw new ArgumentException("length must be either 2 or 4", "length");
		}
		byte[] array = new byte[length];
		array[0] = (byte)(source & 0xFF);
		array[1] = (byte)((source >> 8) & 0xFF);
		if (length == 4)
		{
			array[2] = (byte)((source >> 16) & 0xFF);
			array[3] = (byte)((source >> 24) & 0xFF);
		}
		return array;
	}
}
