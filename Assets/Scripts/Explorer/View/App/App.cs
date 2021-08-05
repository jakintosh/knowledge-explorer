using SouthPointe.Serialization.MessagePack;
using System.Collections.Generic;
using UnityEngine;

namespace Explorer.Client {

	public class App : MonoBehaviour {

		[SerializeField] private List<View.View> _rootViews;

		private void Awake () {

			// Framework.Data.PersistentStore.IsLoggingEnabled = false;
			InitHolochain();

			// create an initial context
			Contexts.New();

			// init all root views
			_rootViews.ForEach( view => view.Init() );
		}
		private void Update () {

			UpdateHolochain();
		}
		private void OnApplicationQuit () {

			ShutdownHolochain();
		}


		// holochain stuff

		[System.Serializable]
		public class MyInput {
			public string first_name;
			public string last_name;
			public MyInput ( string firstName, string lastName ) {
				first_name = firstName;
				last_name = lastName;
			}
		}

		[System.Serializable]
		public class MyOutput {
			public string output;
			public override string ToString () => $"MyOutput({output})";
		}

		private Holochain.AppConductor conductor;

		private void InitHolochain () {

			conductor = new Holochain.AppConductor(
				dnaHash: Holochain.DNAHash.Get(),
				agentPubKey: Holochain.AgentPubKey.Get()
			);
			conductor.OnResponse.AddListener( bytes => {
				try {

					var formatter = new MessagePackFormatter();
					var content = formatter.Deserialize<Explorer.Client.App.MyOutput>( bytes );
					Debug.Log( $"Holochain: AppConductor received content:\n{content.output}" );

				} catch { Debug.Log( "Holochain: Couldn't deserialize zome_call bytes into MyOutput" ); }
			} );
		}
		private void UpdateHolochain () {

			conductor.DispatchMessageQueue();
			if ( Input.GetKeyDown( KeyCode.Space ) ) { SendRequest(); }
		}
		private void ShutdownHolochain () {

			conductor.CloseConnection();
		}
		private void SendRequest () {

			var formatter = new MessagePackFormatter();
			var payload = formatter.Serialize(
				new MyInput(
					firstName: "Mochi",
					lastName: "May"
				)
			);
			conductor.CallFunction(
				zome: "simple",
				function: "say_my_name",
				payload: payload
			);
		}
	}
}