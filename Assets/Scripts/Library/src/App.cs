using Jakintosh.Actions;
using Jakintosh.Data;
using Jakintosh.View;
using System.Collections.Generic;
using UnityEngine;

namespace Library {

	public class App : MonoBehaviourSingleton<App> {


		// ********** Public Interface **********

		public static State State => Instance._state;
		public static History History => Instance._history;
		public static Resources.Workspaces Workspaces => Instance._workspaces;
		public static Resources.Graphs Graphs => Instance._graphs;
		public static Resources.Data Data => Instance._data;


		// ********** Private Interface **********

		[Header( "UI Config" )]
		[SerializeField] private List<View> _rootViews;

		// data
		private Resources.Workspaces _workspaces;
		private Resources.Graphs _graphs;
		private State _state;
		private History _history;

		private Resources.Data _data;
		private string _dataPath = "/local/data.msp";

		[System.Serializable]
		public class MyStruct : IBytesSerializable {
			public int myInt = 0;
			public float myFloat = 0;

			public byte[] GetSerializedBytes () => Serializer.GetSerializedBytes( this );
		}

		protected override void Init () {

			var myStruct = new MyStruct {
				myFloat = 1.0f,
				myInt = 64
			};

			var cache = new Coalescent.Computer.Cache();
			var store = new Coalescent.Computer.Store(
				cache: cache,
				rootDiskPath: System.IO.Path.Combine( Application.persistentDataPath, "local" )
			);
			var hash = store.Put( myStruct );
			Debug.Log( $"hash: {hash}" );

			// Framework.Data.PersistentStore.IsLoggingEnabled = false;

			// load data
			try {
				_data = Framework.Data.PersistentStore.LoadFromMsgPackBytes_Throws<Resources.Data>( _dataPath );
			} catch ( System.Exception ex ) {
				Debug.LogError( ex.Message );
				_data = new Resources.Data();
			}
			_data.Init();

			// init data
			_graphs = new Resources.Graphs( rootDataPath: "/data/local" );
			_workspaces = new Resources.Workspaces( rootDataPath: "/data/local" );
			_state = new State();
			_history = new History( size: 128 );

			// init views
			_rootViews.ForEach( view => view.Init() );
		}
		protected override void Deinit () {

			_history.Flush();
			_workspaces.Close();
			_graphs.Close();

			Framework.Data.PersistentStore.WriteToMsgPackBytes_Throws( _dataPath, _data );
		}

	}
}