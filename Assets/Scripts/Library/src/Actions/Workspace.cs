using Jakintosh.Actions;
using Jakintosh.Resources;
using System;

namespace Library.Actions.Workspace {

	[Serializable]
	public class Open : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Open Workspace";
		string IHistoryAction.GetDescription () {
			return _closedName != null ?
				$"Closed \"{_closedName}\", and opened \"{_openedName}\"." :
				$"Opened \"{_openedName}\"";
		}

		void IHistoryAction.Do () {

			if ( _openedUID == null ) { throw new System.Exception( "Actions.Workspace.Open.Do: Tried to open null workspace." ); }

			// save uid for closed workspce
			_closedUID = App.State.ActiveWorkspaceUID.Get();

			// set new workspace as active
			App.State.ActiveWorkspaceUID.Set( _openedUID );

			// save names
			_closedName ??= App.Workspaces.Get( _closedUID )?.Name;
			_openedName ??= App.Workspaces.Get( _openedUID )?.Name;
		}
		void IHistoryAction.Undo () {

			// revert to prev workspace
			App.State.ActiveWorkspaceUID.Set( _closedUID );
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private string _openedName;
		private string _closedName;
		private string _openedUID;
		private string _closedUID;

		public Open ( string uid ) {

			_openedUID = uid;
		}
	}

	[Serializable]
	public class Close : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Close Workspace";
		string IHistoryAction.GetDescription () => $"Closed \"{_prevName}\"";

		void IHistoryAction.Do () {

			if ( App.State.ActiveWorkspace.Get() == null ) {
				throw new System.Exception( "Actions.Workspace.Close.Do: No open workspace to close." );
			}

			// save prev open workspce
			_prevUid = App.State.ActiveWorkspaceUID.Get();
			_prevName = App.Workspaces.Get( _prevUid ).Name;

			// close workspace
			App.State.ActiveWorkspace.Set( null );
		}
		void IHistoryAction.Undo () {

			// revert to prev workspace
			App.State.ActiveWorkspaceUID.Set( _prevUid );
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private string _prevUid;
		private string _prevName;

		public Close () { }
	}

	[Serializable]
	public class Create : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Create Workspace";
		string IHistoryAction.GetDescription () => $"Created new workspace named \"{_name}\"{( _shouldOpen ? ", and opened it" : "" )}.";

		void IHistoryAction.Do () {

			// create if not existant yet
			if ( _metadata == null ) {
				(_metadata, _workspace) = App.Workspaces.Create( _name );
				if ( _metadata == null ) { throw new System.Exception( "Actions.Workspace.CreateAndOpen.Do was not successful" ); }
			}
			App.Workspaces.Insert( _metadata, _workspace );

			// if should open, open
			if ( _shouldOpen ) {
				_openAction ??= new Open( _metadata.UID );
				_openAction.Do();
			}
		}
		void IHistoryAction.Undo () {

			// if has open action, undo
			_openAction?.Undo();

			// delete new workspace
			var success = App.Workspaces.Delete( _metadata.UID );
			if ( !success ) { throw new System.Exception( "Actions.Workspace.CreateAndOpen.Undo was not successful" ); }
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private string _name;
		private Metadata _metadata;
		private ViewModel.Workspace _workspace;

		private bool _shouldOpen;
		private IHistoryAction _openAction;

		public Create ( string name, bool shouldOpen ) {

			_name = name;
			_shouldOpen = shouldOpen;
		}
	}

	[Serializable]
	public class Rename : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Rename Workspace";
		string IHistoryAction.GetDescription () => $"Renamed workspace named \"{_oldName}\" to \"{_newName}\".";

		void IHistoryAction.Do () {

			var success = App.Workspaces.Rename( _uid, _newName );
			if ( !success ) {
				throw new DoActionFailureException( "Actions.Workspace.Rename.Do was not successful" );
			}
		}
		void IHistoryAction.Undo () {

			var success = App.Workspaces.Rename( _uid, _oldName );
			if ( !success ) {
				throw new UndoActionFailureException( "Actions.Workspace.Rename.Undo was not successful" );
			}
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private string _uid;
		private string _oldName;
		private string _newName;

		public Rename ( string uid, string name ) {

			// save data
			_uid = uid;
			_oldName = App.Workspaces.Get( uid )?.Name;
			_newName = name;
		}
	}

	[Serializable]
	public class Delete : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Delete Workspace";
		string IHistoryAction.GetDescription () => $"Deleted workspace named \"{_workspace.Name}\".";

		void IHistoryAction.Do () {

			// if deleted workspace is open and should close, close it
			var deletedIsOpen = _uid == App.State.ActiveWorkspaceUID.Get();
			if ( deletedIsOpen && _shouldClose ) {
				_closeAction ??= new Close();
				_closeAction.Do();
			}

			_metadata = App.Workspaces.GetMetadata( _uid );
			_workspace = App.Workspaces.Get( _uid );

			var success = App.Workspaces.Delete( _uid );
			if ( !success ) { throw new System.Exception( "Actions.Workspace.Delete.Do was not successful" ); }

		}
		void IHistoryAction.Undo () {

			// reinsert
			var success = App.Workspaces.Insert( _metadata, _workspace );
			if ( !success ) { throw new System.Exception( "Actions.Workspace.Create.Undo was not successful" ); }

			// undo close, if exists
			_closeAction?.Undo();
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private string _uid;
		private Metadata _metadata;
		private ViewModel.Workspace _workspace;

		private bool _shouldClose;
		private IHistoryAction _closeAction;

		public Delete ( string uid, bool shouldClose ) {

			// save data
			_uid = uid;
			_shouldClose = shouldClose;
		}
	}
}
