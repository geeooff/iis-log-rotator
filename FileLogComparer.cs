using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Smartgeek.LogRotator
{
	public class FileLogComparer : IComparer<FileInfo>
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
			if (folder.Period == Folder.PeriodType.MaxSize)
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

		public int Compare(FileInfo x, FileInfo y)
		{
			this.Folder.IsChildLog(x);
			this.Folder.IsChildLog(y);

			switch (this.Method)
			{
				case ComparisonMethod.ByNameOrdinal:
					return StringComparer.Ordinal.Compare(
						x.Name,
						y.Name
					);

				case ComparisonMethod.ByNameOrdinalIgnoringUtf8Prefix:
					// TODO convert YY to YYYY to compare centuries
					// ex.: ex990101.log must be before ex000101.log
					// parse first two digits to integer then pass to Calendar.ToFourDigitYear(...)
					return StringComparer.Ordinal.Compare(
						x.Name.StripeUtf8Prefix(),
						y.Name.StripeUtf8Prefix()
					);

				case ComparisonMethod.ByCreationTime:
					return DateTime.Compare(x.CreationTime, y.CreationTime);

				case ComparisonMethod.ByIisLogDate:
					DateTime xDate, yDate;
					bool xIsChildLog = this.Folder.IsChildLog(x, out xDate), yIsChildLog = this.Folder.IsChildLog(y, out yDate);
					if (xIsChildLog && yIsChildLog)
						return DateTime.Compare(xDate, yDate);
					else if (xIsChildLog)
						return 1;
					else if (yIsChildLog)
						return -1;
					else
						return 0;

				default:
					throw new NotImplementedException("This comparison method is not yet implemented");
			}
		}
	}
}
