using Jakintosh.Subscribable;
using System;

using Workspace = Library.ViewModel.Workspace;

namespace Library {

	// [Serializable]
	public class State {

		public Subscribable<string> ActiveWorkspaceUID;
		public Subscribable<Workspace> ActiveWorkspace;

		public State () {

			ActiveWorkspace = new Subscribable<Workspace>(
				initialValue: null,
				onChange: workspace => {
					ActiveWorkspaceUID?.Set( workspace?.UID );
				}
			);
			ActiveWorkspaceUID = new Subscribable<string>(
				initialValue: null,
				onChange: uid => {
					ActiveWorkspace?.Set( App.Workspaces.Get( uid ) );
				}
			);
		}
	}
}
