using Framework;
using UnityEngine;

namespace Client.View {

	public class Application : ModelHandler<ViewModel.Application> {

		// ****** ModelHandler Implementation ******

		protected override string BindingKey => "view.application";
		protected override void PropogateModel ( ViewModel.Application model ) {

			_workspaces.SetModel( model?.Workspaces );
		}
		protected override void BindViewToOutputs ( ViewModel.Application model ) { }

		protected override void HandleNullModel () => throw new System.NotImplementedException();


		// ************ Private Interface ***********

		[Header( "UI Subviews" )]
		[SerializeField] private Workspaces _workspaces;

		// [Header( "Debug" )]
		// [SerializeField]
		private Model.Application _dataModel;


		// ********* Application Data Model *********

		private static string RootDataPath => "/";

		private void Awake () {

			// init data model
			_dataModel = new Model.Application();
			_dataModel.Load();

			// set view model
			SetModel( new ViewModel.Application( workspaceModel: _dataModel as Model.IWorkspaceAPI ) );
		}
		protected override void OnDestroy () {

			base.OnDestroy();

			_dataModel.Unload();
		}

	}

}