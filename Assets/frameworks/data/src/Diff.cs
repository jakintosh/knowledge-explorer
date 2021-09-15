using System;
using System.Collections.Generic;
using System.Text;

namespace Jakintosh.Data {

	public enum DiffOperationTypes {
		Removal = -1,
		Addition = 1
	}

	[Serializable]
	public class SequenceDiffOperation<T> {

		public readonly DiffOperationTypes Type;
		public readonly int StartIndex;
		public readonly int Length;
		public readonly IList<T> Content;

		public SequenceDiffOperation ( DiffOperationTypes type, int start, int length, IList<T> content ) {

			Type = type;
			StartIndex = start;
			Length = length;
			Content = content;
		}

		// string stuff
		public override string ToString () {
			return $"{GetTypeString()} {StartIndex}:{Length} {{{GetContentString()}}}";
		}
		public string GetTypeString () {
			return Type switch {
				DiffOperationTypes.Addition => "+",
				DiffOperationTypes.Removal => "-",
				_ => "?"
			};
		}
		public string GetContentString () {
			return Type switch {
				DiffOperationTypes.Addition => $"{{\"{Content}\"}}",
				_ => ""
			};
		}
	}

	[Serializable]
	public class StringDiffOperation {

		public readonly DiffOperationTypes Type;
		public readonly int StartIndex;
		public readonly int Length;
		public readonly string Content;

		public StringDiffOperation ( DiffOperationTypes type, int start, int length, string content = null ) {

			Type = type;
			StartIndex = start;
			Length = length;
			Content = content;
		}

		// string stuff
		public override string ToString () {
			return $"{GetTypeString()} {StartIndex}:{Length} {GetContentString()}";
		}
		public string GetTypeString () {
			return Type switch {
				DiffOperationTypes.Addition => "+",
				DiffOperationTypes.Removal => "-",
				_ => "?"
			};
		}
		public string GetContentString () {
			return Type switch {
				DiffOperationTypes.Addition => $"{{\"{Content}\"}}",
				_ => ""
			};
		}
	}

	[Serializable]
	public class SequenceDiff<T> {

		public List<SequenceDiffOperation<T>> Changes { get; private set; }

		public SequenceDiff ( List<SequenceDiffOperation<T>> changes ) {

			// sort by start index, descending (always need sto be applied in reverse)
			changes.Sort( ( a, b ) => b.StartIndex.CompareTo( a.StartIndex ) );

			Changes = changes;
		}

		public IList<T> ApplyTo ( IList<T> original ) {

			Changes.ForEach( change => {
				switch ( change.Type ) {
					case DiffOperationTypes.Addition:
						var index = change.StartIndex;
						change.Content?.ForEach( e => {
							original.Insert( index++, e );
						} );
						break;
					case DiffOperationTypes.Removal:
						for ( int i = 0; i < change.Length; i++ ) {
							original.RemoveAt( change.StartIndex );
						}
						break;
				}
			} );
			return original;
		}

		public override string ToString () {

			var sb = new StringBuilder();
			Changes.ForEach( change => sb.AppendLine( change.ToString() ) );
			return sb.ToString();
		}
	}

	[Serializable]
	public class StringDiff {

		public List<StringDiffOperation> Changes { get; private set; }

		public StringDiff ( List<StringDiffOperation> changes ) {

			// sort by start index, descending (always need sto be applied in reverse)
			changes.Sort( ( a, b ) => b.StartIndex.CompareTo( a.StartIndex ) );

			Changes = changes;
		}

		public string ApplyTo ( string original ) {

			for ( int i = Changes.LastIndex(); i >= 0; i-- ) {
				var change = Changes[i];
				switch ( change.Type ) {
					case DiffOperationTypes.Addition:
						original.Insert( change.StartIndex, change.Content );
						break;
					case DiffOperationTypes.Removal:
						original.Remove( change.StartIndex, change.Length );
						break;
				}
			}
			return original;
		}

		public override string ToString () {

			var sb = new StringBuilder();
			Changes.ForEach( change => sb.AppendLine( change.ToString() ) );
			return sb.ToString();
		}
	}

