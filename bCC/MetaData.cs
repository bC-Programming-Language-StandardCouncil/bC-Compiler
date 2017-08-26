﻿using JetBrains.Annotations;

namespace bCC
{
	public struct MetaData
	{
		public int LineNumber;
		public string FileName;

		public MetaData(int lineNumber, string fileName)
		{
			LineNumber = lineNumber;
			FileName = fileName;
		}

		// FEATURE #10
		[NotNull]
		public string GetErrorHeader() => $"Error in file {FileName} at line {LineNumber}: ";

		public static readonly MetaData Empty = new MetaData(-1, "Unknown");
		public static readonly MetaData BuiltIn = new MetaData(-1, "[built-in]");
	}
}