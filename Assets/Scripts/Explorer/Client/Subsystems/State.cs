namespace Explorer.Client.Subsystems {

	public class State : ISubsystem {

		public Contexts Contexts => _contexts;

		public State () {

			_contexts = new Contexts();
		}
		public void Initialize () {

			// TODO: make this load something
			_contexts.NewContext();
		}
		public void Teardown () { }

		// internal data
		private Contexts _contexts;
	}
}