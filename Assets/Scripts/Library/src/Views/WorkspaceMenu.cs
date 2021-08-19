using Jakintosh.Observable;
using Jakintosh.Resources;
using Jakintosh.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Library.Views {

	public class WorkspaceMenu : View {

		private enum Tabs {
			None,
			Browser,
			Create,
			Settings
		}

		[Header( "UI Control" )]
		[SerializeField] private Button _activeWorkspaceButton;
		[SerializeField] private Button _newWorkspaceButton;
		[SerializeField] private Button _settingsButton;

		[Header( "UI Display" )]
		[SerializeField] private Panel _panel;
		[Space]
		[SerializeField] private TextMeshProUGUI _activeWorkspaceText;
		[Space]
		[SerializeField] private RectTransform _contentContainer;
		[Space]
		[SerializeField] private WorkspaceBrowserTab _browserPanel;
		[SerializeField] private WorkspaceCreateTab _createPanel;
		[SerializeField] private WorkspaceSettingsTab _settingsPanel;

		// view model
		private Observable<string> _activeWorkspaceUID;
		private Observable<string> _activeWorkspaceName;
		private Observable<Tabs> _tab;

		// private data
		private bool _layoutGroupsHaveInitialized = false;

		protected override void OnInitialize () {

			// init subviews
			_panel.Init();
			_browserPanel.Init();
			_createPanel.Init();
			_settingsPanel.InitWith( Library.App.State.ActiveWorkspaceUID.Get() );

			// init observables
			_activeWorkspaceUID = new Observable<string>(
				initialValue: Library.App.State.ActiveWorkspaceUID.Get(),
				onChange: uid => {
					_settingsPanel.InitWith( uid );
				}
			);
			_activeWorkspaceName = new Observable<string>(
				initialValue: Library.App.State.ActiveWorkspace.Get()?.Name,
				onChange: name => {
					if ( name.IsNullOrEmpty() ) {
						name = "Open Workspace";
					}
					_activeWorkspaceText.text = name;

					RelayoutPanel();
				}
			);
			_tab = new Observable<Tabs>(
				initialValue: Tabs.None,
				onChange: tab => {

					_contentContainer.gameObject.SetActive( tab != Tabs.None );
					_browserPanel.gameObject.SetActive( tab == Tabs.Browser );
					_createPanel.gameObject.SetActive( tab == Tabs.Create );
					_settingsPanel.gameObject.SetActive( tab == Tabs.Settings );

					RelayoutPanel();
				}
			);

			// subscribe to controls
			_browserPanel.OnDismiss.AddListener( () => {
				_tab.Set( Tabs.None );
			} );
			_createPanel.OnDismiss.AddListener( () => {
				_tab.Set( Tabs.None );
			} );
			_settingsPanel.OnDismiss.AddListener( () => {
				_tab.Set( Tabs.None );
			} );
			_activeWorkspaceButton.onClick.AddListener( () => {
				_tab.Set( _tab.Get() != Tabs.Browser ? Tabs.Browser : Tabs.None );
			} );
			_newWorkspaceButton.onClick.AddListener( () => {
				_tab.Set( _tab.Get() != Tabs.Create ? Tabs.Create : Tabs.None );
			} );
			_settingsButton.onClick.AddListener( () => {
				_tab.Set( _tab.Get() != Tabs.Settings ? Tabs.Settings : Tabs.None );
			} );

			// app notifications
			Library.App.State.ActiveWorkspace.Subscribe( HandleNewWorkspace );
			App.Workspaces.OnMetadataUpdated += HandleMetadataUpdated;

			StartCoroutine( MarkLayoutGroupsInitialized() );
		}
		protected override void OnCleanup () {

			// app notifications
			Library.App.State.ActiveWorkspace.Unsubscribe( HandleNewWorkspace );
			App.Workspaces.OnMetadataUpdated -= HandleMetadataUpdated;
		}

		private System.Collections.IEnumerator MarkLayoutGroupsInitialized () {

			yield return new WaitForEndOfFrame();
			_layoutGroupsHaveInitialized = true;
			RelayoutPanel();
		}
		private void RelayoutPanel () {

			if ( !_layoutGroupsHaveInitialized ) {
				return;
			}

			var rt = gameObject.GetRectTransform();
			LayoutRebuilder.ForceRebuildLayoutImmediate( _contentContainer );
			LayoutRebuilder.ForceRebuildLayoutImmediate( rt );
			_panel.SetSizeFromCanvas( TextEdit.Bounds.FromRectTransform( rt ).Size );
		}

		// handle events
		private void HandleNewWorkspace ( ViewModel.Workspace workspace ) {

			_activeWorkspaceUID.Set( workspace?.UID );
			_activeWorkspaceName.Set( workspace?.Name );
		}
		private void HandleMetadataUpdated ( Metadata metadata ) {

			// guard against wrong workspace metadata
			var uid = _activeWorkspaceUID.Get();
			if ( uid == null || uid != metadata.UID ) { return; }

			_activeWorkspaceName.Set( metadata.Name );
		}
	}

}