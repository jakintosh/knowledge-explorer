using Jakintosh.Observable;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Library.Views {

	[RequireComponent( typeof( Collider ) )]
	public class DraggableReceiver : MonoBehaviour,
		IPointerEnterHandler,
		IPointerExitHandler {

		public UnityEvent<bool> OnReceiving = new UnityEvent<bool>();
		public UnityEvent<bool> OnHover = new UnityEvent<bool>();
		public UnityEvent<dynamic> OnReceivedPayload = new UnityEvent<dynamic>();

		public void SetReceivableTypes ( params System.Type[] types ) {

			Drag.Unsubscribe( this, _receivableTypes );
			_receivableTypes.Clear();
			_receivableTypes.UnionWith( types );
			Drag.Subscribe( this, _receivableTypes );
		}

		public void EnableReception () => _isReceiving.Set( true );
		public void DisableReception () => _isReceiving.Set( false );
		public void ReceivePayload<T> ( T payload ) {

			// Debug.Log( $"DraggableReceiver: Receiver {name} received {typeof( T ).ToString()} payload", gameObject );
			Drag.ReceiverCannotAccept( this );
			OnReceivedPayload?.Invoke( payload );
			OnHover?.Invoke( false );
		}


		// ********** IPointer Event Handlers **********

		void IPointerEnterHandler.OnPointerEnter ( PointerEventData eventData ) {

			if ( !_isReceiving.Get() ) { return; }
			Drag.ReceiverCanAccept( this );
			OnHover?.Invoke( true );
		}
		void IPointerExitHandler.OnPointerExit ( PointerEventData eventData ) {

			if ( !_isReceiving.Get() ) { return; }
			Drag.ReceiverCannotAccept( this );
			OnHover?.Invoke( false );
		}

		// observables
		private Observable<bool> _isReceiving;

		// data
		private HashSet<System.Type> _receivableTypes = new HashSet<System.Type>();

		private void Awake () {

			// init observables
			_isReceiving = new Observable<bool>(
				initialValue: false,
				onChange: isReceiving => {
					OnReceiving?.Invoke( isReceiving );
				}
			);

			Drag.Subscribe( this, _receivableTypes );
		}
		private void OnDestroy () {

			Drag.Unsubscribe( this, _receivableTypes );
		}
	}
}