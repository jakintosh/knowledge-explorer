using Framework;
using System.Collections.Generic;

namespace Explorer.Client {

	// manages the set of contexts of the application
	public class Contexts {

		// events
		public event Event<Context>.Signature OnCurrentContextChanged;
		public event Event<Context>.Signature OnContextCreated;
		public event Event<string>.Signature OnContextDeleted;

		// properties
		public Context Current => _current.Get();
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

		public string NewContext ( bool setToCurrent = true ) {

			var uid = GetUID();
			var context = new Context( uid );
			_contexts.Add( uid, context );

			Event<Context>.Fire(
				@event: OnContextCreated,
				value: context,
				id: $"Explorer.Model.Contexts.OnContextCreated"
			);

			if ( setToCurrent ) {
				SetCurrentContext( uid );
			}

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
		public void SetCurrentContext ( string uid ) {

			var context = _contexts[uid];
			_current.Set( context );
		}

		// internal data
		private Dictionary<string, Context> _contexts;
		private Observable<Context> _current;

		// private helpers
		private string GetUID () {

			return StringHelpers.UID.Generate(
				length: 4,
				validateUniqueness: candidate => _contexts.KeyIsUnique( candidate )
			);
		}
	}

	// an instance of the application state
	public class Context : IdentifiableResource<Context> {

		// events
		public event Event<Context>.Signature OnContextModified;

		// properties
		public Knowledge.Graph Graph => _graph;
		public Model.View.Workspace Workspace => _workspace;

		public Context ( string uid ) : base( uid ) { }

		public void SetWorkspace ( string uid ) {

			_workspace = Client.Application.Resources.Workspaces.Get( uid );
			_graph = Client.Application.Resources.Graphs.Get( _workspace?.GraphUID );

			Event<Context>.Fire(
				@event: OnContextModified,
				value: this,
				id: $"Explorer.Model.Context.OnContextModified()"
			);
		}

		// internal data
		private Knowledge.Graph _graph;
		private Model.View.Workspace _workspace;
	}

}