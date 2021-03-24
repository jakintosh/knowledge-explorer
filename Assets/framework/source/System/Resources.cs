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
				[JsonProperty] public string ID;
			}

			[JsonIgnore] public string ID => id;
			[JsonIgnore] public string Name => name;
			[JsonIgnore] public string Path => path;
			[JsonIgnore] public Dependency[] Dependencies => dependencies;

			public void SetResourceData ( string id, string name, string path ) {

				this.id = id;
				this.name = name;
				this.path = path;
			}

			[JsonProperty] private string id;
			[JsonProperty] private string name;
			[JsonProperty] private string path;
			[JsonProperty] private Dependency[] dependencies;


			// ********** IEquatable Implementation **********

			bool IEquatable<Resource>.Equals ( Resource other ) => other.ID.Equals( this.ID );
		}
	}

	// TODO: Resources need dependencies
	// public class Resource<T> {

	// 	public void Load ( Metadata.Resource metadata ) {

	// 	}
	// }

	[Serializable]
	public class Resources<TMetadata, TResource>
		where TMetadata : Metadata.Resource, new()
		where TResource : class, new() {


		// *********** Public Interface ***********

		public event Framework.Event<IList<TMetadata>>.Signature OnMetadataChanged;

		public Resources ( string resourcePath, string resourceExtension, int idLength ) {

			// init data structures
			_allMetadata = new List<TMetadata>();
			_allResourceNames = new HashSet<string>();
			_metadataById = new Dictionary<string, TMetadata>();
			_loadedResourcesById = new Dictionary<string, TResource>();

			// store ivars
			_path = resourcePath;
			_extension = resourceExtension;
			_idLength = idLength;
		}
		public void Close () {

			SaveMetadataToDisk();
			var loadedResourceIDs = new List<string>( _loadedResourcesById.Keys );
			foreach ( var id in loadedResourceIDs ) {
				UnloadResource( id, save: true );
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
				length: _idLength,
				validateUniqueness: id => _metadataById.KeyIsUnique( id )
			);

			// init metadata
			metadata.SetResourceData(
				id: resourceID,
				name: name,
				path: ResourcePath( name )
			);

			// store
			TrackMetadata( metadata );
			_loadedResourcesById[resourceID] = resource;

			return (metadata, resource);
		}
		public bool Delete ( string id ) {

			try {

				var metadata = RequestMetadata( id );

				UntrackMetadata( metadata );
				UnloadResource( id, save: false );

				PersistentStore.Delete( metadata.Path );

				return true;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Delete: Can't delete, metadata for id {id} doesn't exist." );
				return false;
			}
		}

		// resource loading
		public bool LoadResource ( string id ) {

			// early exit
			if ( ResourceIsLoaded( id ) ) { return true; }

			try {

				var metadata = RequestMetadata( id );
				var resource = PersistentStore.Load<TResource>( metadata.Path );
				_loadedResourcesById[id] = resource;
				return true;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( $"Client.Model.Resources.Load: Couldn't find resource metadata for id {id}" );
				return false;
			}
			// TODO: handle deserialization exceptions
		}
		public void UnloadResource ( string id, bool save = true ) {

			if ( !ResourceIsLoaded( id ) ) {
				return;
			}

			if ( save ) {
				SaveResource( id );
			}

			_loadedResourcesById.Remove( id );
		}
		public bool SaveResource ( string id ) {

			try {

				var metadata = RequestMetadata( id );
				var resource = RequestResource( id, load: false );
				PersistentStore.Save<TResource>( metadata.Path, resource );
				return true;

			} catch ( ResourceNotLoadedException ) {
				Debug.LogError( $"Client.Model.Resources.Save: Couldn't find loaded resource for id {id}" );
				return false;

			} catch ( ResourceMetadataNotFoundException ) {
				Debug.LogError( $"Client.Model.Resources.Save: Couldn't find resource metadata for id {id}" );
				return false;
			}
		}

		// data requests
		public TMetadata RequestMetadata ( string id ) {

			if ( _metadataById.TryGetValue( id, out var metadata ) ) {
				return metadata;
			} else {
				throw new ResourceMetadataNotFoundException();
			}
		}
		public TResource RequestResource ( string id, bool load ) {

			if ( load ) {
				LoadResource( id );
			}

			// if not loaded, determine issue
			if ( !ResourceIsLoaded( id ) ) {
				if ( !_metadataById.ContainsKey( id ) ) {
					throw new ResourceMetadataNotFoundException();
				} else {
					throw new ResourceNotLoadedException();
				}
			}

			return _loadedResourcesById[id];
		}

		// helpers
		public bool NameIsUnique ( string name ) => !_allResourceNames.Contains( name );
		public IList<TMetadata> GetAllMetadata () => _allMetadata.AsReadOnly();
		public List<TResource> GetLoadedResources () => new List<TResource>( _loadedResourcesById.Values );


		// *********** Private Interface ***********

		// serialized data
		[JsonProperty( propertyName: "metadata", NullValueHandling = NullValueHandling.Ignore )] private List<TMetadata> _allMetadata;

		// runtime data
		private HashSet<string> _allResourceNames;
		private Dictionary<string, TMetadata> _metadataById;
		private Dictionary<string, TResource> _loadedResourcesById;

		// instance variables
		private string _path;
		private string _extension;
		private int _idLength;

		// private helpers
		private string MetadataPath => $"{_path}/.metadata";
		private string ResourcePath ( string name ) => $"{_path}/{name}.{_extension}";
		private bool ResourceIsLoaded ( string id ) => _loadedResourcesById.ContainsKey( id );

		private void TrackMetadata ( TMetadata metadata ) {

			// track metadata
			_allMetadata.Add( metadata );
			_allResourceNames.Add( metadata.Name );
			_metadataById.Add( metadata.ID, metadata );

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
			_metadataById.Remove( metadata.ID );

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
			_metadataById?.Clear();

			// copy serialized data into runtime structures
			_allMetadata.ForEach( metadata => {
				_allResourceNames.Add( metadata.Name );
				_metadataById.Add( metadata.ID, metadata );
			} );
		}
	}

}