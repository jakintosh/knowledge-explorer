using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Explorer.Client {

	// manages the set of contexts of the application
	public interface IContexts<TContext, TState>
		where TContext : IContext<TState> {

		UnityEvent<TContext> OnCurrentContextChanged { get; }
		UnityEvent<TContext> OnContextCreated { get; }
		UnityEvent<string> OnContextDeleted { get; }

		string CurrentUID { get; }
		IReadOnlyDictionary<string, TContext> All { get; }

		TContext GetContext ( string uid );
		void SetCurrentContext ( string uid );
		string CreateContext ( bool setToCurrent = true );
		void DeleteContext ( string uid );
	}

	// an instance of the application state
	public interface IContext<TState> {
		UnityEvent<TState> OnContextStateModified { get; }
		TState State { get; }
		string UID { get; }
	}

	// a base implementation for managing contexts
	public abstract class Contexts<TContext, TState> : IContexts<TContext, TState>
		where TContext : class, IContext<TState>
		where TState : class {

		// *********** Public Interface ***********

		// events
		public UnityEvent<TContext> OnCurrentContextChanged => _onCurrentContextChanged;
		public UnityEvent<TContext> OnContextCreated => _onContextCreated;
		public UnityEvent<string> OnContextDeleted => _onContextDeleted;

		// properties
		public string CurrentUID => _current.Get()?.UID;
		public IReadOnlyDictionary<string, TContext> All => _contexts;

		public Contexts () {

			_contexts = new Dictionary<string, TContext>();

			// init observables
			_current = new Observable<TContext>(
				initialValue: null,
				onChange: context => {
					OnCurrentContextChanged?.Invoke( context );
					// Event<TContext>.Fire(
					// 	@event: OnCurrentContextChanged,
					// 	value: context,
					// 	id: $"Explorer.Model.Contexts.OnCurrentContextChanged"
					// );
				}
			);
		}

		public TContext GetContext ( string uid ) {

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

			var context = NewContext();
			_contexts.Add( context.UID, context );

			OnContextCreated?.Invoke( context );

			if ( setToCurrent ) { SetCurrentContext( context.UID ); }

			return context.UID;
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

			OnContextDeleted?.Invoke( uid );
			// Event<string>.Fire(
			// 	@event: OnContextDeleted,
			// 	value: uid,
			// 	id: $"Explorer.Model.Contexts.OnContextDeleted"
			// );

		}

		protected abstract TContext NewContext ();

		// *********** Private Interface ***********

		// internal events
		private UnityEvent<TContext> _onCurrentContextChanged = new UnityEvent<TContext>();
		private UnityEvent<TContext> _onContextCreated = new UnityEvent<TContext>();
		private UnityEvent<string> _onContextDeleted = new UnityEvent<string>();

		// internal data
		private Dictionary<string, TContext> _contexts;
		private Observable<TContext> _current;

		// private helpers
		protected string NewUID () {

			return StringHelpers.UID.Generate(
				length: 4,
				validateUniqueness: candidate => _contexts.KeyIsUnique( candidate )
			);
		}
	}
}