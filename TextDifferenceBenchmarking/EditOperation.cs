﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TextDifferenceBenchmarking
{
	public struct EditOperation
	{
		public EditOperation(char valueFrom, char valueTo, EditOperationKind operation)
		{
			ValueFrom = valueFrom;
			ValueTo = valueTo;

			Operation = valueFrom == valueTo ? EditOperationKind.None : operation;
		}

		public char ValueFrom { get; }
		public char ValueTo { get; }
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
