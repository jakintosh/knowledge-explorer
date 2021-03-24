using UnityEngine;
using UnityEngine.UI;

namespace Framework.UI {

	public abstract class Cell<TData> : MonoBehaviour {

		// events
		[Header( "Cell - UI Control" )]
		public Event<TData>.Signature OnClick;

		// methods
		public void SetData ( TData data ) {

			_data = data;
			ReceiveData( _data );
		}

		protected virtual void Awake () {

			_button?.onClick.AddListener( () => {
				Event<TData>.Fire(
					@event: OnClick,
					value: _data,
					id: "Framework.UI.Cell.OnClick"
				);
			} );
		}
		protected abstract void ReceiveData ( TData data );

		[SerializeField] protected Button _button;

		protected TData _data;
	}

}