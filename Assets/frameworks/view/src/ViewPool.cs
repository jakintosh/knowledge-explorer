using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jakintosh.View {

	public interface ISubscribableDictionaryElement<TKey, TData> {

		UnityEvent<TData> OnUpdated { get; }

		TKey GetKey ();
	}

	// [Serializable]
	public class SubscribableDictionary<TKey, TModel>
		where TModel : ISubscribableDictionaryElement<TKey, TModel> {

		// events
		[JsonIgnore] public UnityEvent<TKey> OnAdded = new UnityEvent<TKey>();
		[JsonIgnore] public UnityEvent<TKey> OnUpdated = new UnityEvent<TKey>();
		[JsonIgnore] public UnityEvent<TKey> OnRemoved = new UnityEvent<TKey>();

		// crud
		public TModel Get ( TKey handle ) {

			return _data[handle];
		}
		public List<TModel> GetAll () {

			return new List<TModel>( _data.Values );
		}
		public void Register ( TKey handle, TModel data ) {

			data.OnUpdated.AddListener( HandleDataUpdated );
			_data.Add( handle, data );
			OnAdded?.Invoke( handle );
		}
		public void Unregister ( TKey handle ) {

			_data[handle]?.OnUpdated.RemoveListener( HandleDataUpdated );
			_data.Remove( handle );
			OnRemoved?.Invoke( handle );
		}

		// data
		[JsonProperty( propertyName: "data" )] private Dictionary<TKey, TModel> _data = new Dictionary<TKey, TModel>();

		// event handlers
		private void HandleDataUpdated ( TModel data )
			=> OnUpdated?.Invoke( data.GetKey() );
	}

	public class ViewPool<TView, TModel>
		where TView : BaseView
		where TModel : BaseViewModel {

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
			var handle = new ViewHandle( _nextViewHandle++ );
			( view as IViewHandleSet ).SetViewHandle( handle );
			( viewModel as IViewHandleSet ).SetViewHandle( handle );
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
		private int _lastUsedIndex = 0;
		private List<TView> _views = new List<TView>();

		private bool HasViewAvailable ()
			=> _lastUsedIndex < _views.LastIndex();
		private bool IsViewUsed ( int index )
			=> index <= _lastUsedIndex;
		private void CreateNewView () {

			var view = GameObject.Instantiate<TView>( _prefab, _container, false );
			_views.Add( view );
		}
	}

}
