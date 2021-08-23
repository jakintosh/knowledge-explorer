using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Jakintosh.Actions {

	[Serializable]
	public class History {

		// ********** Public Interface **********

		// events
		public UnityEvent OnChange = new UnityEvent();

		// properties
		public int FutureActionCount => _futureActions;
		public int PastActionCount => _history.Count - _futureActions;

		// constructor
		public History ( int size ) {

			_size = size;
		}

		// actions
		public void ExecuteAction ( IHistoryAction action ) {

			if ( action == null ) { return; }
			if ( !TryDo( action ) ) { return; }
			RecordAction( action );
		}
		public void Undo () {

			if ( _mostRecentAction == null ) { return; }
			if ( !TryUndo( _mostRecentAction.Value ) ) { return; }
			CycleBack();
		}
		public void Redo () {

			if ( _nextAction == null ) { return; }
			if ( !TryDo( _nextAction.Value ) ) { return; }
			CycleForward();
		}
		public void Flush () {

			var action = _history.First;
			while ( action != null ) {
				action.Value.Retire();
				action = action.Next;
			}
			_history.Clear();
		}

		// info
		public List<IHistoryAction> GetPastActions ( int count ) {

			// limit to num actions
			count = Math.Min( count, PastActionCount );

			var node = _mostRecentAction;
			var actions = new List<IHistoryAction>();
			for ( int i = 0; i < count; i++ ) {
				actions.Add( node.Value );
				node = node.Previous;
			}
			return actions;
		}
		public List<IHistoryAction> GetFutureActions ( int count ) {

			// limit to num actions
			count = Math.Min( count, FutureActionCount );

			var node = _nextAction;
			var actions = new List<IHistoryAction>();
			for ( int i = 0; i < count; i++ ) {
				actions.Add( node.Value );
				node = node.Next;
			}
			return actions;
		}

		// ********** Private Interface **********

		private int _size = 0;
		private int _futureActions = 0;
		private LinkedList<IHistoryAction> _history = new LinkedList<IHistoryAction>();
		private LinkedListNode<IHistoryAction> _mostRecentAction = null;
		private LinkedListNode<IHistoryAction> _nextAction = null;

		private bool TryDo ( IHistoryAction action ) {

			try {
				action.Do();
				return true;
			} catch ( DoActionFailureException historyException ) {
				UnityEngine.Debug.Log( $"Jakintosh.Actions.History: Failed to do action {action}: {historyException.Message}" );
				return false;
			} catch {
				throw; // rethrow any unexpected exception
			}
		}
		private bool TryUndo ( IHistoryAction action ) {

			try {
				action.Undo();
				return true;
			} catch ( UndoActionFailureException historyException ) {
				UnityEngine.Debug.Log( $"Jakintosh.Actions.History: Failed to undo action {action}: {historyException.Message}" );
				return false;
			} catch {
				throw; // rethrow any unexpected exception
			}
		}

		private void RecordAction ( IHistoryAction action ) {

			// retire all future actions
			var next = _mostRecentAction?.Next;
			while ( next != null ) {
				next.Value.Retire();
				next = next.Next;
			}
			// pop all future actions
			for ( int i = 0; i < _futureActions; i++ ) {
				_history.RemoveLast();
			}
			_futureActions = 0;

			// add new
			_history.AddLast( action );

			// remove oldest if at size limit
			if ( _history.Count > _size ) {
				_history.First.Value.Retire();
				_history.RemoveFirst();
			}

			// most recent action is last
			_mostRecentAction = _history.Last;
			_nextAction = null;
			OnChange?.Invoke();
		}
		private void CycleBack () {

			_futureActions++;
			_nextAction = _mostRecentAction;
			_mostRecentAction = _mostRecentAction.Previous;
			OnChange?.Invoke();
		}
		private void CycleForward () {

			_futureActions--;
			_mostRecentAction = _nextAction;
			_nextAction = _mostRecentAction?.Next;
			OnChange?.Invoke();
		}
	}
}