using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Smartgeek.LogRotator
{
	public class FileLogComparer : IComparer<FileLogInfo>
	{
		public enum ComparisonMethod
		{
			ByNameOrdinal,
			ByNameOrdinalIgnoringUtf8Prefix,
			ByCreationTime,
			ByIisLogDate
		}

		public FileLogComparer(Folder folder)
		{
			this.Folder = folder;

			// if rotation is size-based, we order by creation date
			// other naming syntaxes are sortable patterns
			if (folder.Period == IisPeriodType.MaxSize)
			{
				this.Method = FileLogComparer.ComparisonMethod.ByCreationTime;
			}
			else
			{
				this.Method = FileLogComparer.ComparisonMethod.ByIisLogDate;
			}
		}

		public Folder Folder { get; private set; }
		public ComparisonMethod Method { get; private set; }

		public int Compare(FileLogInfo x, FileLogInfo y)
		{
			switch (this.Method)
			{
				case ComparisonMethod.ByNameOrdinal:
					return StringComparer.Ordinal.Compare(
						x.File.Name,
						y.File.Name
					);

				case ComparisonMethod.ByNameOrdinalIgnoringUtf8Prefix:
					// TODO convert YY to YYYY to compare centuries
					// ex.: ex990101.log must be before ex000101.log
					// parse first two digits to integer then pass to Calendar.ToFourDigitYear(...)
					return StringComparer.Ordinal.Compare(
						x.File.Name.StripeUtf8Prefix(),
						y.File.Name.StripeUtf8Prefix()
					);

				case ComparisonMethod.ByCreationTime:
					return DateTime.Compare(x.File.CreationTime, y.File.CreationTime);

				case ComparisonMethod.ByIisLogDate:
					if (x.IsChild && y.IsChild)
						return DateTime.Compare(x.Date, y.Date);
					else if (x.IsChild)
						return 1;
					else if (y.IsChild)
						return -1;
					else
						return 0;

				default:
					throw new NotImplementedException("This comparison method is not yet implemented");
			}
		}
	}
}
