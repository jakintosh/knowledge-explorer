using Jakintosh.Knowledge;
using Jakintosh.Resources;
using System.Collections.Generic;

using Workspace = Explorer.View.Model.Workspace;

namespace Explorer.Model {


	public interface IWorkspaceCRUD {

		event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		Metadata New ( string name, string graphID = null );
		bool Delete ( string uid );

		Workspace Get ( string uid );
		IList<Metadata> GetAll ();

		bool ValidateName ( string name );
	}
	public class WorkspaceResources : IWorkspaceCRUD {

		// ********** Public Interface **********

		public event Framework.Event<IList<Metadata>>.Signature OnMetadataChanged;

		public WorkspaceResources ( string rootDataPath, GraphResources graphs ) {

			_workspaces = new Resources<Metadata, Workspace>(
				resourcePath: $"{rootDataPath}/workspace",
				resourceExtension: "workspace",
				uidLength: 6
			);

			// pass through event
			_workspaces.OnMetadataChanged += metadata => OnMetadataChanged?.Invoke( metadata );
			_graphs = graphs;
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
				UnityEngine.Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to missing graph." );
				return null;
			}

			// try create workspace
			Metadata workspaceMetadata;
			Workspace workspaceResource;
			try {

				(workspaceMetadata, workspaceResource) = _workspaces.New( name );

			} catch ( ResourceNameEmptyException ) {
				UnityEngine.Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to empty resource name." );
				return null;
			} catch ( ResourceNameConflictException ) {
				UnityEngine.Debug.LogError( "Model.Application.IWorkspaceAPI.Create: Failed due to name conflict." );
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
		public bool Delete ( string uid ) {

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
				// 	Debug.LogError( $"Model.Application.IWorkspaceAPI.Read: Failed due to {ex.Message}. Deleting workspace and aborting." );
				// 	Delete( uid );
				// 	_graphs.Delete( workspaceResource.GraphUID );
				// 	return null;
				// }

				return workspaceResource;

			} catch ( ResourceMetadataNotFoundException ) {

				UnityEngine.Debug.LogError( "Model.Application.IWorkspaceAPI.Read: Failed due to missing workspace." );
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