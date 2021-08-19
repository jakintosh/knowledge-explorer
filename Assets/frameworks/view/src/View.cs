using System;
using UnityEngine;

namespace Jakintosh.View {

	public interface IViewHandle {
		ViewHandle GetViewHandle ();
	}
	internal interface IViewHandleSet {
		void SetViewHandle ( ViewHandle handle );
	}

	public struct ViewHandle : IEquatable<ViewHandle> {

		private int _handle;
		public bool IsEmpty ()
			=> _handle == 0;
		public static ViewHandle Empty
			=> new ViewHandle( 0 );
		public ViewHandle ( int handle ) {
			_handle = handle;
		}

		public bool Equals ( ViewHandle other )
			=> _handle.Equals( other._handle );
		public override bool Equals ( object obj )
			=> ( obj is ViewHandle ) ? this.Equals( (ViewHandle)obj ) : false;
		public override int GetHashCode ()
			=> -558203912 + _handle.GetHashCode();
	}

	public abstract class BaseViewModel : IViewHandle {

		static int handle = 1;
		public BaseViewModel () {
			_viewHandle = new ViewHandle( handle++ );
		}

		private ViewHandle _viewHandle = ViewHandle.Empty;
		public ViewHandle GetViewHandle () => _viewHandle;
	}

	public abstract class BaseView : MonoBehaviour,
		IViewHandle,
		IViewHandleSet {

		private ViewHandle _viewHandle = ViewHandle.Empty;
		public ViewHandle GetViewHandle () => _viewHandle;
		void IViewHandleSet.SetViewHandle ( ViewHandle handle ) => _viewHandle = handle;
	}

	public abstract class View : BaseView {

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

	public abstract class ReuseableView<T> : BaseView {

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