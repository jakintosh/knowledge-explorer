using Jakintosh.Observable;
using UnityEngine;

namespace Explorer.View {

	public class AppContext : BaseContext {

		[Header( "UI Controls" )]
		[SerializeField] private Document _document;

		[Header( "UI Display" )]

		// data
		private Client.ExplorerContext _context;
		private Observable<Client.ExplorerContextState> _contextState;

		// view lifecycle
		public override Client.ExplorerContext GetState () {
			return _context;
		}
		protected override void OnInitialize () {

			// init subviews
			_document.Init();

			// init observables
			_contextState = new Observable<Client.ExplorerContextState>(
				initialValue: Client.ExplorerContextState.Null,
				onChange: contextState => {

				}
			);

			// sub to controls
		}
		protected override void OnPopulate ( Client.ExplorerContext context ) {

			if ( context == null ) {
				throw new System.NullReferenceException( message: "View.AppContext.OnPopulate: Tried to populate with null context." );
			}

			_context = context;

			// context state
			_contextState.Set( _context.State );
			_context.OnContextStateModified.AddListener( _contextState.Set );
		}
		protected override void OnRecycle () {

			_context.OnContextStateModified.RemoveListener( _contextState.Set );
		}
		protected override void OnCleanup () { }
	}

}