using Jakintosh.Observable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.View {

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
		[SerializeField] private TextMeshProUGUI _activeWorkspaceText;
		[Space]
		[SerializeField] private RectTransform _contentContainer;
		[Space]
		[SerializeField] private WorkspaceBrowserTab _browserPanel;
		[SerializeField] private WorkspaceCreateTab _createPanel;
		[SerializeField] private GameObject _settingsPanel;

		// view model
		private Observable<Tabs> _tab;

		protected override void OnInitialize () {

			// init subviews
			_browserPanel.Init();
			_createPanel.Init();

			// init observables
			_tab = new Observable<Tabs>(
				initialValue: Tabs.None,
				onChange: tab => {
					_contentContainer.gameObject.SetActive( tab != Tabs.None );
					_browserPanel.gameObject.SetActive( tab == Tabs.Browser );
					_createPanel.gameObject.SetActive( tab == Tabs.Create );
					_settingsPanel.gameObject.SetActive( tab == Tabs.Settings );
					LayoutRebuilder.ForceRebuildLayoutImmediate( _contentContainer );
				}
			);

			// subscribe to controls
			Client.Contexts.Current.OnContextStateModified.AddListener( state => _activeWorkspaceText.text = state.Workspace.Name );

			_activeWorkspaceButton.onClick.AddListener( () => { _tab.Set( _tab.Get() != Tabs.Browser ? Tabs.Browser : Tabs.None ); } );
			_newWorkspaceButton.onClick.AddListener( () => { _tab.Set( _tab.Get() != Tabs.Create ? Tabs.Create : Tabs.None ); } );
			_settingsButton.onClick.AddListener( () => { _tab.Set( _tab.Get() != Tabs.Settings ? Tabs.Settings : Tabs.None ); } );

			_browserPanel.OnDismiss.AddListener( () => { _tab.Set( Tabs.None ); } );
			_createPanel.OnDismiss.AddListener( () => { _tab.Set( Tabs.None ); } );
		}

		protected override void OnCleanup () { }
	}

}