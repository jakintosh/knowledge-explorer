using Framework;
using UnityEngine;

namespace Client.View {

	public class Workspace : ModelHandler<ViewModel.Workspace> {

		// ********** Model Handler **********

		protected override string BindingKey => "view.workspace";

		protected override void PropogateModel ( ViewModel.Workspace model ) { }
		protected override void BindViewToOutputs ( ViewModel.Workspace model ) { }

		protected override void HandleNullModel () {

			_toolbar.gameObject.SetActive( false );
		}
		protected override void HandleNonNullModel () {

			_toolbar.gameObject.SetActive( true );
		}

		// ********** Private Interface **********

		[Header( "UI Subviews" )]
		[SerializeField] private WorkspaceToolbar _toolbar;
	}

}