using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class DmccResponse : IDisposable
	{
		internal const int DMCC_STATUS_NO_ERROR = 0;

		internal const int DMCC_STATUS_READ_STRING = 1;

		internal const int DMCC_STATUS_AUTO_RESPONSE = 2;

		internal const int DMCC_STATUS_XML_RESULT = 3;

		internal const int DMCC_STATUS_XML_STATISTICS = 4;

		internal const int DMCC_STATUS_IMAGE = 5;

		internal const int DMCC_STATUS_IMAGE_GRAPHICS = 6;

		internal const int DMCC_STATUS_TRAINING_RESULT = 7;

		internal const int DMCC_STATUS_AUTO_TRAIN_BRIGHT = 8;

		internal const int DMCC_STATUS_AUTO_TRAIN_STRING = 9;

		internal const int DMCC_STATUS_CODE_QUALITY_DATA = 10;

		internal const int DMCC_STATUS_AUTO_TRAIN_FOCUS = 11;

		internal const int DMCC_STATUS_EVENT = 12;

		internal const int DMCC_STATUS_INPUT_EVENT = 13;

		internal const int DMCC_STATUS_MST_TEST = 14;

		internal const int DMCC_STATUS_PCM_REPORT = 15;

		internal const int DMCC_STATUS_UNIDENTIFIED_ERROR = 100;

		internal const int DMCC_STATUS_INVALID_COMMAND = 101;

		internal const int DMCC_STATUS_INVALID_PARAM_OR_MISSING_FEATURE = 102;

		internal const int DMCC_STATUS_INCORRECT_CHECKSUM = 103;

		internal const int DMCC_STATUS_PARAMETER_REJECTED = 104;

		internal const int DMCC_STATUS_READER_OFFLINE = 105;

		public MemoryStream BinaryData
		{
			get;
			internal set;
		}

		public int CommandId
		{
			get;
			internal set;
		}

		public TimeSpan CommandRoundtrip
		{
			get;
			internal set;
		}

		public string PayLoad
		{
			get;
			internal set;
		}

		public int ResponseId
		{
			get;
			internal set;
		}

		internal int Status
		{
			get;
			set;
		}

		public DmccResponse()
		{
		}

		public void Dispose()
		{
			if (this.BinaryData != null)
			{
				this.BinaryData.Dispose();
				this.BinaryData = null;
			}
		}
	}
}