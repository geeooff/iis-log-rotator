using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smartgeek.LogRotator
{
	public enum IisLogFormatType
	{
		CentralBinary = -2,
		CentralW3C = -1,
		IIS = 0,
		NCSA = 1,
		W3C = 2,
		Custom = 3
	}
}
