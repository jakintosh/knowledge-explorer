using Jakintosh.Data;
using System;
using System.Collections.Generic;

namespace Coalescent.Computer {

	[Serializable]
	public class MutableAddress : IAddressable {

		public StoreTypes Type => StoreTypes.Mutable;
		public string Identifier => identifier;

		public MutableAddress ( string identifier, string branch = null ) {
			this.identifier = identifier;
			this.branch = branch;
		}

		private string identifier;
		private string branch;
	}


	[Serializable]
	public class Delta<TData, TDiff> : IBytesSerializable, IHashable
		where TData : IBytesSerializable
		where TDiff : IBytesSerializable {

		// properties
		public TDiff Diff => _diff;
		public string Author => _author;
		public string Previous => _previous;

		// constructor
		public Delta ( TDiff diff, string authorId, Delta<TData, TDiff> previous = null ) {

			_diff = diff;
			_author = authorId;
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

		Scratchpad

		This is where you can create new instances of data, modify the data, and commit it back to the store.

		this is different from "in memory" vs "on disk", this is about data that is conceptualy allowed
		to mutate.

		create some fresh empty data that has no history
		commit it to the 

	// have a way to store mutable stuff in memory, but then "flush" it to disk
	// this way we get the "star" thing
	// so like, changes are made in memory, you save it, it saves to disk, next time
	// someone asks for it, its already on disk (maybe we also keep an in-memory cache)

	*/

	public class MutableStore<TData, TDiff>
		where TData : class,
			IBytesSerializable,
			IDiffable<TData, TDiff>,
			IDuplicatable<TData>,
			IUpdatable<TData>,
			new()
		where TDiff : IBytesSerializable {

		public MutableStore ( Store store, Cache cache ) {

			this.store = store;
			this.cache = new Dictionary<string, TData>();
			this.schema = TypeBuilder.Parse( typeof( TData ) );
			this.schemaHash = this.schema.Hash();
		}

		public MutableAddress New () {

			var guid = System.Guid.NewGuid().ToString();
			var data = new TData();
			cache.Add(
				key: guid,
				value: data
			);
			return new MutableAddress( guid );
		}
		public MutableAddress Fork ( ContentAddress contentAddress, string branchTag = null ) {

			var identifier = contentAddress.Identifier;
			if ( !cache.ContainsKey( identifier ) ) {
				var original = store.Get<TData>( contentAddress );
				var duplicate = original.Duplicate();
				cache.Add(
					key: identifier,
					value: duplicate
				);
			}
			return new MutableAddress( identifier, branch: branchTag );
		}
		public (ContentAddress deltaAddress, ContentAddress? dataAddress)? Commit ( MutableAddress address, Delta<TData, TDiff> previousDelta = null, bool storeContent = false ) {

			// get the data
			if ( !cache.TryGetValue( address.Identifier, out var fork ) ) {
				// we don't have the data...
				return null;
			}
			var original = store.Get<TData>( new ContentAddress( address.Identifier ) );

			// generate a delta
			var diff = fork.Diff( from: original );
			var delta = new Delta<TData, TDiff>(
				diff: diff,
				authorId: "<author-id>",
				previous: previousDelta
			);

			// drop mutable
			cache.Remove( address.Identifier );

			// store data
			var deltaAddress = store.Put( delta );
			var dataAddress = storeContent ? store.Put( fork ) : (ContentAddress?)null;

			return (deltaAddress, dataAddress);
		}
		public void Drop ( MutableAddress address ) {

			cache.Remove( address.Identifier );
		}

		private Store store;
		private Dictionary<string, TData> cache;
		private readonly TypeBuilder schema;
		private readonly string schemaHash;
	}

}