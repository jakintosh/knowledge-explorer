using Jakintosh.Knowledge;
using Jakintosh.Resources;
using System.Collections.Generic;

using Workspace = Explorer.View.Model.Workspace;

namespace Explorer.Model {

	public interface IWorkspaceCRUD {

		event Framework.Event<IList<Metadata>>.Signature OnAnyMetadataChanged;
		event Framework.Event<Metadata>.Signature OnMetadataAdded;
		event Framework.Event<Metadata>.Signature OnMetadataUpdated;
		event Framework.Event<Metadata>.Signature OnMetadataDeleted;

		Metadata New ( string name, string graphID = null );
		bool Rename ( string uid, string name );
		bool Delete ( string uid );

		Workspace Get ( string uid );
		IList<Metadata> GetAll ();

		bool ValidateName ( string name );
	}
	public class WorkspaceResources : IWorkspaceCRUD {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnAnyMetadataChanged;
		public event Framework.Event<Metadata>.Signature OnMetadataAdded;
		public event Framework.Event<Metadata>.Signature OnMetadataUpdated;
		public event Framework.Event<Metadata>.Signature OnMetadataDeleted;

		public WorkspaceResources ( string rootDataPath, GraphResources graphs ) {

			_workspaces = new Resources<Metadata, Workspace>(
				resourcePath: $"{rootDataPath}/workspace",
				resourceExtension: "workspace",
				uidLength: 6
			);
			_graphs = graphs;

			// update name when metadata updates
			_workspaces.OnMetadataUpdated += metadata => {
				var workspace = Get( metadata.UID );
				workspace.Name = metadata.Name;
			};

			// pass through event
			_workspaces.OnAnyMetadataChanged += metadata => OnAnyMetadataChanged?.Invoke( metadata );
			_workspaces.OnMetadataAdded += metadata => OnMetadataAdded?.Invoke( metadata );
			_workspaces.OnMetadataUpdated += metadata => OnMetadataUpdated?.Invoke( metadata );
			_workspaces.OnMetadataDeleted += metadata => OnMetadataDeleted?.Invoke( metadata );
		}

		public void LoadMetadata () {

			_workspaces.LoadMetadataFromDisk();
		}
		public void Close () {

			_workspaces.Close();
		}

		public Metadata New ( string name, string graphUid ) {

			// try get graph
			Graph graphResource;
			try {

				if ( graphUid == null ) { graphUid = _graphs.New( name )?.UID; }
				graphResource = _graphs.Get( graphUid );

			} catch ( ResourceMetadataNotFoundException ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Create: Failed due to missing graph." );
				return null;
			}
			if ( graphUid == null ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Create: Failed due to graph retrieval error." );
				return null;
			}

			// try create workspace
			Metadata workspaceMetadata;
			Workspace workspaceResource;
			try {

				(workspaceMetadata, workspaceResource) = _workspaces.New( name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Create: Failed due to empty resource name." );
				return null;
			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Create: Failed due to workspace name conflict." );
				return null;
			}

			// init workspace
			workspaceResource.Initialize(
				uid: workspaceMetadata.UID,
				name: workspaceMetadata.Name,
				graph: graphResource
			);

			// return metadata
			return workspaceMetadata;
		}
		public bool Rename ( string uid, string name ) {

			try {

				// rename graph
				if ( !_graphs.Rename( Get( uid )?.GraphUID, name ) ) {
					return false;
				}

				// rename workspace
				return _workspaces.Rename( uid, name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Rename: Failed due to Resource Name Empty" );
			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Rename: Failed due to Resource Name Conflict" );
			} catch ( ResourceFileNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Rename: Failed due to Resource File Name Conflict, but not Metadata name conflict. Data may be corrupted." );
			}
			return false;
		}
		public bool Delete ( string uid ) {


			if ( !_graphs.Delete( Get( uid )?.GraphUID ) ) {
				return false;
			}

			return _workspaces.Delete( uid );
		}

		public Workspace Get ( string uid ) {

			if ( uid == null ) {
				return null;
			}

			try {

				var workspaceResource = _workspaces.RequestResource( uid, load: true );

				// // try get graph
				// Knowledge.Graph graphResource;
				// try {

				// 	// TODO: this should be using some kind of resource dependency system
				// 	graphResource = _graphs.Get( workspaceResource.GraphUID );

				// } catch ( System.Exception ex ) {
				// 	Debug.LogError( $"Model.Application.WorkspaceResources.Read: Failed due to {ex.Message}. Deleting workspace and aborting." );
				// 	Delete( uid );
				// 	_graphs.Delete( workspaceResource.GraphUID );
				// 	return null;
				// }

				return workspaceResource;

			} catch ( ResourceMetadataNotFoundException ) {

				UnityEngine.Debug.LogError( "Model.Application.WorkspaceResources.Read: Failed due to missing workspace." );
				return null;
			}
		}
		public IList<Metadata> GetAll () {

			return _workspaces.GetAllMetadata();
		}

		public bool ValidateName ( string name ) {

			if ( string.IsNullOrEmpty( name ) ) return false;
			return _workspaces.NameIsUnique( name );
		}


		// ********** Private Interface **********

		private GraphResources _graphs;
		private Resources<Metadata, Workspace> _workspaces;
	}
}