using Jakintosh.Actions;
using Jakintosh.View;
using TMPro;
using UnityEngine;

namespace Library.Views {

	public class ActionHistory : ReuseableView<History> {

		// ********** View Implementation **********

		public override History GetState ()
			=> _history;
		protected override void OnInitialize () {

			_actionViews = new Action[_maxActions];
			for ( int i = 0; i < _maxActions; i++ ) {
				_actionViews[i] = Instantiate<Action>( _actionPrefab, _undoContainer, false );
				_actionViews[i].gameObject.SetActive( false );
			}
		}
		protected override void OnPopulate ( History history ) {

			_history = history;
			ReloadView();
			_history.OnChange.AddListener( ReloadView );
		}
		protected override void OnRecycle () {

			_history.OnChange.RemoveListener( ReloadView );
		}
		protected override void OnCleanup () { }

		// ********** Public Interface **********


		// ********** Private Interface **********

		[Header( "UI Config" )]
		[SerializeField] private int _maxActions;

		[Header( "UI Display" )]
		[SerializeField] private GameObject _marker;
		[SerializeField] private GameObject _historySection;
		[SerializeField] private GameObject _futureSection;
		[SerializeField] private GameObject _undoIndicator;
		[SerializeField] private GameObject _redoIndicator;
		[SerializeField] private Transform _undoContainer;
		[SerializeField] private Transform _redoContainer;
		[SerializeField] private GameObject _undoMoreIndicator;
		[SerializeField] private GameObject _redoMoreIndicator;
		[SerializeField] private TextMeshProUGUI _undoMoreCount;
		[SerializeField] private TextMeshProUGUI _redoMoreCount;
		[SerializeField] private GameObject _emptyText;

		[Header( "UI Assets" )]
		[SerializeField] private Action _actionPrefab;

		// data
		private History _history;
		private Action[] _actionViews;

		// functions
		private void ReloadView () {

			// determine counts
			var visibleFutureActions = Mathf.Min( _maxActions / 2, _history.FutureActionCount );
			var visiblePastActions = Mathf.Min( _maxActions - visibleFutureActions, _history.PastActionCount );
			var nonvisibleFutureActions = _history.FutureActionCount - visibleFutureActions;
			var nonvisiblePastActions = _history.PastActionCount - visiblePastActions;
			var isEmpty = visibleFutureActions + visiblePastActions == 0;

			// set container visibility
			_marker.SetActive( !isEmpty );
			_emptyText.SetActive( isEmpty );
			_undoIndicator.SetActive( visiblePastActions > 0 );
			_historySection.SetActive( visiblePastActions > 0 );
			_redoIndicator.SetActive( visibleFutureActions > 0 );
			_futureSection.SetActive( visibleFutureActions > 0 );
			_undoMoreIndicator.SetActive( nonvisiblePastActions > 0 );
			_redoMoreIndicator.SetActive( nonvisibleFutureActions > 0 );
			_undoMoreCount.text = $"+{nonvisiblePastActions}";
			_redoMoreCount.text = $"+{nonvisibleFutureActions}";

			// set view actions
			var index = 0;
			_history.GetPastActions( visiblePastActions ).ForEach( action => {
				var view = _actionViews[index++];
				view.transform.SetParent( _undoContainer );
				view.transform.SetSiblingIndex( index );
				view.gameObject.SetActive( true );
				view.InitWith( action );
			} );
			_history.GetFutureActions( visibleFutureActions ).ForEach( action => {
				var view = _actionViews[index++];
				view.transform.SetParent( _redoContainer );
				view.transform.SetSiblingIndex( index - visiblePastActions );
				view.gameObject.SetActive( true );
				view.InitWith( action );
			} );
			while ( index < _maxActions ) {
				_actionViews[index++].gameObject.SetActive( false );
			}
		}
	}

}
