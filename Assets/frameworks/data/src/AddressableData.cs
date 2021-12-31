using Coalescent.Computer;
using System;
using System.Collections.Generic;

namespace Jakintosh.Data {

	[Serializable]
	public class Address : IIdentifiable<string>, IEquatable<Address> {

		public string Identifier => _address;

		public Address () : this( null ) { }
		public Address ( string address ) => _address = address;

		public override string ToString () => $"ADDR::{_address}";

		public bool Equals ( Address other ) => _address.Equals( other?._address );
		public override bool Equals ( object obj ) {

			if ( obj == null ) { return false; }

			var addrObj = obj as Address;
			if ( addrObj == null ) { return false; }

			return Equals( addrObj );
		}
		public override int GetHashCode () => _address.GetHashCode();

		protected string _address;
	}

	[Serializable]
	public class MutableAddress : Address, IEquatable<MutableAddress> {

		public string Parent => _parent;

		public MutableAddress () : this( address: null, parent: null ) { }
		public MutableAddress ( string address, Address parent = null ) : base( address ) => _parent = parent?.Identifier;

		public override string ToString () => $"MUTA::{_parent} => {Identifier}";

		public bool Equals ( MutableAddress other ) => EqualityComparer<string>.Default.Equals( Parent, other.Parent ) && base.Equals( other );
		public override bool Equals ( object obj ) {

			if ( obj == null ) { return false; }

			var mutaAddrObj = obj as MutableAddress;
			if ( mutaAddrObj == null ) { return false; }

			return Equals( mutaAddrObj );
		}
		public override int GetHashCode () => _parent != null ? _address.GetHashCode() ^ _parent.GetHashCode() : _address.GetHashCode();

		private string _parent;
	}

	public enum AddressEventTypes {
		Created,
		Dropped,
		Forked,
		Committed,
		Invalidated,
		Revalidated
	}

	public class AddressEventArgs : EventArgs {
		public Address Address { get; private set; }
		public AddressEventTypes Type { get; private set; }
		public AddressEventArgs ( Address address, AddressEventTypes type ) {
			Address = address;
			Type = type;
		}
	}

