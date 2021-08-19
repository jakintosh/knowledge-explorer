namespace Jakintosh.Actions {

	public class DoActionFailureException : System.Exception {
		public DoActionFailureException ( string message ) : base( message ) { }
	}
	public class UndoActionFailureException : System.Exception {
		public UndoActionFailureException ( string message ) : base( message ) { }
	}

	public interface IHistoryAction {

		string GetName ();
		string GetDescription ();

		void Do ();
		void Undo ();
		void Retire ();
	}

}