using Framework;
using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Workspaces {

		// ********** OUTPUTS **********

		[NonSerialized] public Output<Workspace> ActiveWorkspace = new Output<Workspace>();

		// ********** INPUTS ***********

		public void OpenWorkspace ( string id ) {

			if ( id == null ) {
				CloseWorkspace();
				return;
			}

			var workspace = _workspaceModel.Read( id );
			ActiveWorkspace.Set( workspace );
			activeWorkspaceID = id;
		}
		public void CloseWorkspace () {

			ActiveWorkspace.Set( null );
			activeWorkspaceID = null;
		}

		// *****************************

		// subviews
		[SerializeField] public WorkspaceBrowser WorkspaceBrowser = null;

		// runtime data
		private Model.IWorkspaceAPI _workspaceModel;
		public Workspaces ( Model.IWorkspaceAPI workspaceModel ) {

			_workspaceModel = workspaceModel;

			WorkspaceBrowser = new WorkspaceBrowser(
				activeWorkspaceOutput: ActiveWorkspace,
				allWorkspaceMetadataOutput: _workspaceModel.AllMetadata,
				createWorkspaceAction: name => _workspaceModel.Create( name ),
				openWorkspaceAction: OpenWorkspace,
				closeWorkspaceAction: CloseWorkspace,
				workspaceNameValidator: name => _workspaceModel.ValidateName( name )
			);
		}

		// serialized data
		[SerializeField] private string activeWorkspaceID;
	}
}