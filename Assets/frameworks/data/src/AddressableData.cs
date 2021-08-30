using System;
using System.Collections.Generic;

namespace Jakintosh.Data {

	[Serializable]
	public class Address : IIdentifiable<string> {

		public string Identifier => _address;

		public Address () : this( null ) { }
		public Address ( string address ) => _address = address;

		public override string ToString () => $"ADDR::{_address}";

		protected string _address;
	}

	[Serializable]
	public class MutableAddress : Address {

		public string Parent => _parent;

		public MutableAddress () : this( address: null, parent: null ) { }
		public MutableAddress ( string address, Address parent = null ) : base( address ) => _parent = parent?.Identifier;

		public override string ToString () => $"MUTA::{_parent} => {Identifier}";

		private string _parent;
	}

	[Serializable]
	public class AddressableData<TData>
		where TData : class,
			IBytesSerializable,
			IDuplicatable<TData>,
			IUpdatable<TData>,
			new() {

		public AddressableData () {

			_data = new Dictionary<string, TData>();
			_handlers = new Dictionary<string, List<Action<TData>>>();
			_forks = new Dictionary<string, string>();
		}

		// public interface
		public MutableAddress New () {

			var mutableAddress = GenerateMutableAddress();
			var mutableData = new TData();
			mutableData.OnUpdated.AddListener( data => {
				if ( _handlers.TryGetValue( mutableAddress.Identifier, out var handlers ) ) {
					handlers?.ForEach( handler => handler( data ) );
				}
			} );
			_data.Add( mutableAddress.Identifier, mutableData );
			return mutableAddress;
		}
		public void Drop ( MutableAddress mutableAddress ) {

			var key = mutableAddress.Identifier;

			if ( _data.ContainsKey( key ) ) {
				var data = GetData( key );
				data.OnUpdated.RemoveAllListeners();
				_data.Remove( key );
			}

			if ( _handlers.ContainsKey( key ) ) {
				_handlers.Remove( key );
			}

			if ( mutableAddress.Parent != null ) {
				_forks.Remove( mutableAddress.Parent );
			}
		}
		public TData GetCopy ( Address address ) {

			return GetData( address.Identifier )?.Duplicate();
		}
		public TData GetLatestCopy ( Address address ) {

			TData data;

			// try as mutable address
			var mutableAddress = address as MutableAddress;
			if ( mutableAddress != null ) {

				data = GetLatestMutable( mutableAddress );

			} else if ( _forks.TryGetValue( address.Identifier, out var forkedAddress ) ) {

				data = GetData( forkedAddress );

			} else {

				data = GetData( address.Identifier );
			}

			return data?.Duplicate();
		}
		public TData GetMutable ( MutableAddress mutableAddress ) {

			return GetData( mutableAddress.Identifier );
		}
		public bool GetLatestMutable ( Address address, out TData data ) {

			// try as mutable address
			var mutableAddress = address as MutableAddress;
			if ( mutableAddress != null ) {
				data = GetLatestMutable( mutableAddress );
				return data != null;
			}

			// if not mutable, get fork
			if ( _forks.TryGetValue( address.Identifier, out var forkedAddress ) ) {
				data = GetData( forkedAddress );
				return data != null;
			}

			// if fail, no latest mutable
			data = null;
			return false;
		}
		public TData GetLatestMutable ( MutableAddress mutableAddress ) {

			return GetData( mutableAddress.Identifier );
		}
		public MutableAddress Fork ( Address address ) {

			// if already exists, just return it
			if ( _forks.TryGetValue( address.Identifier, out var soft ) ) {
				return new MutableAddress( soft, parent: address );
			}

			// if not, create the fork
			var data = GetData( address.Identifier );
			var mutableAddress = GenerateMutableAddress( parent: address );
			var duplicate = data.Duplicate();
			_data.Add( mutableAddress.Identifier, data );
			_forks.Add( address.Identifier, mutableAddress.Identifier );
			return mutableAddress;
		}
		public Address Commit ( MutableAddress mutableAddress ) {

			var data = GetData( mutableAddress.Identifier );
			Drop( mutableAddress );
			return Commit( data.Duplicate() );
		}
		public Address Commit ( TData data ) {

			var contentHash = Hasher.HashDataToBase64String( data );
			data.OnUpdated.AddListener( data => {
				var handlers = _handlers[contentHash];
				handlers?.ForEach( handler => handler( data ) );
			} );
			_data[contentHash] = data;
			return new Address( contentHash );
		}
		public void Subscribe ( Address address, Action<TData> handler ) {

			var mutableAddress = address as MutableAddress;
			var key = mutableAddress?.Parent ?? address.Identifier;
			if ( !_handlers.TryGetValue( key, out var list ) ) {
				list = new List<Action<TData>>();
				_handlers[key] = list;
			}
			list.Add( handler );
		}
		public void Unsubscribe ( Address address, Action<TData> handler ) {

			var mutableAddress = address as MutableAddress;
			var key = mutableAddress != null ? mutableAddress.Parent : address.Identifier;
			if ( _handlers.TryGetValue( key, out var list ) ) {
				list.Remove( handler );
			}
		}

		// serialized data
		private Dictionary<string, TData> _data;
		private Dictionary<string, string> _forks;

		// runtime data
		[NonSerialized] private Dictionary<string, List<Action<TData>>> _handlers;

		// private funcs
		private TData GetData ( string key ) {

			if ( _data.TryGetValue( key, out var data ) ) {
				return data;
			}
			return null;
		}
		private MutableAddress GenerateMutableAddress ( Address parent = null ) {

			int i = 0;
			string key;
			var time = System.DateTime.Now.ToString( "o" );
			do {
				key = $"{time}-{i}";
			} while ( _data.ContainsKey( key ) );
			return new MutableAddress( address: key, parent );
		}
	}
}
