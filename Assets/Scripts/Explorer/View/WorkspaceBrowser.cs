using Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Metadata = Framework.Data.Metadata.Resource;

namespace Explorer.View {

	public class WorkspaceBrowser : View {

		// *********** Public Interface ***********

		public void SetActiveWorkspace ( Model.Workspace workspace ) => _activeWorkspace.Set( workspace );


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
		private Observable<Model.Workspace> _activeWorkspace;
		private ListObservable<Metadata> _allWorkspaces;


		protected override void Init () {

			// init subviews
			Init( _presenceControl );
			Init( _newWorkspaceDialog );

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
			_activeWorkspace = new Observable<Model.Workspace>(
				initialValue: null,
				onChange: activeWorkspace => {
					_activeWorkspaceNameText.text = activeWorkspace?.Name ?? "---";
					_closeActiveWorkspaceButton.gameObject.SetActive( activeWorkspace != null );
				}
			);
			_allWorkspaces = new ListObservable<Metadata>(
				initialValue: Application.Resources.Workspaces.GetAll(),
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
				Application.Resources.Workspaces.New( name: name );
			} );
			_workspaceListLayout.OnCellClicked.AddListener( cellData => {
				Application.State.Contexts.Current.SetWorkspace( cellData.WorkspaceMetadata.UID );
				_presenceControl.Force( size: Model.Presence.Sizes.Compact );
			} );
			_closeActiveWorkspaceButton.onClick.AddListener( () => {
				Application.State.Contexts.Current.SetWorkspace( null );
				_presenceControl.Force( size: Model.Presence.Sizes.Expanded );
			} );
			_presenceControl.OnSizeChanged.AddListener( presenceSize => {
				var isExpanded = presenceSize == Model.Presence.Sizes.Expanded;
				_workspaceListContainer.gameObject.SetActive( isExpanded );
				_newWorkspaceButton.gameObject.SetActive( isExpanded );
			} );

			// listen to application events
			Application.Resources.Workspaces.OnMetadataChanged += metadata => _allWorkspaces.Set( metadata );

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );
			_newWorkspaceDialog.SetTitle( title: "New Workspace" );
			_newWorkspaceDialog.SetValidators( validator: Application.Resources.Workspaces.ValidateName );
		}
	}

}