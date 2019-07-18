using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace TextDifferenceBenchmarking.Utilities
{
	/// <summary>
	/// Every row in the array is an ArrayPool of {columns}, extending the upper bound of the shared ArrayPool
	/// </summary>
	/// <typeparam name="T">The type of elements in the matrix.</typeparam>
	public readonly struct LargeArrayPoolMatrix<T> : IDisposable
		where T : struct
	{
		private const int MaxSharedArrayPoolSize = 1048576;
		
		private readonly ArrayPool<T> Pool;
		private readonly int PoolCount;
		private readonly int PoolSize;
		
		private readonly T[][] Data;

		/// <summary>
		/// Gets the number of columns in the dense matrix.
		/// </summary>
		public readonly int Columns;

		/// <summary>
		/// Gets the number of rows in the dense matrix.
		/// </summary>
		public readonly int Rows;
		
		/// <summary>
		/// Initializes a new instance of the <see cref=" DenseMatrix{T}" /> struct.
		/// </summary>
		/// <param name="columns">The number of columns.</param>
		/// <param name="rows">The number of rows.</param>
		public LargeArrayPoolMatrix(int columns, int rows)
		{
			Pool = ArrayPool<T>.Shared;
			Rows = rows;
			Columns = columns;
			PoolSize = MaxSharedArrayPoolSize;
			
			var leftOver = columns * rows;
			var numberOfFullPools = 0;
			for (; leftOver >= PoolSize; leftOver -= PoolSize, numberOfFullPools++);

			PoolCount = numberOfFullPools;
			if (leftOver > 0)
			{
				PoolCount++;
			}

			Data = new T[PoolCount][];

			for (int i = 0; i < PoolCount; i++)
			{
				var poolRent = PoolSize;
				if (i + 1 == PoolCount && leftOver > 0)
				{
					poolRent = leftOver;
				}
				Data[i] = Pool.Rent(poolRent);
			}
		}

		/// <summary>
		/// Gets or sets the item at the specified position.
		/// </summary>
		/// <param name="row">The row-coordinate of the item. Must be greater than or equal to zero and less than the height of the array.</param>
		/// <param name="column">The column-coordinate of the item. Must be greater than or equal to zero and less than the width of the array.</param>
		/// <returns>The <see typeparam="T"/> at the specified position.</returns>
		public ref T this[int row, int column]
		{
			[MethodImpl(InliningOptions.ShortMethod)]
			get
			{
				if (row >= Rows)
				{
					throw new ArgumentException("Invalid row index");
				}
				if (column >= Columns)
				{
					throw new ArgumentException("Invalid column index");
				}

				var itemIndex = (row * this.Columns) + column;
				var poolIndex = 0;
				for (; itemIndex >= PoolSize; itemIndex -= PoolSize, poolIndex++);
				return ref Data[poolIndex][itemIndex];
			}
		}

		public void Dispose()
		{
			for (int i = 0; i < PoolCount; i++)
			{
				Pool.Return(Data[i], true);
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is LargeArrayPoolMatrix<T> other && this.Equals(other);

		/// <inheritdoc/>
		public bool Equals(LargeArrayPoolMatrix<T> other) =>
			this.Columns == other.Columns
			&& this.Rows == other.Rows
			&& this.Data.Equals(other.Data);

		/// <inheritdoc/>
		public override int GetHashCode() => this.Data.GetHashCode();
	}
}