	[Serializable]
	public class AddressableData<TData>
		where TData : class,
			IBytesSerializable,
			IDuplicatable<TData>,
			IUpdatable<TData>,
			new() {

		public AddressableData () {

			_data = new Dictionary<Address, TData>();
			_tempData = new Dictionary<MutableAddress, TData>();
			_invalidAddresses = new HashSet<MutableAddress>();
			_handlers = new Dictionary<string, List<Action<TData>>>();
			_anyHandlers = new List<Action<AddressEventArgs>>();
			_forks = new Dictionary<string, string>();
		}

		// public interface
		public List<Address> GetAllAddresses () {

			var addresses = new HashSet<Address>( _data.Keys );
			var tempAddresses = new HashSet<Address>( _tempData.Keys );
			addresses.UnionWith( tempAddresses );
			addresses.ExceptWith( _invalidAddresses );
			return new List<Address>( addresses );
		}
		public List<MutableAddress> GetAllMutableAddresses () {

			var tempAddresses = new HashSet<MutableAddress>( _tempData.Keys );
			tempAddresses.ExceptWith( _invalidAddresses );
			return new List<MutableAddress>( tempAddresses );
		}
		public List<MutableAddress> GetAllForkedAddresses () {

			return _forks.ConvertToList( ( parent, fork ) =>
				new MutableAddress(
					address: fork,
					parent: new Address( parent )
				)
			);
		}
		public TData GetCopy ( Address address ) {

			return GetData( address )?.Duplicate();
		}
		public TData GetLatestCopy ( Address address ) {

			TData data;

			// try as mutable address
			var mutableAddress = address as MutableAddress;
			if ( mutableAddress != null ) {

				data = GetLatestMutable( mutableAddress );

			} else if ( _forks.TryGetValue( address.Identifier, out var forkedAddressIdentifier ) ) {

				var forkedAddress = new MutableAddress(
					address: forkedAddressIdentifier,
					parent: address
				);
				data = GetData( forkedAddress );

			} else {

				data = GetData( address );
			}

			return data?.Duplicate();
		}
		public TData GetMutable ( MutableAddress mutableAddress ) {

			return GetData( mutableAddress );
		}
		public bool GetLatestMutable ( Address address, out TData data ) {

			// try as mutable address
			var mutableAddress = address as MutableAddress;
			if ( mutableAddress != null ) {
				data = GetLatestMutable( mutableAddress );
				return data != null;
			}

			// if not mutable, get fork
			if ( _forks.TryGetValue( address.Identifier, out var forkedAddressIdentifier ) ) {
				var forkedAddress = new MutableAddress(
					address: forkedAddressIdentifier,
					parent: address
				);
				data = GetData( forkedAddress );
				return data != null;
			}

			// if fail, no latest mutable
			data = null;
			return false;
		}
		public TData GetLatestMutable ( MutableAddress mutableAddress ) {

			return GetData( mutableAddress );
		}

		public MutableAddress New () {

			var mutableAddress = GenerateMutableAddress();
			var mutableData = new TData();
			mutableData.OnUpdated.AddListener( data => {
				// fire for those watching parent
				if ( mutableAddress.Parent != null && _handlers.TryGetValue( mutableAddress.Parent, out var parentHandlers ) ) {
					parentHandlers?.ForEach( handler => handler( data ) );
				}
				// fire for those watching this
				if ( _handlers.TryGetValue( mutableAddress.Identifier, out var handlers ) ) {
					handlers?.ForEach( handler => handler( data ) );
				}
			} );
			_tempData.Add( mutableAddress, mutableData );
			FireEvent( new AddressEventArgs( mutableAddress, AddressEventTypes.Created ) );
			return mutableAddress;
		}
		public void Invalidate ( MutableAddress mutableAddress ) {

			// mark a mutable address as invalid, should appear "dead" to the app
			_invalidAddresses.Add( mutableAddress );
			FireEvent( new AddressEventArgs( mutableAddress, AddressEventTypes.Invalidated ) );
		}
		public void Revalidate ( MutableAddress mutableAddress ) {

			// mark a mutable address as valid again, should be "alive" to the app
			_invalidAddresses.Remove( mutableAddress );
			FireEvent( new AddressEventArgs( mutableAddress, AddressEventTypes.Revalidated ) );
		}
		public void Drop ( MutableAddress mutableAddress ) {

			if ( _tempData.ContainsKey( mutableAddress ) ) {
				var tempData = GetData( mutableAddress, seeInvalids: true );
				tempData.OnUpdated.RemoveAllListeners();
				_tempData.Remove( mutableAddress );
				FireEvent( new AddressEventArgs( mutableAddress, AddressEventTypes.Dropped ) );
			}

			var key = mutableAddress.Identifier;
			if ( _handlers.ContainsKey( key ) ) {
				_handlers.Remove( key );
			}

			if ( mutableAddress.Parent != null ) {
				_forks.Remove( mutableAddress.Parent );
			}

		}
		public MutableAddress Fork ( Address address ) {

			// if already exists, just return it
			if ( _forks.TryGetValue( address.Identifier, out var mutableAddressIdentifier ) ) {
				return new MutableAddress( mutableAddressIdentifier, parent: address );
			}

			// if not, create the fork
			var data = GetData( address );
			var mutableAddress = GenerateMutableAddress( parent: address );
			var duplicate = data.Duplicate();
			duplicate.OnUpdated.AddListener( data => {
				// fire for those watching parent
				if ( mutableAddress.Parent != null && _handlers.TryGetValue( mutableAddress.Parent, out var parentHandlers ) ) {
					parentHandlers?.ForEach( handler => handler( data ) );
				}
				// fire for those watching this
				if ( _handlers.TryGetValue( mutableAddress.Identifier, out var handlers ) ) {
					handlers?.ForEach( handler => handler( data ) );
				}
			} );
			_tempData.Add( mutableAddress, duplicate );
			_forks.Add( address.Identifier, mutableAddress.Identifier );
			FireEvent( new AddressEventArgs( mutableAddress, AddressEventTypes.Forked ) );
			return mutableAddress;
		}
		public Address Commit ( MutableAddress mutableAddress ) {

			var tempData = GetData( mutableAddress );
			Drop( mutableAddress );
			return Commit( tempData.Duplicate() );
		}
		public Address Commit ( TData data ) {

			var contentHash = Hasher.HashDataToBase64String( data );
			var address = new Address( contentHash );
			_data.Add( address, data );
			FireEvent( new AddressEventArgs( address, AddressEventTypes.Committed ) );
			return address;
		}

		public void SubscribeAny ( Action<AddressEventArgs> handler ) {

			_anyHandlers.Add( handler );
		}
		public void UnsubscribeAny ( Action<AddressEventArgs> handler ) {

			_anyHandlers.Remove( handler );
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
		private Dictionary<Address, TData> _data;
		private Dictionary<MutableAddress, TData> _tempData;
		private Dictionary<string, string> _forks;

		// runtime data
		[NonSerialized] private HashSet<MutableAddress> _invalidAddresses;
		[NonSerialized] private Dictionary<string, List<Action<TData>>> _handlers;
		[NonSerialized] private List<Action<AddressEventArgs>> _anyHandlers;

		// private funcs
		private TData GetData ( Address address ) {

			var mutableAddress = address as MutableAddress;
			if ( mutableAddress != null ) {
				return GetData( mutableAddress );
			}

			_data.TryGetValue( address, out var data );
			return data;
		}
		private TData GetData ( MutableAddress address, bool seeInvalids = false ) {

			if ( !seeInvalids && _invalidAddresses.Contains( address ) ) {
				return null;
			}

			_tempData.TryGetValue( address, out var data );
			return data;
		}
		private MutableAddress GenerateMutableAddress ( Address parent = null ) {

			int i = 0;
			MutableAddress address;
			var time = System.DateTime.Now.ToString( "o" );
			do {
				address = new MutableAddress( address: $"{time}-{i++}", parent );
			} while ( _tempData.ContainsKey( address ) );
			return address;
		}
		private void FireEvent ( AddressEventArgs args ) {

			_anyHandlers.ForEach( handler => handler( args ) );
		}
	}
}
