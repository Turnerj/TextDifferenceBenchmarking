using System;
using System.Collections.Generic;
using System.Text;

namespace TextDifferenceBenchmarking
{
	public enum EditOperationKind : byte
	{
		None,    // Nothing to do
		Add,     // Add new character
		Edit,    // Edit character into character (including char into itself)
		Remove,  // Delete existing character
	}
}
