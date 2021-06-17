using Framework;
using System;

namespace Explorer.Client {

	public class ExplorerContextState : IEquatable<ExplorerContextState> {

		// static defaults
		public static ExplorerContextState Null => new ExplorerContextState( null );

		// properties
		public View.Model.Workspace Workspace { get; private set; }

		// constructor
		public ExplorerContextState ( View.Model.Workspace workspace ) => Workspace = workspace;

		// IEquatable<ContextState>
		public bool Equals ( ExplorerContextState other ) => ReferenceEquals( Workspace, other.Workspace );
	}
	public class ExplorerContext : IdentifiableResource<ExplorerContext>, IContext<ExplorerContextState> {

		// *********** Public Interface ***********

		// events
		public event Event<ExplorerContextState>.Signature OnContextStateModified;

		// properties
		public ExplorerContextState State => _state.Get();

		public ExplorerContext ( string uid ) : base( uid ) {

			_state = new Observable<ExplorerContextState>(
				initialValue: ExplorerContextState.Null,
				onChange: state => {

					// fire event
					Event<ExplorerContextState>.Fire(
					@event: OnContextStateModified,
					value: state,
					id: $"Explorer.Model.Context.OnContextStateModified"
				);
				}
			);
		}
		public void SetWorkspace ( string uid ) {

			// don't bother loading if null
			if ( uid == null ) {
				_state.Set( ExplorerContextState.Null );
				return;
			}

			var workspace = Client.Application.Resources.Workspaces.Get( uid );
			if ( workspace == null ) {
				UnityEngine.Debug.LogError( $"Context.SetWorkspace: Couldn't find workspace for UID-{uid}" );
				return;
			}

			// ensure this is a valid graph, and pre-load it
			var graph = Client.Application.Resources.Graphs.Get( workspace.GraphUID );
			if ( graph == null ) {
				UnityEngine.Debug.LogError( $"Context.SetWorkspace: Couldn't find graph for UID-{workspace.GraphUID}" );
				return;
			}

			_state.Set( new ExplorerContextState( workspace ) );
		}

		// *********** Private Interface ***********

		// internal data
		private Observable<ExplorerContextState> _state;
	}
	public class ExplorerContexts : Contexts<ExplorerContext, ExplorerContextState> {

		protected override ExplorerContext NewContext () => new ExplorerContext( uid: NewUID() );
	}

}