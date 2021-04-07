using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	[RequireComponent( typeof( Graphic ) )]
	public class DraggableControl : MonoBehaviour,
		IPointerDownHandler,
		IDragHandler,
		IPointerUpHandler {


		// ********** Public Interface **********

		// events
		public UnityEvent OnDragBegin = new UnityEvent();
		public UnityEvent OnDragEnd = new UnityEvent();
		public UnityEvent<Vector3> OnDragDelta = new UnityEvent<Vector3>();


		// ********** IPointer Event Handlers **********

		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {

			var contactPoint = GetContactPoint( eventData );
			_lastPosition = contactPoint;

			OnDragBegin?.Invoke();
		}
		void IDragHandler.OnDrag ( PointerEventData eventData ) {

			var contactPoint = GetContactPoint( eventData );
			var delta = contactPoint - _lastPosition;
			_lastPosition = contactPoint;

			OnDragDelta?.Invoke( delta );
		}
		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {

			OnDragEnd?.Invoke();
		}


		// ********** Private Interface **********

		// data
		private Vector3 _lastPosition = Vector3.zero;

		// private functions
		private Vector3 GetContactPoint ( PointerEventData eventData ) {

			var ray = Camera.main.ScreenPointToRay( eventData.position );
			var plane = new Plane( inNormal: -transform.forward, inPoint: transform.position );
			plane.Raycast( ray, out float distance );
			return ray.origin + ( ray.direction * distance );
		}
	}

}