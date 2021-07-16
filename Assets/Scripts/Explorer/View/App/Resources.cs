using UnityEngine;

namespace Explorer.Client {

	public class Resources : MonoBehaviour {

		// ********** Public Interface **********

		public static Model.IGraphCRUD Graphs => Instance._resources.Graphs;
		public static Model.IWorkspaceCRUD Workspaces => Instance._resources.Workspaces;


		// ********** Private Interface **********

		private Subsystems.Resources _resources = new Subsystems.Resources();

		private static bool _isQuitting = false;

		private static Resources _instance;
		private static Resources Instance {
			get {
				if ( _instance == null ) {
					GetNewInstance();
				}
				return _instance;
			}
		}
		private static void GetNewInstance () {

			if ( _isQuitting ) { return; }

			_instance = GameObject.FindObjectOfType<Resources>();
			if ( _instance == null ) {
				_instance = new GameObject( "Resources" ).AddComponent<Resources>();
			}
			_instance._resources.Initialize();
		}


		private void Awake () {

			if ( _instance == null ) {
				GetNewInstance();
			} else if ( _instance != null && _instance != this ) {
				Destroy( this );
			}
		}
		private void OnDestroy () {

			_resources.Teardown();
		}
		private void OnApplicationQuit () {
			_isQuitting = true;
		}
	}
}