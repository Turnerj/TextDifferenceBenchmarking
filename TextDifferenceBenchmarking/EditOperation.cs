using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TextDifferenceBenchmarking
{
	public struct EditOperation
	{
		public EditOperation(char valueFrom, char valueTo, EditOperationKind operation)
		{
			_ValueFrom = valueFrom;
			_ValueTo = valueTo;

			Operation = valueFrom == valueTo ? EditOperationKind.None : operation;
		}

		[MarshalAs(UnmanagedType.I2)]
		private readonly char _ValueFrom;
		[MarshalAs(UnmanagedType.I2)]
		private readonly char _ValueTo;
		public char ValueFrom => _ValueFrom;
		public char ValueTo => _ValueTo;
		public EditOperationKind Operation { get; }

		public override string ToString()
		{
			switch (Operation)
			{
				case EditOperationKind.None:
					return $"'{ValueTo}' Equal";
				case EditOperationKind.Add:
					return $"'{ValueTo}' Add";
				case EditOperationKind.Remove:
					return $"'{ValueFrom}' Remove";
				case EditOperationKind.Edit:
					return $"'{ValueFrom}' to '{ValueTo}' Edit";
				default:
					return "???";
			}
		}
	}
}
