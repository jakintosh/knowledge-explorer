using UnityEngine;

namespace Explorer.View {

	public abstract class View : MonoBehaviour {

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

	public abstract class ReuseableView<T> : MonoBehaviour {

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


	// public class Anchor {

	// 	public UnityEvent<Float3> OnPositionChange = new UnityEvent<Float3>();

	// 	public Float3 Position;

	// 	private Observable<Float3> _position;

	// 	public void Init () {

	// 		_position = new Observable<Float3>(
	// 			initialValue: Float3.Zero,
	// 			onChange: position => {
	// 				OnPositionChange?.Invoke( position );
	// 			}
	// 		);
	// 	}
	// }
}