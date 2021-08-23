using System;
using UnityEngine;

namespace Jakintosh.View {


	public static class ViewHandles {

		private static int handle = 1;
		public static int Generate () => handle++;
	}

	public abstract class View : MonoBehaviour, IIdentifiableLink<int> {

		public int LinkedIdentifier { get; private set; }
		public void Link ( IIdentifiable<int> viewModel ) {
			LinkedIdentifier = viewModel.Identifier;
		}

		public void Init () {

			if ( !_isInitialized ) {
				OnInitialize();
				_isInitialized = true;
			} else {
				Debug.LogWarning( $"View.Init(): Object \"{name}\" tried to init more than once" );
			}
		}

		private void OnApplicationQuit () {

			_isQuitting = true;
		}
		private void OnDestroy () {

			if ( _isInitialized && !_isQuitting ) {
				OnCleanup();
			}
		}

		protected abstract void OnInitialize ();
		protected abstract void OnCleanup ();

		private bool _isInitialized;
		private bool _isQuitting;
	}

	public abstract class ReuseableView<T> : MonoBehaviour, IIdentifiableLink<int> {

		public int LinkedIdentifier { get; private set; }
		public void Link ( IIdentifiable<int> viewModel ) {
			LinkedIdentifier = viewModel.Identifier;
		}


		public void InitWith ( T data ) {

			if ( !_isInitialized ) {
				OnInitialize();
				_isInitialized = true;
			} else {
				OnRecycle();
			}
			OnPopulate( data );
		}
		public abstract T GetState ();

		private void OnApplicationQuit () {

			_isQuitting = true;
		}
		private void OnDestroy () {

			if ( _isInitialized && !_isQuitting ) {
				OnRecycle();
				OnCleanup();
			}
		}

		protected abstract void OnInitialize ();
		protected abstract void OnPopulate ( T data );
		protected abstract void OnRecycle ();
		protected abstract void OnCleanup ();

		private bool _isInitialized;
		private bool _isQuitting;
	}
}