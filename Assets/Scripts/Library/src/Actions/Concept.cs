using Jakintosh.Actions;
using Jakintosh.Data;
using System;
using System.Collections.Generic;

using ConceptViewModel = Library.ViewModel.Concept;

namespace Library.Actions.Concept {

	[Serializable]
	public class Open : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Open Concept";
		string IHistoryAction.GetDescription () => $"Open concept.";

		void IHistoryAction.Do () {

			if ( _uid == null ) { throw new System.Exception( "Actions.Workspace.Open.Do: Tried to open concept with null uid." ); }

			// create concept if it doesn't exist
			if ( _viewModel == null ) {
				_viewModel = ConceptViewModel.Default( _uid );
				_viewHandle = _viewModel.Identifier;
			}

			// open concept
			App.State.ActiveWorkspace.Get()?.GraphViewport.Concepts.Register( _viewModel );
		}
		void IHistoryAction.Undo () {

			// close concept
			App.State.ActiveWorkspace.Get()?.GraphViewport.Concepts.Unregister( _viewHandle );
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private string _uid;
		private int _viewHandle;
		private ConceptViewModel _viewModel;

		public Open ( string uid ) {

			_uid = uid;
		}
	}

	[Serializable]
	public class Close : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Close Concept";
		string IHistoryAction.GetDescription () => $"Close concept.";

		void IHistoryAction.Do () {

			if ( _viewHandle == 0 ) { throw new System.Exception( "Actions.Workspace.Close.Do: Tried to close concept with empty view handle." ); }

			_viewModel = App.State.ActiveWorkspace.Get()?.GraphViewport.Concepts.Get( _viewHandle );
			App.State.ActiveWorkspace.Get()?.GraphViewport.Concepts.Unregister( _viewHandle );
		}
		void IHistoryAction.Undo () {

			App.State.ActiveWorkspace.Get()?.GraphViewport.Concepts.Register( _viewModel );
		}
		void IHistoryAction.Retire () { }

		// ********** Data **********

		private int _viewHandle;
		private ConceptViewModel _viewModel;

		public Close ( int viewHandle ) {

			_viewHandle = viewHandle;
		}
	}

	[Serializable]
	public class Create : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Create Concept";
		string IHistoryAction.GetDescription () => $"Created new concept.";

		void IHistoryAction.Do () {

			// make sure concept exists
			if ( _uid == null ) {
				_uid = App.Graphs.Default.CreateConcept();
				_address = App.Data.Nodes.New();
			} else {
				_markedForDeletion = false;
				App.Graphs.Default.MarkConceptInvalid( _uid, _markedForDeletion );
				App.Data.Nodes.Revalidate( _address );
			}

			// open concept
			if ( _shouldOpen ) {
				_openAction ??= new Open( _uid );
				_openAction.Do();
			}
		}
		void IHistoryAction.Undo () {

			// undo open, if necessary
			_openAction?.Undo();

			// mark concept for deletion
			_markedForDeletion = true;
			App.Graphs.Default.MarkConceptInvalid( _uid, _markedForDeletion );
			App.Data.Nodes.Invalidate( _address );
		}
		void IHistoryAction.Retire () {

			if ( _markedForDeletion ) {
				App.Graphs.Default.DeleteConcept( _uid );
				App.Data.Nodes.Drop( _address );
			}
		}

		// ********** Data **********

		private string _uid;
		private MutableAddress _address;
		private bool _markedForDeletion;
		private bool _shouldOpen;
		private IHistoryAction _openAction;

		public Create ( bool shouldOpen ) {

			_shouldOpen = shouldOpen;
		}
	}

	[Serializable]
	public class Delete : IHistoryAction {

		// ********** IHistoryAction **********

		string IHistoryAction.GetName () => $"Delete Concept";
		string IHistoryAction.GetDescription () => $"Deleted concept.";

		void IHistoryAction.Do () {

			// mark concept for deletion
			_markedForDeletion = true;
			App.Graphs.Default.MarkConceptInvalid( _uid, _markedForDeletion );
			App.Data.Nodes.Invalidate( _address );

			// close all views
			if ( _shouldClose ) {
				_closeActions ??= App.State.ActiveWorkspace.Get()?.GraphViewport.Concepts
					.GetAll()
					.Filter( concept => concept.NodeUID == _uid )
					.Convert( concept => new Close( concept.Identifier ) as IHistoryAction );
				_closeActions?.ForEach( action => action.Do() );
			}
		}
		void IHistoryAction.Undo () {

			// undo close, if necessary
			if ( _shouldClose ) {
				_closeActions?.ForEach( action => action.Undo() );
			}

			// unmark concept for deletion
			_markedForDeletion = false;
			App.Graphs.Default.MarkConceptInvalid( _uid, _markedForDeletion );
			App.Data.Nodes.Revalidate( _address );
		}
		void IHistoryAction.Retire () {

			// commit deletion when this action is retired
			if ( _markedForDeletion ) {
				App.Graphs.Default.DeleteConcept( _uid );
				App.Data.Nodes.Drop( _address );
			}
		}

		// ********** Data **********

		private string _uid;
		private MutableAddress _address;
		private bool _markedForDeletion;
		private bool _shouldClose;
		private List<IHistoryAction> _closeActions;

		public Delete ( string uid, bool shouldClose ) {

			_uid = uid;
			_shouldClose = shouldClose;
		}
	}
}