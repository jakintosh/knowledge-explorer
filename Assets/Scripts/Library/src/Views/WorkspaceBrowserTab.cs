using Explorer.View;

using Jakintosh.Observable;
using Jakintosh.View;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Library.Views {

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
			_workspaces = App.Workspaces.GetAll();

			// init subviews
			_scroll.Init();

			// init observables
			_activeWorkspaceUID = new Observable<string>(
				initialValue: Library.App.State.ActiveWorkspaceUID.Get(),
				onChange: uid => {
					PopulateList( uid, _workspaces );
				}
			);

			// subscribe to controls
			_workspaceList.OnCellClicked.AddListener( cellData => {
				Library.App.History.ExecuteAction(
					new Actions.Workspace.Open( uid: cellData.WorkspaceMetadata.UID )
				);
				OnDismiss?.Invoke();
			} );

			// sub to app
			App.Workspaces.OnAnyMetadataChanged += HandleNewMetadataList;
			Library.App.State.ActiveWorkspaceUID.Subscribe( _activeWorkspaceUID.Set );
		}
		protected override void OnCleanup () {

			App.Workspaces.OnAnyMetadataChanged -= HandleNewMetadataList;
			Library.App.State.ActiveWorkspaceUID.Unsubscribe( _activeWorkspaceUID.Set );
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