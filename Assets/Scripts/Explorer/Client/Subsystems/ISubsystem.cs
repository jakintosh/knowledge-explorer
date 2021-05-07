using System.Collections.Generic;

namespace Explorer.Client {

	public interface ISubsystem {
		void Initialize ();
		void Teardown ();
	}

	public class SubsystemStack {

		public SubsystemStack () {

			_subsystems = new Stack<ISubsystem>();
		}
		public void Push ( ISubsystem subsystem ) {

			subsystem.Initialize();
			_subsystems.Push( subsystem );
		}
		public void Teardown () {

			while ( _subsystems.Count > 0 ) {
				_subsystems.Pop().Teardown();
			}
		}

		private Stack<ISubsystem> _subsystems;
	}

}