using Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Explorer.View {

	public abstract class View : MonoBehaviour {

		// override to implement
		protected abstract void Init ();

		// to init other views
		protected void InitView ( View view ) {
			view.Init();
		}
		protected void InitView<TView, TData> ( TView view, TData data ) where TView : IViewInit<TData> {
			view.InitFrom( data );
		}
	}

	public interface IViewInit<T> {
		void InitFrom ( T data );
	}

	public abstract class View<T> : View, IViewInit<T> {

		void IViewInit<T>.InitFrom ( T data ) => this.InitFrom( data );

		protected abstract void InitFrom ( T data );
		public abstract T GetInitData ();
	}

	public abstract class RootView : View {

		private void Awake () {
			Init();
		}
	}


	public class Anchor {

		public UnityEvent<Float3> OnPositionChange = new UnityEvent<Float3>();

		public Float3 Position;

		private Observable<Float3> _position;

		public void Init () {

			_position = new Observable<Float3>(
				initialValue: Float3.Zero,
				onChange: position => {
					OnPositionChange?.Invoke( position );
				}
			);
		}
	}
}