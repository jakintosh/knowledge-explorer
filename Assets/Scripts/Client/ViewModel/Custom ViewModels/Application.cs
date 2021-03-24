using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Application {

		// subviews
		[SerializeField] public Workspaces Workspaces = null;

		// runtime data
		public Application ( Model.IWorkspaceAPI workspaceModel ) {

			Workspaces = new Workspaces( workspaceModel );
		}
	}

}