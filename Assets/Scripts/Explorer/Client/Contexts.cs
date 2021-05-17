using Framework;
using System;
using System.Collections.Generic;

namespace Explorer.Client {

	// manages the set of contexts of the application
	public class Contexts {

		// *********** Public Interface ***********

		// events
		public event Event<Context>.Signature OnCurrentContextChanged;
		public event Event<Context>.Signature OnContextCreated;
		public event Event<string>.Signature OnContextDeleted;

		// properties
		public string CurrentUID => _current.Get()?.UID;
		public IReadOnlyDictionary<string, Context> All => _contexts;

		public Contexts () {

			_contexts = new Dictionary<string, Context>();

			// init observables
			_current = new Observable<Context>(
				initialValue: null,
				onChange: context => {
					Event<Context>.Fire(
						@event: OnCurrentContextChanged,
						value: context,
						id: $"Explorer.Model.Contexts.OnCurrentContextChanged"
					);
				}
			);
		}

		public Context GetContext ( string uid ) {

			if ( !_contexts.TryGetValue( uid, out var context ) ) {
				UnityEngine.Debug.LogError( $"Contexts.GetContext: Can't find context for UID-{uid}" );
				return null;
			}
			return context;
		}
		public void SetCurrentContext ( string uid ) {

			var context = _contexts[uid];
			_current.Set( context );
		}
		public string CreateContext ( bool setToCurrent = true ) {

			var uid = NewUID();
			var context = new Context( uid );
			_contexts.Add( uid, context );

			Event<Context>.Fire(
				@event: OnContextCreated,
				value: context,
				id: $"Explorer.Model.Contexts.OnContextCreated"
			);

			if ( setToCurrent ) { SetCurrentContext( uid ); }

			return uid;
		}
		public void DeleteContext ( string uid ) {

			if ( !_contexts.TryGetValue( uid, out var context ) ) {
				// doesnt exist
				return;
			}

			if ( _current.Get().Equals( context ) ) {
				// figure out which one becomes active
			}

			_contexts.Remove( uid );

			Event<string>.Fire(
				@event: OnContextDeleted,
				value: uid,
				id: $"Explorer.Model.Contexts.OnContextDeleted"
			);

		}


		// *********** Private Interface ***********

		// internal data
		private Dictionary<string, Context> _contexts;
		private Observable<Context> _current;

		// private helpers
		private string NewUID () {

			return StringHelpers.UID.Generate(
				length: 4,
				validateUniqueness: candidate => _contexts.KeyIsUnique( candidate )
			);
		}
	}

	public struct ContextState : IEquatable<ContextState> {

		// static defaults
		public static ContextState Null => new ContextState( null );

		// properties
		public View.Model.Workspace Workspace { get; private set; }

		// constructor
		public ContextState ( View.Model.Workspace workspace ) => Workspace = workspace;

		// IEquatable<ContextState>
		public bool Equals ( ContextState other ) => ReferenceEquals( Workspace, other.Workspace );
	}

	// an instance of the application state
	public class Context : IdentifiableResource<Context> {

		// *********** Public Interface ***********

		// events
		public event Event<ContextState>.Signature OnContextStateModified;

		// properties
		public ContextState State => _state.Get();

		public Context ( string uid ) : base( uid ) {

			_state = new Observable<ContextState>(
				initialValue: new ContextState( null ),
				onChange: state => {

					// fire event
					Event<ContextState>.Fire(
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
				_state.Set( ContextState.Null );
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

			_state.Set( new ContextState( workspace ) );
		}

		// *********** Private Interface ***********

		// internal data
		private Observable<ContextState> _state;
	}

}