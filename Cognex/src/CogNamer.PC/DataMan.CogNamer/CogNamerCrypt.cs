using System;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	internal sealed class CogNamerCrypt
	{
		private const int MULTIPLIER = 3417;

		private const int ADDER = 23;

		private const int RAND_MASK = 4064;

		private const int OFFSET_0 = 26;

		private const int OFFSET_US = 36;

		private const int OFFSET_MINUS = 37;

		private const int OFFSET_A = 38;

		private readonly static byte[] LUT;

		static CogNamerCrypt()
		{
			int i;
			CogNamerCrypt.LUT = new byte[64];
			for (i = 0; i <= 25; i++)
			{
				CogNamerCrypt.LUT[i] = (byte)(i + 97);
			}
			for (int j = 48; j <= 57; j++)
			{
				CogNamerCrypt.LUT[i] = (byte)j;
				i++;
			}
			byte[] lUT = CogNamerCrypt.LUT;
			int num = i;
			int num1 = num + 1;
			lUT[num] = (byte)95;
			byte[] numArray = CogNamerCrypt.LUT;
			int num2 = num1;
			int num3 = num2 + 1;
			numArray[num2] = (byte)45;
			for (int k = 65; k <= 90; k++)
			{
				CogNamerCrypt.LUT[num3] = (byte)k;
				num3++;
			}
		}

		public CogNamerCrypt()
		{
		}

		public static string av_decrypt(string src)
		{
			if (string.IsNullOrEmpty(src))
			{
				return string.Empty;
			}
			char[] charArray = src.ToCharArray();
			byte[] numArray = new byte[(int)charArray.Length];
			for (int i = 0; i < (int)charArray.Length; i++)
			{
				numArray[i] = (byte)charArray[i];
			}
			byte[] numArray1 = CogNamerCrypt.av_decrypt(numArray);
			char[] chrArray = new char[(int)numArray1.Length];
			for (int j = 0; j < (int)numArray1.Length; j++)
			{
				chrArray[j] = (char)numArray1[j];
			}
			return new string(chrArray);
		}

		public static byte[] av_decrypt(byte[] encrypt)
		{
			if (encrypt == null || (int)encrypt.Length == 0)
			{
				return new byte[0];
			}
			byte[] lUT = new byte[(int)encrypt.Length - 1];
			CogNamerCrypt.SeedValue seedValue = new CogNamerCrypt.SeedValue()
			{
				Seed = CogNamerCrypt.get_offset(encrypt[0])
			};
			CogNamerCrypt.av_random(seedValue);
			seedValue.Seed = 0;
			for (int i = 1; i < (int)encrypt.Length; i++)
			{
				int num = CogNamerCrypt.av_random(seedValue) % 64;
				int _offset = CogNamerCrypt.get_offset(encrypt[i]);
				if (_offset < 0)
				{
					lUT[i - 1] = (byte)(-_offset);
				}
				else
				{
					int num1 = _offset - num;
					lUT[i - 1] = CogNamerCrypt.LUT[(num1 < 0 ? num1 + 64 : num1)];
				}
			}
			return lUT;
		}

		public static byte[] av_encrypt(string src)
		{
			if (string.IsNullOrEmpty(src))
			{
				return new byte[0];
			}
			return CogNamerCrypt.av_encrypt(Encoding.UTF8.GetBytes(src));
		}

		public static byte[] av_encrypt(byte[] str)
		{
			if (str == null || (int)str.Length == 0)
			{
				return new byte[0];
			}
			byte[] lUT = new byte[(int)str.Length + 1];
			CogNamerCrypt.SeedValue seedValue = new CogNamerCrypt.SeedValue();
			if ((int)str.Length == 0)
			{
				return null;
			}
			Random random = new Random();
			seedValue.Seed = (int)(random.NextDouble() * 63) + 1;
			lUT[0] = CogNamerCrypt.LUT[seedValue.Seed];
			CogNamerCrypt.av_random(seedValue);
			seedValue.Seed = 0;
			int num = 0;
			while (num < (int)str.Length)
			{
				int num1 = CogNamerCrypt.av_random(seedValue) % 64;
				int _offset = CogNamerCrypt.get_offset(str[num]);
				byte[] numArray = lUT;
				int num2 = num + 1;
				num = num2;
				numArray[num2] = (_offset < 0 ? (byte)(-_offset) : CogNamerCrypt.LUT[(num1 + _offset) % 64]);
			}
			return lUT;
		}

		private static int av_random(CogNamerCrypt.SeedValue v)
		{
			if (v.Seed != 0)
			{
				v.Value = -1 * v.Seed;
			}
			v.Value = v.Value * 3417 + 23;
			return CogNamerCrypt.URShift(v.Value & 4064, 4);
		}

		private static int get_offset(byte b)
		{
			int num = b & 255;
			if (num >= 97 && num <= 122)
			{
				return num - 97;
			}
			if (num >= 65 && num <= 90)
			{
				return num - 65 + 38;
			}
			if (num >= 48 && num <= 57)
			{
				return num - 48 + 26;
			}
			if (num == 95)
			{
				return 36;
			}
			if (num == 45)
			{
				return 37;
			}
			return -num;
		}

		private static int URShift(int number, int bits)
		{
			if (number >= 0)
			{
				return number >> (bits & 31);
			}
			return (number >> (bits & 31)) + (2 << (~bits & 31));
		}

		private class SeedValue
		{
			internal int Seed;

			internal int Value;

			public SeedValue()
			{
			}
		}
	}
}