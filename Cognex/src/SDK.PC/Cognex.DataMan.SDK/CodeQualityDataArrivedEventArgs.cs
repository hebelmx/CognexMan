using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class CodeQualityDataArrivedEventArgs : EventArgs
	{
		public string CodeQualityData
		{
			get;
			private set;
		}

		internal CodeQualityDataArrivedEventArgs(string codeQualityData)
		{
			this.CodeQualityData = codeQualityData;
		}
	}
}