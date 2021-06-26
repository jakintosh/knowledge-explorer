using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Jakintosh.List {

	public abstract class Cell<TData> : MonoBehaviour {

		// events
		[HideInInspector] public UnityEvent<TData> OnClick = new UnityEvent<TData>();

		// methods
		public void SetData ( TData data ) {

			_data = data;
			ReceiveData( _data );
		}

		protected virtual void Awake () {

			_button?.onClick.AddListener( () => {
				OnClick?.Invoke( _data );
			} );
		}
		protected abstract void ReceiveData ( TData data );

		[SerializeField] protected Button _button;

		protected TData _data;
	}

}