using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jakintosh.Data {

	public enum DiffOperationTypes {
		Removal = -1,
		Addition = 1
	}

	[Serializable]
	public class SequenceDiffOperation<T> : IEquatable<SequenceDiffOperation<T>>
		where T : IEquatable<T> {

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


		public bool Equals ( SequenceDiffOperation<T> other ) {

			var equal = Type.Equals( other.Type ) && StartIndex.Equals( other.StartIndex ) && Length.Equals( other.Length );
			if ( Content != null ) { equal = equal && Enumerable.SequenceEqual( Content, other.Content ); }
			return equal;
		}
		public override bool Equals ( object obj ) {

			if ( obj == null ) { return false; }
			var diffObj = obj as SequenceDiffOperation<T>;
			if ( diffObj == null ) { return false; }
			return this.Equals( diffObj );
		}
		public override int GetHashCode () {

			var hashCode = (int)Type ^ StartIndex ^ Length;
			if ( Content != null ) {
				Content.ForEach( element => hashCode ^= ( element.GetHashCode() * 31 ) );
			}
			return hashCode;
		}
	}

	[Serializable]
	public class StringDiffOperation : IEquatable<StringDiffOperation> {

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

		public bool Equals ( StringDiffOperation other ) {

			var equal = Type.Equals( other.Type ) && StartIndex.Equals( other.StartIndex ) && Length.Equals( other.Length );
			if ( Content != null ) { equal = equal && Content.Equals( other.Content ); }
			return equal;
		}
		public override bool Equals ( object obj ) {

			if ( obj == null ) { return false; }
			var diffObj = obj as StringDiffOperation;
			if ( diffObj == null ) { return false; }
			return this.Equals( diffObj );
		}
		public override int GetHashCode () {

			return (int)Type ^ StartIndex ^ Length ^ Content.GetHashCode();
		}
	}

	[Serializable]
	public class SequenceDiff<T> : IEquatable<SequenceDiff<T>>
		where T : IEquatable<T> {

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


		public bool Equals ( SequenceDiff<T> other ) {

			if ( other == null ) {
				return false;
			}

			if ( Changes != null && other.Changes != null ) {
				return Enumerable.SequenceEqual( Changes, other.Changes );
			}
			return Changes == null && other.Changes == null;
		}
		public override bool Equals ( object obj ) {

			if ( obj == null ) { return false; }
			var diffObj = obj as SequenceDiff<T>;
			if ( diffObj == null ) { return false; }
			return this.Equals( diffObj );
		}
		public override int GetHashCode () {

			int hashCode = 0;
			Changes.ForEach( change => hashCode ^= ( change.GetHashCode() * 31 ) );
			return hashCode;
		}
	}

	[Serializable]
	public class StringDiff : IEquatable<StringDiff> {

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

		public bool Equals ( StringDiff other ) {

			if ( other == null ) {
				return false;
			}

			if ( Changes != null && other.Changes != null ) {
				return Enumerable.SequenceEqual( Changes, other.Changes );
			}
			return Changes == null && other.Changes == null;
		}
		public override bool Equals ( object obj ) {

			if ( obj == null ) { return false; }
			var diffObj = obj as StringDiff;
			if ( diffObj == null ) { return false; }
			return this.Equals( diffObj );
		}
		public override int GetHashCode () {

			int hashCode = 0;
			Changes.ForEach( change => hashCode ^= ( change.GetHashCode() * 31 ) );
			return hashCode;
		}
	}

	public struct LongestCommonSubsequence<T>
		where T : IEquatable<T> {

		public int[,] Length { get; private set; }

		public LongestCommonSubsequence ( IList<T> a, IList<T> b ) {

			Length = ComputeLength( a, b );
		}

		public static int[,] ComputeLength ( IList<T> a, IList<T> b ) {

			// treat null as empty
			if ( a == null ) { a = new List<T>(); }
			if ( b == null ) { b = new List<T>(); }

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

			// treat null as empty
			if ( a == null ) { a = new List<T>(); }
			if ( b == null ) { b = new List<T>(); }

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

			// treat null as empty
			if ( a == null ) { a = ""; }
			if ( b == null ) { b = ""; }

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

			// treat null as empty
			if ( a == null ) { a = ""; }
			if ( b == null ) { b = ""; }

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

			// treat null as empty
			if ( oldData == null ) { oldData = new List<T>(); }
			if ( newData == null ) { newData = new List<T>(); }

			var results = new List<SequenceDiffOperation<T>>();
			var lcsLength = new LongestCommonSubsequence<T>( oldData, newData ).Length;

			var lastOp = (DiffOperationTypes?)null;
			var trailingOldIndex = -1;
			var trailingNewIndex = -1;

			var oldIndex = oldData.Count;
			var newIndex = newData.Count;

			// define a local func to commit an operation to results
			void CommitOperation () {

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

			while ( !( oldIndex == 0 && newIndex == 0 ) ) {

				var newOp = (DiffOperationTypes?)null;

				// if one sequence is drained, the rest of the other sequence is logged as all + or -
				if ( oldIndex == 0 ) {
					newOp = DiffOperationTypes.Addition;
				} else if ( newIndex == 0 ) {
					newOp = DiffOperationTypes.Removal;
				}

				// if elements are the same, nothing happened
				else if ( oldData[oldIndex - 1].Equals( newData[newIndex - 1] ) ) {  // -1 to go from lcs.Length -> data index
					/* do nothing... just catch this case */
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
						CommitOperation();
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

			// if we had an uncommitted op at the end, commit it
			if ( lastOp.HasValue ) {
				CommitOperation();
			}

			// return diff
			return new SequenceDiff<T>( results );
		}

		public static StringDiff CreateStringDiff ( string oldString, string newString ) {

			// treat null as empty
			if ( oldString == null ) { oldString = ""; }
			if ( newString == null ) { newString = ""; }

			var results = new List<StringDiffOperation>();
			var lcsLength = new LongestCommonSubstring( oldString, newString ).Length;

			var lastOp = (DiffOperationTypes?)null;
			var trailingOldIndex = -1;
			var trailingNewIndex = -1;

			// drain i and j to zero
			var oldIndex = oldString.Length;
			var newIndex = newString.Length;

			// define a local func to commit an operation to results
			void CommitOperation () {

				var length = lastOp.Value switch {
					DiffOperationTypes.Removal => trailingOldIndex - oldIndex,
					_ => 0,
				};
				var content = lastOp.Value switch {
					DiffOperationTypes.Addition => newString.Substring( newIndex, trailingNewIndex - newIndex ),
					_ => null
				};
				var op = new StringDiffOperation(
					type: lastOp.Value,
					start: oldIndex,
					length: length,
					content: content
				);
				results.Add( op );
			}

			while ( oldIndex != 0 || newIndex != 0 ) {

				var newOp = (DiffOperationTypes?)null;

				// if one string is drained, the rest of the other string is logged as all + or -
				if ( oldIndex == 0 ) {
					newOp = DiffOperationTypes.Addition;
				} else if ( newIndex == 0 ) {
					newOp = DiffOperationTypes.Removal;
				}

				// if chars are the same, nothing happened
				else if ( oldString[oldIndex - 1] == newString[newIndex - 1] ) {  // -1 to go from lcs.Length -> string index
					/* do nothing... just catch this case */
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
						CommitOperation();
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

			// if we had an uncommitted op at the end, commit it
			if ( lastOp.HasValue ) {
				CommitOperation();
			}

			// return delta
			return new StringDiff( results );
		}
	}
}
