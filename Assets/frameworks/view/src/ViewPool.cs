using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jakintosh.View {

	public class ViewPool<TView, TModel>
		where TView : MonoBehaviour, IIdentifiableLink<int>
		where TModel : IIdentifiable<int> {

		public ViewPool ( TView prefab, Transform container, Action<TView, TModel> setup, Action<TView> teardown ) {

			_prefab = prefab;
			_container = container ?? new GameObject( $"{_prefab.name} Container" ).transform;
			_setup = setup;
			_teardown = teardown;
		}
		public TView GetView ( TModel viewModel ) {

			// create new if necessary
			if ( !HasViewAvailable() ) {
				CreateNewView();
			}

			// setup and return
			var view = _views[++_lastUsedIndex];
			_setup( view, viewModel );

			var handle = ( viewModel as IIdentifiable<int> ).Identifier;
			( view as IIdentifiableLink<int> ).Link( viewModel );

			return view;
		}
		public void ReleaseView ( TView instance ) {

			// get index of view
			var index = _views.IndexOf( instance );
			if ( !IsViewUsed( index ) ) { return; } // this view is not in use

			// swap released view to outside of pool
			var lastInstance = _views[_lastUsedIndex];
			_views[index] = lastInstance;
			_views[_lastUsedIndex] = instance;
			_lastUsedIndex--;

			// run teardown logic
			_teardown( instance );
		}

		private TView _prefab;
		private Transform _container;
		private Action<TView, TModel> _setup;
		private Action<TView> _teardown;

		private int _nextViewHandle = 1;
		private int _lastUsedIndex = -1;
		private List<TView> _views = new List<TView>();

		private bool HasViewAvailable ()
			=> _views.Count > 0 && _lastUsedIndex < _views.LastIndex();
		private bool IsViewUsed ( int index )
			=> index <= _lastUsedIndex;
		private void CreateNewView () {

			var view = GameObject.Instantiate<TView>( _prefab, _container, false );
			_views.Add( view );
		}
	}

}
