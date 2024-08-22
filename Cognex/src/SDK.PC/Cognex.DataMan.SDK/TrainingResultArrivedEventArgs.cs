using System;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK
{
	public class TrainingResultArrivedEventArgs : EventArgs
	{
		public string TrainingResult
		{
			get;
			private set;
		}

		internal TrainingResultArrivedEventArgs(string trainingResult)
		{
			this.TrainingResult = trainingResult;
		}
	}
}