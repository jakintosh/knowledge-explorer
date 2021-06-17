namespace Explorer.Client.Subsystems {

	public class State<TContext, TState> : ISubsystem
		where TContext : IContext<TState> {

		public IContexts<TContext, TState> Contexts => _contexts;

		public State ( IContexts<TContext, TState> contexts ) {

			_contexts = contexts;
		}
		public void Initialize () {

			// TODO: make this load something
			_contexts.CreateContext();
		}
		public void Teardown () { }

		// internal data
		private IContexts<TContext, TState> _contexts;
	}
}