	public struct LongestCommonSubsequence<T>
		where T : IEquatable<T> {

		public int[,] Length { get; private set; }

		public LongestCommonSubsequence ( IList<T> a, IList<T> b ) {

			Length = ComputeLength( a, b );
		}

		public static int[,] ComputeLength ( IList<T> a, IList<T> b ) {

			var m = a.Count;
			var n = b.Count;

			var lcs = new int[m + 1, n + 1];

			for ( int i = 0; i < m + 1; i++ ) {
				for ( int j = 0; j < n + 1; j++ ) {
					if ( i == 0 || j == 0 ) {
						lcs[i, j] = 0;
					} else if ( a[i - 1].Equals( b[j - 1] ) ) {
						lcs[i, j] = 1 + lcs[i - 1, j - 1];
					} else {
						lcs[i, j] = Math.Max( lcs[i - 1, j], lcs[i, j - 1] );
					}
				}
			}

			return lcs;
		}

		public static List<T> Compute ( IList<T> a, IList<T> b ) {

			var subsequence = new List<T>();
			var length = ComputeLength( a, b );

			// drain i or j to zero
			var i = a.Count;
			var j = b.Count;
			while ( i != 0 && j != 0 ) {

				// if the same, add to results
				if ( a[i - 1].Equals( b[j - 1] ) ) {
					subsequence.Add( a[i - 1] );
					i--;
					j--;
				}

				// if not the same, follow the largest
				else if ( length[i - 1, j] >= length[i, j - 1] ) {
					i--;
				} else {
					j--;
				}
			}

			// reverse the result
			subsequence.Reverse();
			return subsequence;
		}
	}
	public struct LongestCommonSubstring {

		public int[,] Length { get; private set; }

		public LongestCommonSubstring ( string a, string b ) {

			Length = ComputeLength( a, b );
		}

		public static int[,] ComputeLength ( string a, string b ) {

			var m = a.Length;
			var n = b.Length;

			var lcs = new int[m + 1, n + 1];

			for ( int i = 0; i < m + 1; i++ ) {
				for ( int j = 0; j < n + 1; j++ ) {
					if ( i == 0 || j == 0 ) {
						lcs[i, j] = 0;
					} else if ( a[i - 1] == b[j - 1] ) {
						lcs[i, j] = 1 + lcs[i - 1, j - 1];
					} else {
						lcs[i, j] = Math.Max( lcs[i - 1, j], lcs[i, j - 1] );
					}
				}
			}

			return lcs;
		}

		public static string Compute ( string a, string b ) {

			var sb = new StringBuilder();
			var length = ComputeLength( a, b );

			// drain i or j to zero
			var i = a.Length;
			var j = b.Length;
			while ( i != 0 && j != 0 ) {

				// if the same, add to results
				if ( a[i - 1] == b[j - 1] ) {
					sb.Append( a[i - 1] );
					i--;
					j--;
				}

				// if not the same, follow the largest
				else if ( length[i - 1, j] >= length[i, j - 1] ) {
					i--;
				} else {
					j--;
				}
			}

			// reverse the result
			var result = new StringBuilder();
			for ( int r = sb.Length; r > 0; r-- ) { result.Append( sb[r - 1] ); }
			return result.ToString();
		}
	}

	public static class IListExtensions {

		public static List<T> GetRange<T> ( this IList<T> list, int index, int count ) {

			if ( list == null ) {
				return null;
			}

			var result = new List<T>( count );
			var stop = index + count;
			for ( int i = index; i < stop; i++ ) {
				result.Add( list[i] );
			}
			return result;
		}
	}

	public class DiffUtil {

