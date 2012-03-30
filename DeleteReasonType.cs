using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Smartgeek.LogRotator
{
	/// <summary>
	/// File logs deletion reasons
	/// </summary>
	public enum DeleteReasonType
	{
		/// <summary>
		/// Because it was too old
		/// </summary>
		Obsolete,

		/// <summary>
		/// Because it was compressed before, so the original file is now useless
		/// </summary>
		PreviouslyCompressed
	}
}
