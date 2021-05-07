using Framework;
using UnityEngine;

namespace Explorer.View {

	/*
		Context

		View that represents a full "instance" of the application. To follow
		changes to a new context, there should be a new instance of a context.
	*/
	public class Context : View<Client.Context> {

		[Header( "UI Controls" )]
		[SerializeField] private WorkspaceBrowser _workspaceBrowser = null;
		[SerializeField] private RelationTypeBrowser _relationTypeBrowser = null;

		[Header( "UI Display" )]
		[SerializeField] private GraphViewport _graphViewport = null;

		// view model data
		private Observable<Model.View.Workspace> _workspace;
		private Observable<Knowledge.Graph> _graph;

		protected override void InitFrom ( Client.Context context ) {

			// init subviews
			InitView( _workspaceBrowser );
			InitView( _relationTypeBrowser );
			InitView( _graphViewport );

			// listen for context changes
			context.OnContextModified += context => {

				_workspaceBrowser.SetActiveWorkspace(
					workspace: context.Workspace
				);
				_relationTypeBrowser.SetContext(
					workspace: context.Workspace,
					graph: context.Graph
				);
				_graphViewport.SetContext(
					workspace: context.Workspace,
					graph: context.Graph
				);
			};
		}
		protected override void Init () { throw new System.NotImplementedException(); }
		public override Client.Context GetInitData () { throw new System.NotImplementedException(); }

	}

}