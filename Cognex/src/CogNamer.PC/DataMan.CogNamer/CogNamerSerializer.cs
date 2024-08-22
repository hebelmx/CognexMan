using System;
using System.Net;
using System.Text;

namespace Cognex.DataMan.CogNamer
{
	internal static class CogNamerSerializer
	{
		public static byte[] GetBuffer(byte[] recordBytes, ref int inputPosition)
		{
			int num = CogNamerSerializer.GetInt(recordBytes, ref inputPosition);
			byte[] numArray = new byte[num];
			Array.Copy(recordBytes, inputPosition, numArray, 0, num);
			inputPosition += num;
			return numArray;
		}

		public static byte[] GetBytes(byte value)
		{
			return BitConverter.GetBytes((short)value);
		}

		public static byte[] GetBytes(ushort value)
		{
			return BitConverter.GetBytes(value);
		}

		public static byte[] GetBytes(uint value)
		{
			return BitConverter.GetBytes(value);
		}

		public static byte[] GetBytes(int input)
		{
			if (input < 1)
			{
				return new byte[1];
			}
			int num = input;
			int num1 = 0;
			int num2 = 0;
			int num3 = input;
			while (num3 > 0)
			{
				num3 >>= 1;
				num2++;
			}
			int num4 = (int)Math.Ceiling((double)num2 / 7);
			byte[] numArray = new byte[num4];
			while (num > 127)
			{
				numArray[num1] = (byte)(num % 128);
				ref byte numPointer = ref numArray[num1];
				numPointer = (byte)(numPointer + 128);
				num /= 128;
				num1++;
			}
			numArray[num1] = (byte)num;
			return numArray;
		}

		public static byte[] GetBytes(IPAddress address)
		{
			if (address == null)
			{
				return new byte[4];
			}
			byte[] addressBytes = address.GetAddressBytes();
			byte[] numArray = new byte[] { addressBytes[3], addressBytes[2], addressBytes[1], addressBytes[0] };
			return numArray;
		}

		public static byte[] GetBytesForBuffer(byte[] buffer)
		{
			byte[] bytes = CogNamerSerializer.GetBytes((int)buffer.Length);
			return CogNamerSerializer.JoinBuffers(bytes, buffer);
		}

		public static byte[] GetBytesWithLength(string input)
		{
			if (input == null)
			{
				input = "";
			}
			byte[] bytes = CogNamerSerializer.GetBytes(input.Length);
			byte[] numArray = Encoding.UTF8.GetBytes(input);
			return CogNamerSerializer.JoinBuffers(bytes, numArray);
		}

		public static byte[] GetBytesWithoutLength(string input)
		{
			return Encoding.UTF8.GetBytes(input);
		}

		public static int GetInt(byte[] input, ref int inputPosition)
		{
			int num = 0;
			int num1 = 0;
			while (inputPosition < (int)input.Length)
			{
				int num2 = inputPosition;
				int num3 = num2;
				inputPosition = num2 + 1;
				byte num4 = input[num3];
				if (num4 <= 127)
				{
					return num1 + num4 * (int)Math.Pow(128, (double)num);
				}
				num1 = num1 + (num4 - 128) * (int)Math.Pow(128, (double)num);
				num++;
			}
			throw new Exception("Unexpected end of VarInt field");
		}

		public static IPAddress GetIPAddress(byte[] input, ref int inputPosition)
		{
			byte[] numArray = CogNamerSerializer.ReadBytes(input, ref inputPosition, 4);
			return new IPAddress(CogNamerSerializer.ReverseBytes(numArray));
		}

		public static string GetStringFromAllInputBytes(byte[] input)
		{
			return Encoding.UTF8.GetString(input, 0, (int)input.Length);
		}

		public static string GetStringWithLength(byte[] recordBytes, ref int inputPosition)
		{
			int num = CogNamerSerializer.GetInt(recordBytes, ref inputPosition);
			return CogNamerSerializer.GetStringWithoutLength(recordBytes, num, ref inputPosition);
		}

		public static string GetStringWithoutLength(byte[] recordBytes, int stringLength, ref int inputPosition)
		{
			string str = Encoding.UTF8.GetString(recordBytes, inputPosition, stringLength);
			inputPosition += stringLength;
			return str;
		}

		public static ushort GetU16(byte[] input, ref int inputPosition)
		{
			return BitConverter.ToUInt16(CogNamerSerializer.ReadBytes(input, ref inputPosition, 2), 0);
		}

		public static uint GetU32(byte[] input, ref int inputPosition)
		{
			return BitConverter.ToUInt32(CogNamerSerializer.ReadBytes(input, ref inputPosition, 4), 0);
		}

		public static uint GetU8(byte[] input, ref int inputPosition)
		{
			return CogNamerSerializer.ReadBytes(input, ref inputPosition, 1)[0];
		}

		public static byte[] JoinBuffers(byte[] buffer1, byte[] buffer2)
		{
			byte[] numArray = new byte[(int)buffer1.Length + (int)buffer2.Length];
			Buffer.BlockCopy(buffer1, 0, numArray, 0, (int)buffer1.Length);
			Buffer.BlockCopy(buffer2, 0, numArray, (int)buffer1.Length, (int)buffer2.Length);
			return numArray;
		}

		private static byte[] ReadBytes(byte[] input, ref int inputPosition, int numBytes)
		{
			byte[] numArray = new byte[numBytes];
			Array.Copy(input, inputPosition, numArray, 0, numBytes);
			inputPosition += numBytes;
			return numArray;
		}

		public static byte[] ReverseBytes(byte[] bytes)
		{
			byte[] numArray = new byte[(int)bytes.Length];
			for (int i = 0; i < (int)numArray.Length; i++)
			{
				numArray[i] = bytes[(int)bytes.Length - i - 1];
			}
			return numArray;
		}
	}
}