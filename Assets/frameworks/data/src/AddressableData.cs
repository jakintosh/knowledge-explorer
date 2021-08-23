using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.Events;

namespace Jakintosh.Data {

	public interface IReadOnlyConvertible<TInterface> {
		TInterface ToReadOnly ();
	}
	public interface IDuplicatable<T> {
		T Duplicate ();
	}
	public interface IUpdatable<T> {
		UnityEvent<T> OnUpdated { get; }
	}

	[Serializable]
	public class Address : IIdentifiable<string> {

		public string Identifier => _address;

		public Address () : this( null ) { }
		public Address ( string address ) => _address = address;

		public override string ToString () => $"ADDR::{_address}";

		protected string _address;
	}

	[Serializable]
	public class TempAddress : Address {

		public string Parent => _parent;

		public TempAddress () : this( address: null, parent: null ) { }
		public TempAddress ( string address, Address parent = null ) : base( address ) => _parent = parent?.Identifier;

		public override string ToString () => $"FORK::{_parent} => {Identifier}";

		private string _parent;
	}


	[Serializable]
	public class AddressableData<TData, TReadInterface>
		where TData : class,
			IReadOnlyConvertible<TReadInterface>,
			IDuplicatable<TData>,
			IUpdatable<TData>,
			new()
		where TReadInterface : class {

		public AddressableData () {

			_data = new Dictionary<string, TData>();
			_handlers = new Dictionary<string, List<Action<TData>>>();
			_forks = new Dictionary<string, string>();
		}

		// public interface
		public TempAddress New () {

			var tempAddress = GenerateTempAddress();
			var tempData = new TData();
			tempData.OnUpdated.AddListener( data => {
				if ( _handlers.TryGetValue( tempAddress.Identifier, out var handlers ) ) {
					handlers?.ForEach( handler => handler( data ) );
				}
			} );
			_data.Add( tempAddress.Identifier, tempData );
			return tempAddress;
		}
		public void Drop ( TempAddress tempAddress ) {

			var key = tempAddress.Identifier;

			if ( _data.ContainsKey( key ) ) {
				var data = GetData( key );
				data.OnUpdated.RemoveAllListeners();
				_data.Remove( key );
			}

			if ( _handlers.ContainsKey( key ) ) {
				_handlers.Remove( key );
			}

			if ( tempAddress.Parent != null ) {
				_forks.Remove( tempAddress.Parent );
			}
		}
		public TReadInterface Get ( Address address ) {

			return GetData( address.Identifier )?.ToReadOnly();
		}
		public TReadInterface GetLatest ( Address address ) {

			TData data;

			// try as temp address
			var tempAddress = address as TempAddress;
			if ( tempAddress != null ) {

				data = GetLatestMutable( tempAddress );

			} else if ( _forks.TryGetValue( address.Identifier, out var forkedAddress ) ) {

				data = GetData( forkedAddress );

			} else {

				data = GetData( address.Identifier );
			}

			return data?.ToReadOnly();
		}
		public TData GetMutable ( TempAddress tempAddress ) {

			return GetData( tempAddress.Identifier );
		}
		public bool GetLatestMutable ( Address address, out TData data ) {

			// try as temp address
			var tempAddress = address as TempAddress;
			if ( tempAddress != null ) {
				data = GetLatestMutable( tempAddress );
				return data != null;
			}

			// if not temp, get fork
			if ( _forks.TryGetValue( address.Identifier, out var forkedAddress ) ) {
				data = GetData( forkedAddress );
				return data != null;
			}

			// if fail, no latest mutable
			data = null;
			return false;
		}
		public TData GetLatestMutable ( TempAddress tempAddress ) {

			return GetData( tempAddress.Identifier );
		}
		public TempAddress Fork ( Address address ) {

			// if already exists, just return it
			if ( _forks.TryGetValue( address.Identifier, out var soft ) ) {
				return new TempAddress( soft, parent: address );
			}

			// if not, create the fork
			var data = GetData( address.Identifier );
			var tempAddress = GenerateTempAddress( parent: address );
			var duplicate = data.Duplicate();
			_data.Add( tempAddress.Identifier, data );
			_forks.Add( address.Identifier, tempAddress.Identifier );
			return tempAddress;
		}
		public Address Commit ( TempAddress tempAddress ) {

			var data = GetData( tempAddress.Identifier );
			Drop( tempAddress );
			return Commit( data.Duplicate() );
		}
		public Address Commit ( TData data ) {

			var contentHash = Hash( data );
			data.OnUpdated.AddListener( data => {
				var handlers = _handlers[contentHash];
				handlers?.ForEach( handler => handler( data ) );
			} );
			_data[contentHash] = data;
			return new Address( contentHash );
		}
		public void Subscribe ( Address address, Action<TData> handler ) {

			var tempAddress = address as TempAddress;
			var key = tempAddress?.Parent ?? address.Identifier;
			if ( !_handlers.TryGetValue( key, out var list ) ) {
				list = new List<Action<TData>>();
				_handlers[key] = list;
			}
			list.Add( handler );
		}
		public void Unsubscribe ( Address address, Action<TData> handler ) {

			var tempAddress = address as TempAddress;
			var key = tempAddress != null ? tempAddress.Parent : address.Identifier;
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
		private TempAddress GenerateTempAddress ( Address parent = null ) {

			int i = 0;
			string key;
			var time = System.DateTime.Now.ToString( "o" );
			do {
				key = $"{time}-{i}";
			} while ( _data.ContainsKey( key ) );
			return new TempAddress( address: key, parent );
		}
		private string Hash ( TData data ) {

			var formatter = new SouthPointe.Serialization.MessagePack.MessagePackFormatter();
			var sha256 = new SHA256CryptoServiceProvider();
			var bytes = formatter.Serialize<TData>( data );
			var hash = sha256.ComputeHash( bytes );
			return Convert.ToBase64String( hash );
		}
	}

	[Serializable]
	public class SubscribableDictionary<TKey, TModel>
		where TModel : IUpdatable<TModel>, IIdentifiable<TKey> {

		// events
		[NonSerialized] public UnityEvent<TKey> OnAdded = new UnityEvent<TKey>();
		[NonSerialized] public UnityEvent<TKey> OnUpdated = new UnityEvent<TKey>();
		[NonSerialized] public UnityEvent<TKey> OnRemoved = new UnityEvent<TKey>();

		// crud
		public TModel Get ( TKey handle ) {

			return _data[handle];
		}
		public List<TModel> GetAll () {

			return new List<TModel>( _data.Values );
		}
		public void Register ( TModel data ) {

			var handle = data.Identifier;
			data.OnUpdated.AddListener( HandleDataUpdated );
			_data.Add( handle, data );
			OnAdded?.Invoke( handle );
		}
		public void Unregister ( TKey handle ) {

			_data[handle]?.OnUpdated.RemoveListener( HandleDataUpdated );
			_data.Remove( handle );
			OnRemoved?.Invoke( handle );
		}

		// data
		private Dictionary<TKey, TModel> _data = new Dictionary<TKey, TModel>();

		// event handlers
		private void HandleDataUpdated ( TModel data )
			=> OnUpdated?.Invoke( data.Identifier );
	}



}
