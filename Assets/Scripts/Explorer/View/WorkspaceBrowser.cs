using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Metadata = Jakintosh.Resources.Metadata;
using WorkspaceModel = Explorer.View.Model.Workspace;

namespace Explorer.View {

	public class WorkspaceBrowser : View {

		// *********** Public Interface ***********

		public void SetCurrentContextUID ( string contextUID ) => _contextUID = contextUID;
		public void SetActiveWorkspace ( WorkspaceModel workspace ) => _activeWorkspace.Set( workspace );


		// *********** Private Interface ***********

		[Header( "UI Subviews" )]
		[SerializeField] private PresenceControl _presenceControl;
		[SerializeField] private ValidatedTextEntryDialog _newWorkspaceDialog;

		[Header( "UI Control" )]
		[SerializeField] private Button _newWorkspaceButton;
		[SerializeField] private Button _closeActiveWorkspaceButton;
		[SerializeField] private WorkspaceList _workspaceListLayout;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _activeWorkspaceNameText;
		[SerializeField] private GameObject _activeWorkspaceContainer;
		[SerializeField] private GameObject _workspaceListContainer;
		[SerializeField] private GameObject _fade;


		// model data
		private Observable<bool> _dialogOpen;
		private Observable<WorkspaceModel> _activeWorkspace;
		private ListObservable<Metadata> _allWorkspaces;

		private string _contextUID;

		protected override void OnInitialize () {

			// init subviews
			_presenceControl.Init();
			_newWorkspaceDialog.Init();

			// init observables
			_dialogOpen = new Observable<bool>(
				initialValue: false,
				onChange: open => {
					_fade.SetActive( open );
					_newWorkspaceDialog.gameObject.SetActive( open );
					_newWorkspaceButton.interactable = !open;
					_presenceControl.SetInteractive( close: false, size: !open, context: false );
				}
			);
			_activeWorkspace = new Observable<WorkspaceModel>(
				initialValue: null,
				onChange: activeWorkspace => {
					_activeWorkspaceNameText.text = activeWorkspace?.Name ?? "---";
					_closeActiveWorkspaceButton.gameObject.SetActive( activeWorkspace != null );
				}
			);
			_allWorkspaces = new ListObservable<Metadata>(
				initialValue: Client.Application.Resources.Workspaces.GetAll(),
				onChange: allWorkspaces => {
					var cells = allWorkspaces?.Convert( metadata => new WorkspaceCellData( title: metadata.Name, active: false, metadata: metadata ) );
					_workspaceListLayout.SetData( cells );
				}
			);

			// connect controls
			_newWorkspaceButton.onClick.AddListener( () => {
				_dialogOpen.Set( true );
			} );
			_newWorkspaceDialog.OnClose.AddListener( () => {
				_dialogOpen.Set( false );
			} );
			_newWorkspaceDialog.OnConfirm.AddListener( name => {
				Client.Application.Resources.Workspaces.New( name: name );
			} );
			_workspaceListLayout.OnCellClicked.AddListener( cellData => {
				Client.Application.State.Contexts.GetContext( _contextUID )?.SetWorkspace( cellData.WorkspaceMetadata.UID );
				_presenceControl.Force( size: PresenceControl.Sizes.Compact );
			} );
			_closeActiveWorkspaceButton.onClick.AddListener( () => {
				Client.Application.State.Contexts.GetContext( _contextUID )?.SetWorkspace( null );
				_presenceControl.Force( size: PresenceControl.Sizes.Expanded );
			} );
			_presenceControl.OnSizeChanged.AddListener( presenceSize => {
				var isExpanded = presenceSize == PresenceControl.Sizes.Expanded;
				_workspaceListContainer.gameObject.SetActive( isExpanded );
				_newWorkspaceButton.gameObject.SetActive( isExpanded );
			} );

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );
			_newWorkspaceDialog.SetTitle( title: "New Workspace" );
			_newWorkspaceDialog.SetValidators( validators: Client.Application.Resources.Workspaces.ValidateName );

			// sub to app events
			Client.Application.Resources.Workspaces.OnAnyMetadataChanged += HandleNewWorkspaceMetadata;
		}
		protected override void OnCleanup () {

			Client.Application.Resources.Workspaces.OnAnyMetadataChanged -= HandleNewWorkspaceMetadata;
		}

		private void HandleNewWorkspaceMetadata ( IList<Metadata> metadata ) => _allWorkspaces.Set( metadata );
	}

}