using Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Sizes = Client.ViewModel.Presence.Sizes;
using ResourceMetadata = Framework.Data.Metadata.Resource;

namespace Client.View {

	public class WorkspaceBrowser : ModelHandler<ViewModel.WorkspaceBrowser> {


		// ****** ModelHandler Implementation ******

		protected override string BindingKey => $"view.workspace-browser-{GetInstanceID()}";

		protected override void PropogateModel ( ViewModel.WorkspaceBrowser model ) {

			_presenceControl.SetModel( model?.Presence );
			_newWorkspaceDialog.SetModel( model?.NewWorkspaceDialog );
		}
		protected override void BindViewToOutputs ( ViewModel.WorkspaceBrowser model ) {

			Bind( _dialogOpenBinding, toOutput: model.NewWorkspaceDialog.IsOpen );
			Bind( _presenceSizeBinding, toOutput: model.Presence.Size );
			Bind( _activeWorkspaceBinding, toOutput: model.ActiveWorkspace );
			Bind( _allWorkspacesBinding, toOutput: model.AllWorkspaces );
		}

		protected override void HandleNullModel () => throw new System.NotImplementedException();

		// *********** Private Interface ***********

		[Header( "UI Subviews" )]
		[SerializeField] private PresenceControl _presenceControl;
		[SerializeField] private ValidatedTextEntryDialog _newWorkspaceDialog;

		[Header( "UI Control" )]
		[SerializeField] private Button _newButton;
		[SerializeField] private Button _closeActiveWorkspaceButton;
		[SerializeField] private WorkspaceListLayout _workspaceListLayout;

		[Header( "UI Display" )]
		[SerializeField] private TextMeshProUGUI _activeWorkspaceNameText;
		[SerializeField] private GameObject _activeWorkspaceContainer;
		[SerializeField] private GameObject _workspaceListContainer;
		[SerializeField] private GameObject _fade;

		// bindings
		private Output<bool>.Binding _dialogOpenBinding;
		private Output<Sizes>.Binding _presenceSizeBinding;
		private Output<ViewModel.Workspace>.Binding _activeWorkspaceBinding;
		private ListOutput<ResourceMetadata>.Binding _allWorkspacesBinding;

		private void Awake () {

			// configure subviews
			_presenceControl.SetEnabled( close: false, size: true, context: false );

			// connect controls
			_newButton.onClick.AddListener( () => {
				_model?.OpenNewWorkspaceDialog();
			} );
			_newWorkspaceDialog.OnConfirm.AddListener( text => {
				_model?.ConfirmNewWorkspace( text );
			} );
			_workspaceListLayout.OnCellClicked.AddListener( cellData => {
				_model?.OpenWorkspace( cellData.WorkspaceMetadata.UID );
			} );
			_closeActiveWorkspaceButton.onClick.AddListener( () => {
				_model?.CloseActiveWorkspace();
			} );

			// create bindings
			_dialogOpenBinding = new Output<bool>.Binding( valueHandler: isOpen => {
				_fade.SetActive( isOpen );
				_newWorkspaceDialog.gameObject.SetActive( isOpen );
				_newButton.interactable = !isOpen;
				_presenceControl.SetInteractive( close: false, size: !isOpen, context: false );
			} );
			_presenceSizeBinding = new Output<Sizes>.Binding( valueHandler: presenceSize => {
				var isExpanded = presenceSize == Sizes.Expanded;
				_workspaceListContainer.gameObject.SetActive( isExpanded );
				_newButton.gameObject.SetActive( isExpanded );
			} );
			_activeWorkspaceBinding = new Output<ViewModel.Workspace>.Binding( valueHandler: activeWorkspace => {
				_activeWorkspaceNameText.text = activeWorkspace?.Name.Get() ?? "---";
				_closeActiveWorkspaceButton.gameObject.SetActive( activeWorkspace != null );
			} );
			_allWorkspacesBinding = new ListOutput<ResourceMetadata>.Binding( valueHandler: allWorkspaces => {
				var cells = allWorkspaces?.Convert( metadata => new WorkspaceCellData( title: metadata.Name, metadata: metadata ) );
				_workspaceListLayout.SetData( cells );
			} );

		}

	}
}
