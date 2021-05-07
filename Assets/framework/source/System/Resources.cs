using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Framework.Data {

	// ******** Exception Definitions ********

	public class ResourceNameEmptyException : Exception { }
	public class ResourceNameConflictException : Exception { }
	public class ResourceNotLoadedException : Exception { }
	public class ResourceMetadataNotFoundException : Exception { }


	// ******** Metadata Definitions ********

	namespace Metadata {

		[Serializable]
		public class Resource : IEquatable<Resource> {

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

			bool IEquatable<Resource>.Equals ( Resource other ) => other?.UID.Equals( this.UID ) ?? false;
		}
	}

	// TODO: Resources need dependencies

	[Serializable]
	public class Resources<TMetadata, TResource>
		where TMetadata : Metadata.Resource, new()
		where TResource : class, new() {


		// *********** Public Interface ***********

		public event Framework.Event<IList<TMetadata>>.Signature OnMetadataChanged;

		public Resources ( string resourcePath, string resourceExtension, int uidLength ) {

			// init data structures
			_allMetadata = new List<TMetadata>();
			_allResourceNames = new HashSet<string>();
			_metadataByUID = new Dictionary<string, TMetadata>();
			_loadedResourcesByUID = new Dictionary<string, TResource>();

			// store ivars
			_path = resourcePath;
			_extension = resourceExtension;
			_uidLength = uidLength;
		}
		public void Close () {

			SaveMetadataToDisk();
			var loadedResourceUIDs = new List<string>( _loadedResourcesByUID.Keys );
			foreach ( var uid in loadedResourceUIDs ) {
				UnloadResource( uid, save: true );
			}
		}

		// data operations
		public void LoadMetadataFromDisk ()
			=> PersistentStore.LoadInto( path: MetadataPath, obj: this );
		public void SaveMetadataToDisk ()
			=> PersistentStore.Save( path: MetadataPath, data: this );

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
		public bool Delete ( string uid ) {

			if ( uid == null ) { return false; }

			try {

				var metadata = RequestMetadata( uid );

				UntrackMetadata( metadata );
				UnloadResource( uid, save: false );

				PersistentStore.Delete( metadata.Path );

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
				var resource = PersistentStore.Load<TResource>( metadata.Path );
				_loadedResourcesByUID[uid] = resource;
				return true;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Load: Couldn't find resource metadata for uid {uid}" );
				return false;
			}
			// TODO: handle deserialization exceptions
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
				PersistentStore.Save<TResource>( metadata.Path, resource );
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

		// runtime data
		private HashSet<string> _allResourceNames;
		private Dictionary<string, TMetadata> _metadataByUID;
		private Dictionary<string, TResource> _loadedResourcesByUID;

		// instance variables
		private string _path;
		private string _extension;
		private int _uidLength;

		// private helpers
		private string MetadataPath => $"{_path}/{_extension}.metadata";
		private string ResourcePath ( string name ) => $"{_path}/{name}.{_extension}";
		private bool ResourceIsLoaded ( string uid ) => _loadedResourcesByUID.ContainsKey( uid );

		private void TrackMetadata ( TMetadata metadata ) {

			// track metadata
			_allMetadata.Add( metadata );
			_allResourceNames.Add( metadata.Name );
			_metadataByUID.Add( metadata.UID, metadata );

			// fire event
			Framework.Event<IList<TMetadata>>.Fire(
				@event: OnMetadataChanged,
				value: _allMetadata.AsReadOnly(),
				id: $"Resources<{typeof( TResource ).ToString()}>.OnMetadataChanged",
				priority: Framework.EventLogPriorities.Important
			);
		}
		private void UntrackMetadata ( TMetadata metadata ) {

			// untrack metadata
			_allMetadata.Remove( metadata );
			_allResourceNames.Remove( metadata.Name );
			_metadataByUID.Remove( metadata.UID );

			// fire event
			Framework.Event<IList<TMetadata>>.Fire(
				@event: OnMetadataChanged,
				value: _allMetadata.AsReadOnly(),
				id: "Resources.OnMetadataChanged",
				priority: Framework.EventLogPriorities.Important
			);
		}


		[OnDeserialized]
		private void OnAfterDeserialize ( StreamingContext context ) {

			// clear runtime data
			_allResourceNames?.Clear();
			_metadataByUID?.Clear();

			// copy serialized data into runtime structures
			_allMetadata.ForEach( metadata => {
				_allResourceNames.Add( metadata.Name );
				_metadataByUID.Add( metadata.UID, metadata );
			} );
		}
	}

}