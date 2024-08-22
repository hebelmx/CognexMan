using System;
using System.Text;

namespace Cognex.DataMan.SDK
{
	internal interface IDmccParser
	{
		System.Text.Encoding Encoding
		{
			get;
			set;
		}

		bool ExpectReadStringAsBinaryResponse
		{
			get;
			set;
		}

		bool IsAtEndOfDmccHeader
		{
			get;
		}

		bool IsAtEndOfDmccMessage
		{
			get;
		}

		bool IsParserInErrorState
		{
			get;
		}

		bool IsReadingBinaryData
		{
			get;
		}

		DmccMessage LastDmccMessage
		{
			get;
		}

		int RemainingBinaryDataBytes
		{
			get;
		}

		int TotalBinaryDataBytes
		{
			get;
		}

		void ParseByte(byte dataByte);

		void Reset();

		void StoreBinaryDataBytes(byte[] aBinaryBytes, int startIndex, int bytesToStore);
	}
}