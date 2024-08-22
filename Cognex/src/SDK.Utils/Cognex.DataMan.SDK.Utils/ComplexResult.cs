using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cognex.DataMan.SDK.Utils
{
	public class ComplexResult
	{
		public List<SimpleResult> SimpleResults
		{
			get;
			private set;
		}

		public ComplexResult()
		{
			this.SimpleResults = new List<SimpleResult>();
		}
	}
}