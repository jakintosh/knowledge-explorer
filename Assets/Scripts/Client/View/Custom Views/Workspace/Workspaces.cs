using Framework;
using UnityEngine;

namespace Client.View {

	public class Workspaces : ModelHandler<ViewModel.Workspaces> {

		// ********** Model Handler **********

		protected override string BindingKey => "view.workspaces";

		protected override void PropogateModel ( ViewModel.Workspaces model ) {

			_workspaceBrowser.SetModel( model?.WorkspaceBrowser );
		}
		protected override void HandleNullModel () => throw new System.NotImplementedException();
		protected override void BindViewToOutputs ( ViewModel.Workspaces model ) {

			Bind( _activeWorkspaceBinding, toOutput: model.ActiveWorkspace );
		}


		// ********** Private Interface **********

		[Header( "UI Subviews" )]
		[SerializeField] private Workspace _workspace;
		[SerializeField] private WorkspaceBrowser _workspaceBrowser;

		private Output<ViewModel.Workspace>.Binding _activeWorkspaceBinding;

		private void Awake () {

			// bindings
			_activeWorkspaceBinding = new Output<ViewModel.Workspace>.Binding( valueHandler: activeWorkspace => {
				_workspace.SetModel( activeWorkspace );
			} );
		}
	}

}