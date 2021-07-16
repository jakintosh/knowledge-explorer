using Framework.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Jakintosh.Resources {

	// ******** Exception Definitions ********

	public class ResourceNameEmptyException : Exception { }
	public class ResourceNameConflictException : Exception { }
	public class ResourceFileNameConflictException : Exception { }
	public class ResourceNotLoadedException : Exception { }
	public class ResourceMetadataNotFoundException : Exception { }


	// ******** Metadata Definitions ********

	[Serializable]
	public class Metadata : IEquatable<Metadata> {

		[Serializable]
		public struct Dependency {
			[JsonProperty] public string Type;
			[JsonProperty] public string UID;
		}

		[JsonIgnore] public string UID => uid;
		[JsonIgnore] public string Name => name;
		[JsonIgnore] public string Path => path;
		[JsonIgnore] public Dependency[] Dependencies => dependencies;

		public void SetResourceData ( string uid, string name, string path ) {

			this.uid = uid;
			this.name = name;
			this.path = path;
		}

		[JsonProperty] private string uid;
		[JsonProperty] private string name;
		[JsonProperty] private string path;
		[JsonProperty] private Dependency[] dependencies;


		// ********** IEquatable Implementation **********

		bool IEquatable<Metadata>.Equals ( Metadata other )
			=> ( other?.UID.Equals( this.UID ) ?? false ) &&
			   ( other?.Name.Equals( this.Name ) ?? false ) &&
			   ( other?.Path.Equals( this.Path ) ?? false );
	}

	// TODO: Resources need dependencies

	[Serializable]
	public class Resources<TMetadata, TResource>
		where TMetadata : Metadata, new()
		where TResource : class, new() {


		// *********** Public Interface ***********

		public event Framework.Event<IList<TMetadata>>.Signature OnAnyMetadataChanged;
		public event Framework.Event<TMetadata>.Signature OnMetadataAdded;
		public event Framework.Event<TMetadata>.Signature OnMetadataUpdated;
		public event Framework.Event<TMetadata>.Signature OnMetadataDeleted;

		public Resources ( string resourcePath, string resourceExtension, int uidLength ) {

			// init serialized data structures
			_allMetadata = new List<TMetadata>();
			_persistedUIDList = new List<string>();

			// init runtime data structures
			_persistedUIDs = new HashSet<string>();
			_allResourceNames = new HashSet<string>();
			_metadataByUID = new Dictionary<string, TMetadata>();
			_loadedResourcesByUID = new Dictionary<string, TResource>();

			// store ivars
			_path = resourcePath;
			_extension = resourceExtension;
			_uidLength = uidLength;
		}
		public void Close () {

			var loadedResourceUIDs = new List<string>( _loadedResourcesByUID.Keys );
			foreach ( var uid in loadedResourceUIDs ) {
				UnloadResource( uid, save: true );
			}
			SaveMetadataToDisk();
		}

		// data operations
		public void LoadMetadataFromDisk () {

			try {

				PersistentStore.LoadInto_Throws( path: MetadataPath, obj: this );

			} catch ( PersistentStore.InvalidJsonException ) {

				Debug.LogError( $"Client.Model.Resources.LoadMetadataFromDisk: Couldn't LoadMetadataFromDiskload metadata, invalid json in file." );

			} catch ( PersistentStore.FileNotFoundException ) {

				// this is not an error, it just isnhasn't been saved before
			}
		}
		public void SaveMetadataToDisk () {

			try {

				PersistentStore.Save_Throws( path: MetadataPath, data: this );

			} catch ( PersistentStore.InvalidPathException ) {

				Debug.LogError( $"Client.Model.Resources.SaveMetadataToDisk: Couldn't save metadata, invalid save path." );
			}
		}

		// resource creation
		public (TMetadata metadata, TResource resource) New ( string name ) {

			// require name
			if ( string.IsNullOrEmpty( name ) ) {
				throw new ResourceNameEmptyException();
			}

			// avoid name conflicts
			if ( !NameIsUnique( name ) ) {
				throw new ResourceNameConflictException();
			}

			// create data
			var metadata = new TMetadata();
			var resource = new TResource();
			var resourceID = StringHelpers.UID.Generate(
				length: _uidLength,
				validateUniqueness: uid => _metadataByUID.KeyIsUnique( uid )
			);

			// init metadata
			metadata.SetResourceData(
				uid: resourceID,
				name: name,
				path: ResourcePath( name )
			);

			// store
			TrackMetadata( metadata );
			_loadedResourcesByUID[resourceID] = resource;

			return (metadata, resource);
		}
		public bool Rename ( string uid, string name ) {

			if ( uid == null ) { return false; }

			// require name
			if ( string.IsNullOrEmpty( name ) ) {
				throw new ResourceNameEmptyException();
			}

			// avoid name conflicts
			if ( !NameIsUnique( name ) ) {
				throw new ResourceNameConflictException();
			}

			try {

				// rename the file
				var metadata = RequestMetadata( uid );
				var newPath = ResourcePath( name );

				// move file
				if ( ResourceIsOnDisk( uid ) ) {
					try {
						PersistentStore.Move_Throws( metadata.Path, newPath );
					} catch ( PersistentStore.MoveDestinationConflictException ) {
						throw new ResourceFileNameConflictException();
					}
				}


				// create and migrate new metadata entry
				var newMetadata = new TMetadata();
				newMetadata.SetResourceData(
					uid: uid,
					name: name,
					path: ResourcePath( name )
				);
				MigrateTrackedMetadata(
					source: metadata,
					destination: newMetadata
				);

				return true;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Rename: Can't rename, metadata for uid {uid} doesn't exist." );
				return false;
			}
		}
		public bool Delete ( string uid ) {

			if ( uid == null ) { return false; }

			try {

				var metadata = RequestMetadata( uid );

				UntrackMetadata( metadata );
				UnloadResource( uid, save: false );

				if ( ResourceIsOnDisk( uid ) ) {
					try {
						PersistentStore.Delete( metadata.Path );
						_persistedUIDs.Remove( uid );
					} catch ( PersistentStore.FileNotFoundException ) {
						Debug.LogError( $"Client.Model.Resources.Delete: Can't delete resource with uid {uid}, file doesn't exist at path." );
						return false;
					}
				}

				return true;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Delete: Can't delete, metadata for uid {uid} doesn't exist." );
				return false;
			}
		}

		// resource loading
		public bool LoadResource ( string uid ) {

			// early exit
			if ( ResourceIsLoaded( uid ) ) { return true; }

			try {

				var metadata = RequestMetadata( uid );
				var resource = PersistentStore.Load_Throws<TResource>( metadata.Path );
				_loadedResourcesByUID[uid] = resource;
				return true;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Load: Couldn't find resource metadata for uid {uid}" );
				return false;

			} catch ( PersistentStore.FileNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Load: File not found at path in metadata for uid {uid}" );
				return false;

			} catch ( PersistentStore.InvalidJsonException ) {

				Debug.LogError( $"Client.Model.Resources.Load: Invalid json at path in metadata for uid {uid}" );
				return false;
			}
		}
		public void UnloadResource ( string uid, bool save = true ) {

			if ( !ResourceIsLoaded( uid ) ) {
				return;
			}

			if ( save ) {
				SaveResource( uid );
			}

			_loadedResourcesByUID.Remove( uid );
		}
		public bool SaveResource ( string uid ) {

			try {

				var metadata = RequestMetadata( uid );
				var resource = RequestResource( uid, load: false );
				PersistentStore.Save_Throws<TResource>( metadata.Path, resource );
				_persistedUIDs.Add( uid );
				return true;

			} catch ( ResourceNotLoadedException ) {
				Debug.LogError( $"Client.Model.Resources.Save: Couldn't find loaded resource for uid {uid}" );
				return false;

			} catch ( ResourceMetadataNotFoundException ) {
				Debug.LogError( $"Client.Model.Resources.Save: Couldn't find resource metadata for uid {uid}" );
				return false;
			}
		}

		// data requests
		public TMetadata RequestMetadata ( string uid ) {

			if ( _metadataByUID.TryGetValue( uid, out var metadata ) ) {
				return metadata;
			} else {
				throw new ResourceMetadataNotFoundException();
			}
		}
		public TResource RequestResource ( string uid, bool load ) {

			if ( load ) {
				LoadResource( uid );
			}

			// if not loaded, determine issue
			if ( !ResourceIsLoaded( uid ) ) {
				if ( !_metadataByUID.ContainsKey( uid ) ) {
					throw new ResourceMetadataNotFoundException();
				} else {
					throw new ResourceNotLoadedException();
				}
			}

			return _loadedResourcesByUID[uid];
		}

		// helpers
		public bool NameIsUnique ( string name ) => !_allResourceNames.Contains( name );
		public IList<TMetadata> GetAllMetadata () => _allMetadata.AsReadOnly();
		public List<TResource> GetLoadedResources () => new List<TResource>( _loadedResourcesByUID.Values );


		// *********** Private Interface ***********

		// serialized data
		[JsonProperty( propertyName: "metadata", NullValueHandling = NullValueHandling.Ignore )] private List<TMetadata> _allMetadata;
		[JsonProperty( propertyName: "persistedUIDs", NullValueHandling = NullValueHandling.Ignore )] private List<string> _persistedUIDList;

		// runtime data
		private HashSet<string> _allResourceNames;
		private Dictionary<string, TMetadata> _metadataByUID;
		private Dictionary<string, TResource> _loadedResourcesByUID;
		private HashSet<string> _persistedUIDs;


		// instance variables
		private string _path;
		private string _extension;
		private int _uidLength;

		// private helpers
		private string MetadataPath => $"{_path}/{_extension}.metadata";
		private string ResourcePath ( string name ) => $"{_path}/{name}.{_extension}";
		private bool ResourceIsLoaded ( string uid ) => _loadedResourcesByUID.ContainsKey( uid );
		private bool ResourceIsOnDisk ( string uid ) => _persistedUIDs.Contains( uid );

		private void TrackMetadata ( TMetadata metadata ) {

			// track metadata
			_allMetadata.Add( metadata );
			_allResourceNames.Add( metadata.Name );
			_metadataByUID.Add( metadata.UID, metadata );

			// fire event
			Framework.Event<TMetadata>.Fire(
				@event: OnMetadataAdded,
				value: metadata,
				id: $"Resources<{typeof( TResource ).ToString()}>.OnMetadataAdded",
				priority: Framework.EventLogPriorities.Important
			);
			Framework.Event<IList<TMetadata>>.Fire(
				@event: OnAnyMetadataChanged,
				value: _allMetadata.AsReadOnly(),
				id: $"Resources<{typeof( TResource ).ToString()}>.OnAnyMetadataChanged",
				priority: Framework.EventLogPriorities.Important
			);
		}
		private void MigrateTrackedMetadata ( TMetadata source, TMetadata destination ) {

			// untrack old
			_allMetadata.Remove( source );
			_allResourceNames.Remove( source.Name );
			_metadataByUID.Remove( source.UID );

			// track new
			_allMetadata.Add( destination );
			_allResourceNames.Add( destination.Name );
			_metadataByUID.Add( destination.UID, destination );

			// fire event
			Framework.Event<TMetadata>.Fire(
				@event: OnMetadataUpdated,
				value: destination,
				id: $"Resources<{typeof( TResource ).ToString()}>.OnMetadataUpdated",
				priority: Framework.EventLogPriorities.Important
			);
			Framework.Event<IList<TMetadata>>.Fire(
				@event: OnAnyMetadataChanged,
				value: _allMetadata.AsReadOnly(),
				id: $"Resources<{typeof( TResource ).ToString()}>.OnAnyMetadataChanged",
				priority: Framework.EventLogPriorities.Important
			);
		}
		private void UntrackMetadata ( TMetadata metadata ) {

			// untrack metadata
			_allMetadata.Remove( metadata );
			_allResourceNames.Remove( metadata.Name );
			_metadataByUID.Remove( metadata.UID );

			// fire event
			Framework.Event<TMetadata>.Fire(
				@event: OnMetadataDeleted,
				value: metadata,
				id: $"Resources<{typeof( TResource ).ToString()}>.OnMetadataDeleted",
				priority: Framework.EventLogPriorities.Important
			);
			Framework.Event<IList<TMetadata>>.Fire(
				@event: OnAnyMetadataChanged,
				value: _allMetadata.AsReadOnly(),
				id: $"Resources<{typeof( TResource ).ToString()}>.OnAnyMetadataChanged",
				priority: Framework.EventLogPriorities.Important
			);
		}


		[OnSerializing]
		private void OnBeforeSerialize ( StreamingContext context ) {

			// move runtime data into serialized structures
			_persistedUIDList.Clear();
			_persistedUIDList.AddRange( _persistedUIDs );
		}

		[OnDeserialized]
		private void OnAfterDeserialize ( StreamingContext context ) {

			// clear runtime data
			_allResourceNames.Clear();
			_metadataByUID.Clear();
			_persistedUIDs.Clear();

			// copy serialized data into runtime structures
			_allMetadata.ForEach( metadata => {
				_allResourceNames.Add( metadata.Name );
				_metadataByUID.Add( metadata.UID, metadata );
			} );
			_persistedUIDs.UnionWith( _persistedUIDList );
		}
	}

}