using Framework;
using Framework.Data;
using System;
using UnityEngine;

using Graph = Server.KnowledgeGraph;
using Workspace = Client.ViewModel.Workspace;
using Metadata = Framework.Data.Metadata;

namespace Client.Model {


	public interface IWorkspaceAPI {

		// outputs
		ListOutput<Metadata.Resource> AllMetadata { get; }

		// methods
		Metadata.Resource Create ( string name, string bucketUID = null );
		Workspace Read ( string uid );
		bool Delete ( string uid );

		// helpers
		bool ValidateName ( string name, bool allowEmpty = false );
	}
	public interface IGraphAPI {

		// outputs
		ListOutput<Metadata.Resource> AllMetadata { get; }

		// methods
		Metadata.Resource Create ( string name );
		Graph Read ( string uid );
		bool Delete ( string uid );

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
				uidLength: WORKSPACE_UID_LENGTH
			);
			_graphs = new Resources<Metadata.Resource, Graph>(
				resourcePath: GraphPath,
				resourceExtension: GRAPH_EXTENSION,
				uidLength: GRAPH_UID_LENGTH
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
				graph.FirstInitialization(); // TODO: can we make this also some resource management thing?
				return metadata;

			} catch ( ResourceNameEmptyException ) {

				Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Empty" );
				return null;

			} catch ( ResourceNameConflictException ) {

				Debug.LogError( "Model.Application.IGraphAPI.Create: Failed due to Resource Name Conflict" );
				return null;
			}
		}
		Graph IGraphAPI.Read ( string uid ) {

			try {

				return _graphs.RequestResource( uid, load: true );

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( "Model.Application.IGraphAPI.Read: Failed due to missing graph." );
				return null;
			}
		}
		bool IGraphAPI.Delete ( string uid ) {

			return _graphs.Delete( uid );
		}


		// data resources
		private static int GRAPH_UID_LENGTH = 6;
		private static string GRAPH_EXTENSION = "graph";
		private string GraphPath => $"{LocalDataPath}/graph";

		private Resources<Metadata.Resource, Graph> _graphs;
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
			workspaceResource.UID.Set( workspaceMetadata.UID );
			workspaceResource.Name.Set( workspaceMetadata.Name );

			// if graph doesn't exist, try to create it
			if ( graphID == null ) {
				var graphMetadata = ( this as IGraphAPI ).Create( name );
				graphID = graphMetadata?.UID;
			}

			// try get graph
			Graph graphResource;
			try {

				graphResource = _graphs.RequestResource( graphID, load: true );

			} catch ( ResourceMetadataNotFoundException ) {
				Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to missing graph. Deleting created workspace and aborting." );
				( this as IWorkspaceAPI ).Delete( workspaceMetadata.UID );
				return null;
			}

			// init workspace
			workspaceResource.SetGraph( graphResource );
			workspaceResource.GraphUID.Set( graphID );

			// return metadata
			return workspaceMetadata;
		}
		Workspace IWorkspaceAPI.Read ( string uid ) {

			try {

				var workspaceResource = _workspaces.RequestResource( uid, load: true );

				// try get graph
				Graph graphResource;
				try {

					// TODO: this should be using some kind of resource dependency system
					graphResource = _graphs.RequestResource( workspaceResource.GraphUID.Get(), load: true );

				} catch {
					Debug.LogError( "Model.Application.IWorkspaceAPI.Read: Failed due to missing graph. Deleting workspace and aborting." );
					( this as IWorkspaceAPI ).Delete( uid );
					( this as IGraphAPI ).Delete( workspaceResource.GraphUID.Get() );
					return null;
				}

				workspaceResource.SetGraph( graphResource );

				return workspaceResource;

			} catch ( ResourceMetadataNotFoundException ) {

				Debug.LogError( "Model.Application.IWorkspaceAPI.Read: Failed due to missing workspace." );
				return null;
			}
		}
		bool IWorkspaceAPI.Delete ( string uid ) {

			return _workspaces.Delete( uid );
		}


		// data resources
		private static int WORKSPACE_UID_LENGTH = 6;
		private static string WORKSPACE_EXTENSION = "workspace";
		private string WorkspacePath => $"{LocalDataPath}/workspace";

		private Resources<Metadata.Resource, Workspace> _workspaces;
		private ListOutput<Metadata.Resource> _workspaceMetadataOutput = new ListOutput<Metadata.Resource>();
	}

}