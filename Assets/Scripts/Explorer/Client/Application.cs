using System.Collections.Generic;
using UnityEngine;

namespace Explorer.Client {

	public class Application : MonoBehaviour {

		// ********** Public Interface **********

		public static Subsystems.Resources Resources => Instance._resources;
		public static Subsystems.State State => Instance._state;


		// ********** Private Interface **********

		// subsystems
		private SubsystemStack _subsystems;
		private Subsystems.Resources _resources;
		private Subsystems.State _state;

		private void Initialize () {

			_subsystems = new SubsystemStack();
			_resources = new Subsystems.Resources();
			_state = new Subsystems.State();

			_subsystems.Push( _resources );
			_subsystems.Push( _state );
		}
		private void Teardown () {

			_subsystems.Teardown();
		}

		// ui setup
		[Header( "Prefabs" )]
		[SerializeField] private View.Context _contextPrefab;

		private Dictionary<string, View.Context> _contextViewsByUID;

		private void SetupUI () {

			_contextViewsByUID = new Dictionary<string, View.Context>();

			// init all views
			_state.Contexts.All.ForEach( ( uid, context ) => {
				var view = Instantiate<View.Context>( _contextPrefab );
				view.InitWith( context );
				view.gameObject.SetActive( uid == _state.Contexts.CurrentUID );
				_contextViewsByUID.Add( uid, view );
			} );

			// listen for context changes
			_state.Contexts.OnCurrentContextChanged += current => {
				_contextViewsByUID.ForEach( ( uid, view ) => {
					view.gameObject.SetActive( uid == current.UID );
				} );
			};
			_state.Contexts.OnContextCreated += context => {
				var uid = context.UID;
				var view = Instantiate<View.Context>( _contextPrefab );
				view.InitWith( context );
				view.gameObject.SetActive( uid == _state.Contexts.CurrentUID );
				_contextViewsByUID.Add( uid, view );
			};
			_state.Contexts.OnContextDeleted += uid => {
				var view = _contextViewsByUID[uid];
				Destroy( view.gameObject );
				_contextViewsByUID.Remove( uid );
			};
		}
		private void TeardownUI () {

		}

		// lifecycle/mono/singleton
		private static Application _instance;
		private static Application Instance {
			get {
				if ( _instance == null ) {
					_instance = new GameObject( "Application" ).AddComponent<Application>();
				}
				return _instance;
			}
		}
		private void Awake () {

			if ( _instance == null ) {
				_instance = this;
				Initialize();
				SetupUI();
			} else {
				Destroy( gameObject );
				return;
			}
		}
		private void OnApplicationQuit () {

			if ( _instance == this ) {
				TeardownUI();
				Teardown();
			}
		}
	}

}