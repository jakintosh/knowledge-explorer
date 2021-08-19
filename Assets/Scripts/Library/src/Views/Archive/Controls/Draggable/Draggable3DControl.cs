using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Library.Views {

	public static class Drag {

		public enum Mode {
			Primary,
			Secondary
		}

		/*

			a Draggable Object D begins dragging, with a payload of type T
			Drag Manager M knows about all drag receivers R[] and what payload type they can receieve
			D notifies M that paylod P of type T is being dragged
			M notifes R[]s that can recieve T that P is in flight
			R starts listening to notifications
			 - R can see enter/exit/release, and dereference the payload and pass those on to listeners
			D notifies M that P is not being dragged
			M notifies relevant R that P is no longer in flight

		*/

		private static dynamic _activePayload;
		private static DraggableReceiver _activeReceiver;
		private static Dictionary<System.Type, List<DraggableReceiver>> _receivers = new Dictionary<System.Type, List<DraggableReceiver>>();

		public static void LaunchPayload<T> ( T payload, List<DraggableReceiver> ignoredReceivers ) {

			_activePayload = payload;
			_receivers[typeof( T )].Filter( r => !ignoredReceivers.Contains( r ) ).ForEach( r => r.EnableReception() );
			// Debug.Log( $"DragManager: Payload of type {typeof( T ).ToString()} in flight" );
		}
		public static void DropPayload<T> ( T payload ) {

			if ( _activePayload != payload ) {
				Debug.LogError( "DragManager: Trying to drop a payload that isn't active." );
				return;
			}
			// Debug.Log( $"DragManager: Payload of type {typeof( T ).ToString()} dropped" );
			_activeReceiver?.ReceivePayload( _activePayload );
			_activePayload = null;
			_receivers[typeof( T )].ForEach( r => r.DisableReception() );
		}

		public static void ReceiverCanAccept ( DraggableReceiver receiver ) {

			_activeReceiver = receiver;
			// Debug.Log( $"DragManager: Receiver {receiver.name} is ready to accept payload" );
		}
		public static void ReceiverCannotAccept ( DraggableReceiver receiver ) {

			if ( _activeReceiver == receiver ) {
				_activeReceiver = null;
				// Debug.Log( $"DragManager: Receiver {receiver.name} is not ready to accept payload" );
			}
		}

		public static void Subscribe ( DraggableReceiver receiver, IEnumerable<System.Type> receivableTypes ) {

			foreach ( var type in receivableTypes ) {
				_receivers.EnsureValue( key: type, () => new List<DraggableReceiver>() );
				_receivers[type].Add( receiver );
				// Debug.Log( $"DragManager: Receiver {receiver.name} registered to handle {type} payloads" );
			}
		}
		public static void Unsubscribe ( DraggableReceiver receiver, IEnumerable<System.Type> receivableTypes ) {

			foreach ( var type in receivableTypes ) {
				_receivers.EnsureValue( key: type, () => new List<DraggableReceiver>() );
				_receivers[type].Remove( receiver );
				// Debug.Log( $"DragManager: Receiver {receiver.name} unregistered to handle {type} payloads" );
			}
		}
	}

	[RequireComponent( typeof( Collider ) )]
	public class Draggable3DControl : MonoBehaviour,
		IPointerDownHandler,
		IDragHandler,
		IPointerUpHandler {

		// ********** Public Interface **********


		public struct DragEventData {
			public Plane Plane;
			public Vector3 Delta;
			public Drag.Mode Mode;
			public DragEventData ( Vector3 delta, Drag.Mode mode, Plane plane ) {
				Delta = delta;
				Mode = mode;
				Plane = plane;
			}
		}

		public void AddPayload ( Drag.Mode mode, dynamic payload ) => _payloads[mode] = payload;
		public void ClearPayloads () => _payloads.Clear();
		public void IgnoreReceivers ( params DraggableReceiver[] receivers ) => _ignoredReceivers.AddRange( receivers );
		public void ClearIgnoredReceivers () => _ignoredReceivers.Clear();

		// events
		public UnityEvent<DragEventData> OnDragBegin = new UnityEvent<DragEventData>();
		public UnityEvent<DragEventData> OnDragEnd = new UnityEvent<DragEventData>();
		public UnityEvent<DragEventData> OnDragDelta = new UnityEvent<DragEventData>();


		// ********** IPointer Event Handlers **********

		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {

			var contactPoint = GetContactPoint( eventData );
			_lastPosition = contactPoint;

			OnDragBegin?.Invoke( GetEventData( eventData ) );
		}
		void IDragHandler.OnDrag ( PointerEventData eventData ) {

			if ( _isDragging == false ) {
				_isDragging = true;
				var mode = GetModeFromEventData( eventData );
				if ( _payloads.TryGetValue( mode, out var payload ) ) {
					if ( payload != null ) {
						_payloadInFlight = payload;
						// Debug.Log( $"DraggableControl: Draggable {name} launched payload of type {_payloadInFlight.GetType().ToString()}", gameObject );
						Drag.LaunchPayload( _payloadInFlight, _ignoredReceivers );
					}
				}
			}

			var contactPoint = GetContactPoint( eventData );
			var delta = contactPoint - _lastPosition;
			_lastPosition = contactPoint;

			OnDragDelta?.Invoke( GetEventData( eventData, delta ) );
		}
		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {

			if ( _isDragging ) {
				_isDragging = false;
				if ( _payloadInFlight != null ) {
					// Debug.Log( $"DraggableControl: Draggable {name} dropped payload of type {_payloadInFlight.GetType().ToString()}", gameObject );
					Drag.DropPayload( _payloadInFlight );
					_payloadInFlight = null;
				}
			}
			OnDragEnd?.Invoke( GetEventData( eventData ) );
		}


		// ********** Private Interface **********

		// payload
		private Dictionary<Drag.Mode, dynamic> _payloads = new Dictionary<Drag.Mode, dynamic>();
		private List<DraggableReceiver> _ignoredReceivers = new List<DraggableReceiver>();
		private dynamic _payloadInFlight = null;

		// data
		private Vector3 _lastPosition = Vector3.zero;
		private bool _isDragging = false;

		// private functions
		private DragEventData GetEventData ( PointerEventData eventData, Vector3? delta = null ) =>
			new DragEventData(
				delta: delta.HasValue ? delta.Value : Vector3.zero,
				mode: GetModeFromEventData( eventData ),
				plane: GetPlane()
			);

		private Drag.Mode GetModeFromEventData ( PointerEventData eventData ) =>
			eventData.button == PointerEventData.InputButton.Left ? Drag.Mode.Primary : Drag.Mode.Secondary;

		private Plane GetPlane () =>
			new Plane( inNormal: -transform.forward, inPoint: transform.position );

		private Ray GetRay ( PointerEventData eventData ) =>
			Camera.main.ScreenPointToRay( eventData.position );

		private Vector3 GetContactPoint ( PointerEventData eventData ) {

			var ray = GetRay( eventData );
			GetPlane().Raycast( ray, out float distance );
			return ray.origin + ( ray.direction * distance );
		}
	}

}