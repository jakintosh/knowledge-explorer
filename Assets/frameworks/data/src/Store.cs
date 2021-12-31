using Jakintosh.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace Coalescent.Computer {


	// EXCEPTIONS

	public class DataNotFoundException : System.Exception { }
	public class DataUnreadableException : System.Exception { }


	// INTERFACES

	public interface IAddressable {
		StoreTypes Type { get; }
		string Identifier { get; }
	}

	[Serializable]
	public struct ContentAddress : IAddressable {

		public StoreTypes Type => StoreTypes.Immutable;
		public string Identifier => identifier;

		public ContentAddress ( string identifier ) => this.identifier = identifier;
		private string identifier;
	}


	// ENUMS

	public enum StoreTypes {
		Immutable,
		Mutable
	}

	// CLASSES

	public class Cache {

		// ********** Public Interface **********

		public Cache () {

			_store = new Dictionary<string, byte[]>();
			_var = new Dictionary<string, byte[]>();
		}

		public bool Hold ( IAddressable address, byte[] bytes ) {

			var cache = CacheFor( address.Type );
			if ( cache.ContainsKey( address.Identifier ) ) {
				return false;
			}
			cache.Add( address.Identifier, bytes );
			return true;
		}
		public bool Drop ( IAddressable address ) {

			var cache = CacheFor( address.Type );
			return cache.Remove( address.Identifier );
		}
		public byte[] Fetch ( IAddressable address ) {

			var cache = CacheFor( address.Type );
			if ( cache == null ) { return null; }
			if ( !cache.TryGetValue( address.Identifier, out var bytes ) ) { return null; }
			return bytes;
		}


		// ********** Private Interface **********

		private Dictionary<string, byte[]> _store;
		private Dictionary<string, byte[]> _var;

		private Dictionary<string, byte[]> CacheFor ( StoreTypes type ) => type switch {
			StoreTypes.Immutable => _store,
			StoreTypes.Mutable => _var,
			_ => null
		};
	}

	public class Store {

		// ********** Public Interface **********

		public Store ( Cache cache, string rootDiskPath ) {

			_cache = cache;

			var fullRootPath = Path.GetFullPath( rootDiskPath );
			_immutablePath = Path.Combine( fullRootPath, "store" );
			_mutablePath = Path.Combine( fullRootPath, "var" );
			EnsureDirectory( _immutablePath );
			EnsureDirectory( _mutablePath );
		}

		public T Get<T> ( IAddressable address ) where T : new() {

			// get from cache
			var bytes = _cache.Fetch( address );

			// if not found from cache, get from disk
			if ( bytes == null ) {

				var dataFilePath = PathFor<T>( address );
				if ( !File.Exists( dataFilePath ) ) {
					throw new DataNotFoundException();
				}

				try {
					bytes = File.ReadAllBytes( dataFilePath );
					_cache.Hold( address, bytes );
				} catch {
					throw new DataUnreadableException();
				}
			}

			// deserialize and return
			try {
				var data = Serializer.DeserializeBytes<T>( bytes );
				return data;
			} catch {
				throw new DataUnreadableException();
			}
		}
		public ContentAddress Put<T> ( T data ) where T : IBytesSerializable {

			// serialize data
			var bytes = Serializer.GetSerializedBytes( data );

			// get hashes
			var dataHash = Hasher.HashBytesToBase64String( bytes );
			var typeHash = TypeBuilder.Parse( typeof( T ) ).Hash();
			var address = new ContentAddress( dataHash );

			// ensure directory exists for type in '/store'
			var typeFilePath = Path.Combine( _immutablePath, Base64ToFileString( typeHash ) );
			EnsureDirectory( typeFilePath );

			// create file path
			var dataFilePath = Path.Combine( typeFilePath, Base64ToFileString( dataHash ) );
			dataFilePath = Path.ChangeExtension( dataFilePath, FILE_EXT );

			// write, if not already written
			if ( !File.Exists( dataFilePath ) ) {
				File.WriteAllBytes( dataFilePath, bytes );
				_cache.Hold( address, bytes );
			} else {
				// maybe log something here
			}

			return address;
		}
		public bool Delete<T> ( IAddressable address ) {

			var isInCache = _cache.Drop( address );

			var dataFilePath = PathFor<T>( address );
			var isOnDisk = File.Exists( dataFilePath );
			if ( isOnDisk ) {
				File.Delete( dataFilePath );
			}

			return isInCache || isOnDisk;
		}

		// ********** Private Interface **********

		private Cache _cache;
		private string _immutablePath;
		private string _mutablePath;

		private string PathFor ( StoreTypes type ) => type switch {
			StoreTypes.Immutable => _immutablePath,
			_ => _mutablePath
		};
		private string PathFor<T> ( IAddressable address ) {

			// get type hash
			var typeHash = TypeBuilder.Parse( typeof( T ) ).Hash();

			// build path
			var dataFilePath = Path.Combine(
				PathFor( address.Type ),
				Base64ToFileString( typeHash ),
				Base64ToFileString( address.Identifier )
			);
			dataFilePath = Path.ChangeExtension( dataFilePath, FILE_EXT );
			return dataFilePath;
		}


		// static

		private const string FILE_EXT = ".ccp";

		private static string Base64ToFileString ( string b64String ) => b64String.Replace( '/', '{' );
		private static string FileStringToBase64 ( string fileString ) => fileString.Replace( '{', '/' );

		private static void EnsureDirectory ( string path ) {

			if ( !Directory.Exists( path ) ) {
				var lastSlash = path.LastIndexOf( '/' );
				if ( lastSlash != -1 ) {
					var subPath = path.Substring( 0, lastSlash );
					EnsureDirectory( subPath );
					Directory.CreateDirectory( path );
				} else {
					return;
				}
			}
		}
	}
}
