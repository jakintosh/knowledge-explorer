using Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Client.View {

	public class Workspace : ModelHandler<ViewModel.Workspace> {

		// ********** Model Handler **********

		protected override string BindingKey => "view.workspace";

		protected override void PropogateModel ( ViewModel.Workspace model ) {

			_toolbar?.SetModel( model );
		}
		protected override void BindViewToOutputs ( ViewModel.Workspace model ) {

			Bind( _nodesBinding, toOutput: model.Nodes );
		}

		protected override void HandleNullModel () {

			_toolbar.gameObject.SetActive( false );
		}
		protected override void HandleNonNullModel () {

			_toolbar.gameObject.SetActive( true );
		}

		// ********** Private Interface **********

		[Header( "UI Subviews" )]
		[SerializeField] private WorkspaceToolbar _toolbar;

		[Header( "Prefabs" )]
		[SerializeField] private Window _windowPrefab;

		private Dictionary<string, Window> _windows = new Dictionary<string, Window>();

		// bindings
		private ListOutput<ViewModel.Node>.Binding _nodesBinding;

		private void Awake () {

			_nodesBinding = new ListOutput<ViewModel.Node>.Binding( valueHandler: nodes => {

				// create windows for new nodes
				var uids = new List<string>();
				foreach ( var node in nodes ) {
					var uid = node.UID.Get();
					uids.Add( uid );
					if ( _windows.KeyIsUnique( uid ) ) {
						var window = Instantiate<Window>( _windowPrefab );
						window.SetModel( node.Window );
						_windows[uid] = window;
					}
				}

				// hide nodes that are gone now
				foreach ( var window in _windows ) {
					window.Value.gameObject.SetActive( uids.Contains( window.Key ) );
				}
			} );
		}

	}

}