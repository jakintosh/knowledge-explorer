using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace View {


	public interface IPositionChangeHandler {
		void PositionChanged ();
	}

	public class DragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler {

		void IPointerDownHandler.OnPointerDown ( PointerEventData eventData ) {

			if ( _graphic != null ) {
				_color = _graphic.color;
				_graphic.color = _color + new Color( 0.1f, 0.1f, 0.1f );
			}

			var ray = Camera.main.ScreenPointToRay( eventData.position );
			_plane.Raycast( ray, out float distance );
			_offset = ( ray.origin + ( ray.direction * distance ) ) - _root.transform.position;
		}

		void IDragHandler.OnDrag ( PointerEventData eventData ) {

			var ray = Camera.main.ScreenPointToRay( eventData.position );
			_plane.Raycast( ray, out float distance );
			_root.transform.position = ray.origin + ( ray.direction * distance ) - _offset;
		}

		void IPointerUpHandler.OnPointerUp ( PointerEventData eventData ) {

			if ( _graphic != null ) { _graphic.color = _color; }

			var handler = _root.GetComponent<IPositionChangeHandler>();
			if ( handler != null ) {
				handler.PositionChanged();
			} else {
				Debug.LogWarning( $"DragHandle: Tried to send position change event but no handler." );
			}
		}


		[SerializeField] private GameObject _root;
		[SerializeField] private Graphic _graphic;

		private Vector3 _offset;
		private Color _color;
		private Plane _plane => new Plane( inNormal: -transform.forward, inPoint: transform.position );
	}
}
