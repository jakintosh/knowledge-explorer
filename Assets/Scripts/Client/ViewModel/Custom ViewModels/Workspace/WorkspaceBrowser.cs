using Framework;
using System;
using UnityEngine;

using ResourceMetadata = Framework.Data.Metadata.Resource;

namespace Client.ViewModel {

	[Serializable]
	public class WorkspaceBrowser {

		// ********** OUTPUTS **********

		public Output<Workspace> ActiveWorkspace => _activeWorkspaceOutput;
		public ListOutput<ResourceMetadata> AllWorkspaces => _allWorkspaceMetadataOutput;

		// ********** INPUTS ***********

		public void OpenNewWorkspaceDialog () {
			NewWorkspaceDialog.Open();
		}
		public void ConfirmNewWorkspace ( string name ) {
			_createWorkspaceAction( name );
			NewWorkspaceDialog.ValidatedText.Set( "" );
			NewWorkspaceDialog.Close();
		}
		public void OpenWorkspace ( string id ) {
			_openWorkspaceAction( id );
			Presence.Size.Set( Presence.Sizes.Compact );
		}
		public void CloseActiveWorkspace () {
			_closeWorkspaceAction();
		}

		// *****************************


		// components
		[SerializeField] public Presence Presence = new Presence();

		// subviews
		[SerializeField] public ValidatedTextEntryDialog NewWorkspaceDialog = null;


		// runtime model
		private Output<Workspace> _activeWorkspaceOutput;
		private ListOutput<ResourceMetadata> _allWorkspaceMetadataOutput;
		private Action<string> _createWorkspaceAction;
		private Action<string> _openWorkspaceAction;
		private Action _closeWorkspaceAction;
		private Func<string, bool> _workspaceNameValidator;

		public WorkspaceBrowser (
			Output<Workspace> activeWorkspaceOutput,
			ListOutput<ResourceMetadata> allWorkspaceMetadataOutput,
			Action<string> createWorkspaceAction,
			Action<string> openWorkspaceAction,
			Action closeWorkspaceAction,
			Func<string, bool> workspaceNameValidator ) {

			// proxies assigned at runtime
			_activeWorkspaceOutput = activeWorkspaceOutput;
			_allWorkspaceMetadataOutput = allWorkspaceMetadataOutput;
			_createWorkspaceAction = createWorkspaceAction;
			_openWorkspaceAction = openWorkspaceAction;
			_closeWorkspaceAction = closeWorkspaceAction;
			_workspaceNameValidator = workspaceNameValidator;

			// init subviews
			NewWorkspaceDialog = new ValidatedTextEntryDialog(
				title: "New Workspace",
				stringValidator: _workspaceNameValidator
			);
		}

	}

}