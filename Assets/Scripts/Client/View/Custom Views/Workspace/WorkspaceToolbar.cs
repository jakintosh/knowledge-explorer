using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Client.View {

	public class WorkspaceToolbar : ModelHandler<ViewModel.Workspace> {


		// ********** Model Handler **********

		protected override string BindingKey => "view.workspace-toolbar";

		protected override void PropogateModel ( ViewModel.Workspace model ) { }
		protected override void BindViewToOutputs ( ViewModel.Workspace model ) { }


		// ********** Private Interface **********

		[SerializeField] private Button _addButton;
		[SerializeField] private Button _saveButton;


		private void Awake () {

			// connect controls
			_addButton.onClick.AddListener( () => {
				_model?.CreateNewNode();
			} );

			// bindings

		}
	}

}