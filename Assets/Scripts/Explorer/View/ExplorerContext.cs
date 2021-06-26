using Jakintosh.Observable;
using UnityEngine;

namespace Explorer.View {

	/*
		Context

		View that represents a full "instance" of the application. To follow
		changes to a new context, there should be a new instance of a context.
	*/
	public abstract class BaseContext : ReuseableView<Client.ExplorerContext> { }

	public class ExplorerContext : BaseContext {


		[Header( "UI Controls" )]
		[SerializeField] private WorkspaceBrowser _workspaceBrowser = null;
		[SerializeField] private RelationTypeBrowser _relationTypeBrowser = null;
		[SerializeField] private GraphToolbar _graphToolbar = null;

		[Header( "UI Display" )]
		[SerializeField] private GraphViewport _graphViewport = null;

		// data
		private Client.ExplorerContext _context;
		private Observable<Client.ExplorerContextState> _contextState;

		// view lifecycle
		public override Client.ExplorerContext GetState () {
			return _context;
		}
		protected override void OnInitialize () {

			// init subviews
			_workspaceBrowser.Init();
			_graphToolbar.Init();

			// init observables
			_contextState = new Observable<Client.ExplorerContextState>(
				initialValue: Client.ExplorerContextState.Null,
				onChange: contextState => {
					_workspaceBrowser.SetActiveWorkspace( contextState.Workspace );
					_relationTypeBrowser.InitWith( contextState.Workspace );
					_graphViewport.InitWith( contextState.Workspace?.GraphViewport );
				}
			);

			// sub to controls
			_graphToolbar.OnNewItem.AddListener( () => {
				Debug.Log( "Toolbar: New Item" );
				var graphUid = _context.State.Workspace?.GraphUID;
				_graphViewport.NewConcept( graphUid );
			} );
			_graphToolbar.OnSave.AddListener( () => {
				Debug.Log( "Toolbar: Save" );
				var workspace = _context.State.Workspace;
				if ( workspace == null ) { return; }
				workspace.GraphViewport = _graphViewport.GetState();
			} );
		}
		protected override void OnPopulate ( Client.ExplorerContext context ) {

			if ( context == null ) {
				throw new System.NullReferenceException( message: "View.Context.OnPopulate: Tried to populate with null context." );
			}

			Debug.Log( $"View.Context: OnPopulate(uid: {context.UID})" );

			_context = context;
			_workspaceBrowser.SetCurrentContextUID( context.UID );

			// context state
			_contextState.Set( _context.State );
			_context.OnContextStateModified.AddListener( _contextState.Set );
		}
		protected override void OnRecycle () {

			_context.OnContextStateModified.RemoveListener( _contextState.Set );
		}
	}

}