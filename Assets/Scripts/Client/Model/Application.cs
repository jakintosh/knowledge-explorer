using Framework;
using Framework.Data;
using System;
using UnityEngine;

using Bucket = Library.Model.Bucket;
using Workspace = Client.ViewModel.Workspace;
using Metadata = Framework.Data.Metadata;

namespace Client.Model {


	public interface IWorkspaceAPI {

		// outputs
		ListOutput<Metadata.Resource> AllMetadata { get; }

		// methods
		Metadata.Resource Create ( string name, string bucketID = null );
		Workspace Read ( string id );
		bool Delete ( string id );

		// helpers
		bool ValidateName ( string name, bool allowEmpty = false );
	}

	// public interface IBucketAPI {

	// 	// outputs
	// 	ListOutput<Metadata.Resource> AllMetadata { get; }

	// 	// methods
	// 	Metadata.Resource Create ( string name );
	// 	Bucket Read ( string id );
	// 	bool Delete ( string id );

	// 	// helpers
	// 	bool ValidateName ( string name, bool allowEmpty = false );
	// }

	public interface IGraphAPI {

		// outputs
		ListOutput<Metadata.Resource> AllMetadata { get; }

		// methods
		Metadata.Resource Create ( string name );
		Server.Graph Read ( string id );
		bool Delete ( string id );

		// helpers
		bool ValidateName ( string name, bool allowEmpty = false );
	}


	[Serializable]
	public partial class Application {

		// *********** Public Interface ***********


		public Application () {

			// initialize resources
			_workspaces = new Resources<Metadata.Resource, Workspace>(
				resourcePath: WorkspacePath,
				resourceExtension: WORKSPACE_EXTENSION,
				idLength: WORKSPACE_ID_LENGTH
			);
			_graphs = new Resources<Metadata.Resource, Server.Graph>(
				resourcePath: GraphPath,
				resourceExtension: GRAPH_EXTENSION,
				idLength: GRAPH_ID_LENGTH
			);

			// subscribe to events on resources
			_workspaces.OnMetadataChanged += metadata => {
				_workspaceMetadataOutput.Set( metadata );
			};
			_graphs.OnMetadataChanged += metadata => {
				_graphMetadataOutput.Set( metadata );
			};
		}


		public void Load () {

			// load from disk
			_workspaces.LoadMetadataFromDisk();
			_graphs.LoadMetadataFromDisk();

			// get initial data
			_workspaceMetadataOutput.Set( _workspaces.GetAllMetadata() );
			_graphMetadataOutput.Set( _graphs.GetAllMetadata() );
		}
		public void Unload () {

			_workspaces.Close();
			_graphs.Close();
		}

		// *********** Private Interface ***********

		private string LocalDataPath => $"/data/local";
	}

	public partial class Application : IGraphAPI {

		// outputs
		ListOutput<Metadata.Resource> IGraphAPI.AllMetadata => _graphMetadataOutput;

		// methods
		bool IGraphAPI.ValidateName ( string name, bool allowEmpty ) {

			if ( !allowEmpty && string.IsNullOrEmpty( name ) ) return false;
			return _graphs.NameIsUnique( name );
		}
		Metadata.Resource IGraphAPI.Create ( string name ) {

			try {

				var (metadata, graph) = _graphs.New( name );
				graph.UID = metadata.ID;
				return metadata;

			} catch ( ResourceNameEmptyException ) {

				Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Empty" );
				return null;

			} catch ( ResourceNameConflictException ) {

				Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Conflict" );
				return null;
			}
		}
		Server.Graph IGraphAPI.Read ( string id ) {

			try {

				return _graphs.RequestResource( id, load: true );

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( "Model.Application.IGraphAPI.Read: Failed due to missing graph." );
				return null;
			}
		}
		bool IGraphAPI.Delete ( string id ) {

			return _graphs.Delete( id );
		}


		// data resources
		private static int GRAPH_ID_LENGTH = 6;
		private static string GRAPH_EXTENSION = "graph";
		private string GraphPath => $"{LocalDataPath}/graph";

		private Resources<Metadata.Resource, Server.Graph> _graphs;
		private ListOutput<Metadata.Resource> _graphMetadataOutput = new ListOutput<Metadata.Resource>();
	}
	public partial class Application : IWorkspaceAPI {

		// outputs
		ListOutput<Metadata.Resource> IWorkspaceAPI.AllMetadata => _workspaceMetadataOutput;

		// methods
		bool IWorkspaceAPI.ValidateName ( string name, bool allowEmpty ) {

			if ( !allowEmpty && string.IsNullOrEmpty( name ) ) return false;
			return _workspaces.NameIsUnique( name );
		}
		Metadata.Resource IWorkspaceAPI.Create ( string name, string graphID ) {

			// try create workspace
			Metadata.Resource workspaceMetadata;
			Workspace workspaceResource;
			try {

				(workspaceMetadata, workspaceResource) = _workspaces.New( name );

			} catch ( ResourceNameEmptyException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to empty resource name" );
				return null;
			} catch ( ResourceNameConflictException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to name conflict" );
				return null;
			}

			// set metadata
			workspaceResource.SetMetadata( workspaceMetadata );

			// if graph doesn't exist, try to create it
			if ( graphID == null ) {
				var graphMetadata = ( this as IGraphAPI ).Create( name );
				graphID = graphMetadata?.ID;
			}

			// try get graph
			Server.Graph graphResource;
			try {

				graphResource = _graphs.RequestResource( graphID, load: true );

			} catch ( ResourceMetadataNotFoundException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to missing graph. Deleting created workspace and aborting." );
				( this as IWorkspaceAPI ).Delete( workspaceMetadata.ID );
				return null;
			}

			// init workspace
			workspaceResource.SetGraph( graphResource );

			// return metadata
			return workspaceMetadata;
		}
		Workspace IWorkspaceAPI.Read ( string id ) {

			try {

				var workspace = _workspaces.RequestResource( id, load: true );

				// try get graph
				Server.Graph graphResource;
				try {

					graphResource = _graphs.RequestResource( workspace.GraphUID, load: true );

				} catch {
					Debug.LogError( "Model.Application.IWorkspaceAPI.Read: Failed due to missing graph. Deleting workspace and aborting." );
					( this as IWorkspaceAPI ).Delete( id );
					return null;
				}

				workspace.SetGraph( graphResource );

				return workspace;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( "Model.Application.IWorkspaceAPI.Read: Failed due to missing workspace." );
				return null;
			}
		}
		bool IWorkspaceAPI.Delete ( string id ) {

			return _workspaces.Delete( id );
		}


		// data resources
		private static int WORKSPACE_ID_LENGTH = 6;
		private static string WORKSPACE_EXTENSION = "workspace";
		private string WorkspacePath => $"{LocalDataPath}/workspace";

		private Resources<Metadata.Resource, Workspace> _workspaces;
		private ListOutput<Metadata.Resource> _workspaceMetadataOutput = new ListOutput<Metadata.Resource>();
	}

}