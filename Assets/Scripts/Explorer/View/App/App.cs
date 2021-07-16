using System.Collections.Generic;
using UnityEngine;

namespace Explorer.Client {

	public class App : MonoBehaviour {

		[SerializeField] private List<View.View> _rootViews;

		private void Awake () {

			// Framework.Data.PersistentStore.IsLoggingEnabled = false;

			// create an initial context
			Contexts.New();

			// init all root views
			_rootViews.ForEach( view => view.Init() );
		}
	}
}