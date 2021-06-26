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
		private ListObservable<Jakintosh.Resources.Metadata> _workspaces;

		protected override void OnInitialize () {

			// init subviews
			_scroll.Init();

			// init observables
			_activeWorkspaceUID = new Observable<string>(
				initialValue: Client.Contexts.Current.State.Workspace?.UID,
				onChange: uid => {
					PopulateList( uid, _workspaces?.Get() );
				}
			);
			_workspaces = new ListObservable<Jakintosh.Resources.Metadata>(
				initialValue: Client.Resources.Workspaces.GetAll(),
				onChange: workspaces => {
					PopulateList( _activeWorkspaceUID.Get(), workspaces );
				}
			);

			// subscribe to controls
			Client.Resources.Workspaces.OnMetadataChanged += _workspaces.Set;
			Client.Contexts.Current.OnContextStateModified.AddListener( state => {
				_activeWorkspaceUID.Set( state.Workspace.UID );
			} );
			_workspaceList.OnCellClicked.AddListener( cellData => {
				Client.Contexts.Current.SetWorkspace( cellData.WorkspaceMetadata.UID );
				OnDismiss?.Invoke();
			} );
		}
		protected override void OnCleanup () {

			Client.Resources.Workspaces.OnMetadataChanged -= _workspaces.Set;
		}

		private void OnEnable () {

			_scroll.ResetScrollOffset();
			StartCoroutine( LayoutListContent() );
		}

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