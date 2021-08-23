using Jakintosh.Actions;
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


		// ********** Private Interface **********

		[Header( "UI Config" )]
		[SerializeField] private List<View> _rootViews;

		// data
		private Resources.Workspaces _workspaces;
		private Resources.Graphs _graphs;
		private State _state;
		private History _history;


		protected override void Init () {

			// Framework.Data.PersistentStore.IsLoggingEnabled = false;

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
		}

	}
}