using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Client.View {

	[RequireComponent( typeof( Graphic ) )]
	public class DraggableControl : MonoBehaviour,
		IPointerDownHandler,
		IDragHandler,
		IPointerUpHandler {


		// ********** Public Interface **********

		// events
		public event Framework.Event.Signature OnDragBegin;
		public event Framework.Event.Signature OnDragEnd;
		public event Framework.Event<Vector3>.Signature OnDragDelta;


		// ********** IPointer Event Handlers **********

		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {

			// calculations
			var contactPoint = GetContactPoint( eventData );
			_lastPosition = contactPoint;

			// fire events
			Framework.Event.Fire(
				@event: OnDragBegin,
				id: "DraggableGraphic.OnDragBegin"
			);
		}
		void IDragHandler.OnDrag ( PointerEventData eventData ) {

			// calculations
			var contactPoint = GetContactPoint( eventData );
			var delta = contactPoint - _lastPosition;
			_lastPosition = contactPoint;

			// update model
			// if ( _frameModel != null ) {
			// 	var pos = _frameModel.Position.Get();
			// 	_frameModel.Position.Set( pos + delta );
			// }

			// fire events
			Framework.Event<Vector3>.Fire(
				@event: OnDragDelta,
				value: delta,
				id: "DraggableGraphic.OnDragDelta",
				priority: Framework.EventLogPriorities.Verbose
			);
		}
		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {

			// fire events
			Framework.Event.Fire(
				@event: OnDragEnd,
				id: "DraggableGraphic.OnDragEnd"
			);
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