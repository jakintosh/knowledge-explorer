using UnityEngine;

namespace Explorer.Client {

	public class Contexts : MonoBehaviour {

		// ********** Public Interface **********

		public static string CurrentUID => Instance._contexts.CurrentUID;

		public static ExplorerContext Current => Instance._contexts.GetContext( CurrentUID );

		public static void New ( bool setToCurrent = true ) => Instance._contexts.CreateContext( setToCurrent );


		// ********** Private Interface **********

		private ExplorerContexts _contexts = new ExplorerContexts();

		private static bool _isQuitting = false;

		private static Contexts _instance;
		private static Contexts Instance {
			get {
				if ( _instance == null ) {
					GetNewInstance();
				}
				return _instance;
			}
		}
		private static void GetNewInstance () {

			if ( _isQuitting ) { return; }

			_instance = GameObject.FindObjectOfType<Contexts>();
			if ( _instance == null ) {
				_instance = new GameObject( "Contexts" ).AddComponent<Contexts>();
			}
		}

		private void Awake () {

			if ( _instance != null && _instance != this ) {
				Destroy( this );
			} else {
				_instance = this;
			}
		}
		private void OnApplicationQuit () {

			_isQuitting = true;
		}
	}
}