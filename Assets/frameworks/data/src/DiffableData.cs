using System;
using System.Collections.Generic;

namespace Jakintosh.Data {

	[Serializable]
	public class Delta<TData, TDiff> :
		IBytesSerializable,
		IHashable
		where TData : IBytesSerializable
		where TDiff : IBytesSerializable {

		// properties
		public TDiff Diff => _diff;
		public string Author => _author;
		public string Previous => _previous;

		// constructor
		public Delta ( TDiff diff, string authorPubKey, Delta<TData, TDiff> previous = null ) {

			_diff = diff;
			_author = authorPubKey;
			_previous = previous?.GetBase64Hash();
		}

		// interface implementations
		public byte[] GetSerializedBytes () => Serializer.GetSerializedBytes( this );
		public byte[] GetBytesHash () => Hasher.HashBytes( GetSerializedBytes() );
		public string GetBase64Hash () => Hasher.HashDataToBase64String( this );

		// serialized data
		private TDiff _diff;
		private string _author;
		private string _previous;
	}

	/*
		Diffable data is for creating delta chains.

		You submit some data, the address to a previous delta, and it will diff
		them, and create a new delta object that contains the changes from the
		last object.
	*/
	public class DiffableData<TData, TDiff>
		where TData :
			class,
			IBytesSerializable,
			IDiffable<TData, TDiff>,
			IDuplicatable<TData>,
			IUpdatable<TData>,
			new()
		where TDiff : IBytesSerializable {

		public DiffableData ( AddressableData<TData> addressableDataRepo ) {

			_deltas = new Dictionary<string, Delta<TData, TDiff>>();
			_deltaToContentMap = new Dictionary<string, string>();
			_addressableContent = addressableDataRepo;
		}

		public Delta<TData, TDiff> GetDelta ( Address deltaAddress ) {

			if ( _deltas.TryGetValue( deltaAddress.Identifier, out var delta ) ) {
				return delta;
			}

			// error
			return null;
		}
		public Address Commit ( TData data, string author, bool compile = false, Address previousDeltaAddress = null ) {

			if ( data == null ) {
				// error
			}

			// create the diff
			TData prevData;
			if ( previousDeltaAddress != null ) {
				prevData = GetData( previousDeltaAddress );
				if ( prevData == null ) {
					// couldn't get data. error
				}
			} else {
				prevData = new TData();
			}
			var diff = data.Diff( from: prevData );

			// grab prev delta
			Delta<TData, TDiff> prevDelta = null;
			if ( previousDeltaAddress != null ) {
				if ( !_deltas.TryGetValue( previousDeltaAddress.Identifier, out prevDelta ) ) {
					// has delta, but can't find it. error
				}
			}

			// create delta
			var delta = new Delta<TData, TDiff>( diff, author, prevDelta );
			var deltaHash = delta.GetBase64Hash();
			_deltas[deltaHash] = delta;


			// commit compiled data if necessary
			var shouldCompile = compile || ( previousDeltaAddress == null );
			if ( shouldCompile ) {
				var contentAddress = _addressableContent.Commit( data.Duplicate() );
				_deltaToContentMap.Add( deltaHash, contentAddress.Identifier );
			}

			return new Address( deltaHash );
		}
		public TData GetData ( Address deltaAddress ) {

			// return associated content if available
			if ( _deltaToContentMap.TryGetValue( deltaAddress.Identifier, out var contentAddress ) ) {
				return _addressableContent.GetCopy( new Address( contentAddress ) );
			}

			// if no delta at address, abort
			if ( !_deltas.TryGetValue( deltaAddress.Identifier, out var delta ) ) {
				return null; // missing delta
			}

			// if delta has no parent, abort
			if ( delta.Previous == null ) {
				return null; // no content, and also no parent: can't generate chain
			}

			var content = GetData( new Address( delta.Previous ) );
			return content.Apply( delta.Diff );
		}

		// serialized data
		private Dictionary<string, Delta<TData, TDiff>> _deltas;
		private Dictionary<string, string> _deltaToContentMap;

		// runtime data
		[NonSerialized] private AddressableData<TData> _addressableContent;
	}

}
