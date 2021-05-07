using Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Metadata = Framework.Data.Metadata.Resource;
using WorkspaceModel = Explorer.Model.View.Workspace;

namespace Explorer.View {

	public class WorkspaceBrowser : View {

		// *********** Public Interface ***********

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


		protected override void Init () {

			// init subviews
			InitView( _presenceControl );
			InitView( _newWorkspaceDialog );

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
					var cells = allWorkspaces?.Convert( metadata => new WorkspaceCellData( title: metadata.Name, metadata: metadata ) );
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
				Client.Application.State.Contexts.Current.SetWorkspace( cellData.WorkspaceMetadata.UID );
				_presenceControl.Force( size: PresenceControl.Sizes.Compact );
			} );
			_closeActiveWorkspaceButton.onClick.AddListener( () => {
				Client.Application.State.Contexts.Current.SetWorkspace( null );
				_presenceControl.Force( size: PresenceControl.Sizes.Expanded );
			} );
			_presenceControl.OnSizeChanged.AddListener( presenceSize => {
				var isExpanded = presenceSize == PresenceControl.Sizes.Expanded;
				_workspaceListContainer.gameObject.SetActive( isExpanded );
				_newWorkspaceButton.gameObject.SetActive( isExpanded );
			} );

			// listen to application events
			Client.Application.Resources.Workspaces.OnMetadataChanged += metadata => _allWorkspaces.Set( metadata );

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );
			_newWorkspaceDialog.SetTitle( title: "New Workspace" );
			_newWorkspaceDialog.SetValidators( validators: Client.Application.Resources.Workspaces.ValidateName );
		}
	}

}