		public static SequenceDiff<T> CreateSequenceDiff<T> ( IList<T> oldData, IList<T> newData )
			where T : IEquatable<T> {

			var results = new List<SequenceDiffOperation<T>>();
			var lcsLength = new LongestCommonSubsequence<T>( oldData, newData ).Length;

			var lastOp = (DiffOperationTypes?)null;
			var trailingOldIndex = -1;
			var trailingNewIndex = -1;

			var oldIndex = oldData.Count;
			var newIndex = newData.Count;
			while ( !( oldIndex == 0 && newIndex == 0 ) ) {

				var newOp = (DiffOperationTypes?)null;

				// if elements are the same, nothing happened
				if ( oldData[oldIndex - 1].Equals( newData[newIndex - 1] ) ) {  // -1 to go from lcs.Length -> data index
					/* do nothing... just catch this case */
				}

				// if one sequence is drained, the rest of the other sequence is logged as all + or -
				else if ( oldIndex == 0 ) {
					newOp = DiffOperationTypes.Addition;
				} else if ( newIndex == 0 ) {
					newOp = DiffOperationTypes.Removal;
				}

				// if elements not the same, backtrack with bias for addition in new sequence
				else if ( lcsLength[oldIndex, newIndex - 1] >= lcsLength[oldIndex - 1, newIndex] ) {
					newOp = DiffOperationTypes.Addition;
				} else {
					newOp = DiffOperationTypes.Removal;
				}

				// if op changed, it started/ended
				if ( newOp != lastOp ) {

					// if last op not null, commit that op to results
					if ( lastOp.HasValue ) {

						var length = lastOp.Value switch {
							DiffOperationTypes.Removal => trailingOldIndex - oldIndex,
							_ => 0,
						};
						var content = lastOp.Value switch {
							DiffOperationTypes.Addition => newData.GetRange( newIndex, trailingNewIndex - newIndex ),
							_ => null
						};
						var op = new SequenceDiffOperation<T>(
							type: lastOp.Value,
							start: oldIndex,
							length: length,
							content: content
						);
						results.Add( op );
					}

					// something new is happening, mark indices
					trailingOldIndex = oldIndex;
					trailingNewIndex = newIndex;
				}

				// decrement indices
				if ( newOp == null || newOp == DiffOperationTypes.Addition ) { newIndex--; }
				if ( newOp == null || newOp == DiffOperationTypes.Removal ) { oldIndex--; }

				// log op
				lastOp = newOp;
			}

			// return diff
			return new SequenceDiff<T>( results );
		}

		public static StringDiff CreateStringDiff ( string oldString, string newString ) {

			var results = new List<StringDiffOperation>();
			var lcsLength = new LongestCommonSubstring( oldString, newString ).Length;

			var lastOp = (DiffOperationTypes?)null;
			var trailingOldIndex = -1;
			var trailingNewIndex = -1;

			// drain i and j to zero
			var oldIndex = oldString.Length;
			var newIndex = newString.Length;
			while ( !( oldIndex == 0 && newIndex == 0 ) ) {

				var newOp = (DiffOperationTypes?)null;

				// if chars are the same, nothing happened
				if ( oldString[oldIndex - 1] == newString[newIndex - 1] ) {  // -1 to go from lcs.Length -> string index
					/* do nothing... just catch this case */
				}

				// if one string is drained, the rest of the other string is logged as all + or -
				else if ( oldIndex == 0 ) {
					newOp = DiffOperationTypes.Addition;
				} else if ( newIndex == 0 ) {
					newOp = DiffOperationTypes.Removal;
				}

				// if chars not the same, backtrack with bias for addition in new string
				else if ( lcsLength[oldIndex, newIndex - 1] >= lcsLength[oldIndex - 1, newIndex] ) {
					newOp = DiffOperationTypes.Addition;
				} else {
					newOp = DiffOperationTypes.Removal;
				}

				// if op changed, it started/ended
				if ( newOp != lastOp ) {

					// if last op not null, commit that op to results
					if ( lastOp.HasValue ) {

						var length = lastOp.Value switch {
							DiffOperationTypes.Removal => trailingOldIndex - oldIndex,
							_ => 0,
						};
						var content = lastOp.Value switch {
							DiffOperationTypes.Addition => newString.Substring( newIndex, trailingNewIndex - newIndex ),
							_ => ""
						};
						var op = new StringDiffOperation(
							type: lastOp.Value,
							start: oldIndex,
							length: length,
							content: content
						);
						results.Add( op );
					}

					// something new is happening, mark indices
					trailingOldIndex = oldIndex;
					trailingNewIndex = newIndex;
				}

				// decrement indices
				if ( newOp == null || newOp == DiffOperationTypes.Addition ) { newIndex--; }
				if ( newOp == null || newOp == DiffOperationTypes.Removal ) { oldIndex--; }

				// log op
				lastOp = newOp;
			}

			// return delta
			return new StringDiff( results );
		}
	}
}
