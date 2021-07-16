using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	public class WorkspaceBrowserTab : View {

		// *********** Public Interface ***********

		public UnityEvent OnDismiss = new UnityEvent();


		// *********** Private Interface ***********

		[Header( "UI Control" )]
		[SerializeField] private TextEdit.Scroll _scroll;

		[Header( "UI Display" )]
		[SerializeField] private WorkspaceList _workspaceList;

		// view model
		private Observable<string> _activeWorkspaceUID;

		// private data
		private IList<Jakintosh.Resources.Metadata> _workspaces = null;

		protected override void OnInitialize () {

			// init vars
			_workspaces = Client.Resources.Workspaces.GetAll();

			// init subviews
			_scroll.Init();

			// init observables
			_activeWorkspaceUID = new Observable<string>(
				initialValue: Client.Contexts.Current.State.Workspace?.UID,
				onChange: uid => {
					PopulateList( uid, _workspaces );
				}
			);

			// subscribe to controls
			_workspaceList.OnCellClicked.AddListener( cellData => {
				Client.Contexts.Current.SetWorkspace( cellData.WorkspaceMetadata.UID );
				OnDismiss?.Invoke();
			} );

			// sub to app
			Client.Resources.Workspaces.OnAnyMetadataChanged += HandleNewMetadataList;
			Client.Contexts.Current.OnContextStateModified.AddListener( HandleNewContextState );
		}
		protected override void OnCleanup () {

			Client.Resources.Workspaces.OnAnyMetadataChanged -= HandleNewMetadataList;
			Client.Contexts.Current.OnContextStateModified.RemoveListener( HandleNewContextState );
		}

		// mono stuff
		private void OnEnable () {


			_scroll.ResetScrollOffset();
			StartCoroutine( LayoutListContent() );
		}

		// event handlers
		private void HandleNewMetadataList ( IList<Jakintosh.Resources.Metadata> workspaces ) {

			_workspaces = workspaces;
			PopulateList( _activeWorkspaceUID.Get(), _workspaces );
		}
		private void HandleNewContextState ( Client.ExplorerContextState state ) {

			_activeWorkspaceUID.Set( state.Workspace?.UID );
		}

		// list handling

		private void PopulateList ( string activeWorkspaceUID, IList<Jakintosh.Resources.Metadata> workspaces ) {

			var cellData = workspaces?.Convert( workspace =>
				new WorkspaceCellData(
					title: workspace.Name,
					active: workspace.UID == activeWorkspaceUID,
					metadata: workspace
				)
			);
			_workspaceList.SetData( cellData );

			LayoutListContent();
		}
		private System.Collections.IEnumerator LayoutListContent () {

			yield return new WaitForEndOfFrame();

			LayoutRebuilder.ForceRebuildLayoutImmediate( _workspaceList.gameObject.GetRectTransform() );
			_scroll.RefreshContentSize();
		}
	}
}