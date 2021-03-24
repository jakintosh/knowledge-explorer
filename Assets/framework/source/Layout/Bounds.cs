using System;
using UnityEngine;

namespace Framework.Layout {


	[Serializable]
	public enum ScaleContext {
		Global,
		Local
	}

	[Serializable]
	public struct EdgeDelta {

		public float Left;
		public float Right;
		public float Top;
		public float Bottom;
		public float Front;
		public float Back;

		public float WidthDelta => Left + Right;
		public float HeightDelta => Top + Bottom;
		public float DepthDelta => Front + Back;
	}

	public class Bounds : MonoBehaviour {

		// public Vector3 AnchorMin = Vector3.zero;
		// public Vector3 AnchorMax = Vector3.zero;

		[Header( "Sizing" )]
		public Vector3 PreferredSize = Vector3.one;
		public Vector3 Pivot = new Vector3( 0.5f, 0.5f, 0.5f );
		public EdgeDelta Margin;
		public EdgeDelta Padding;
		// public Vector3 MinimumSize = Vector3.zero;

		[Header( "Scale Units" )]
		public ScaleContext ScaleContext;
		public ScaleUnits LocalScaleUnit;


		// backing data
		private Vector3 _size;
		private Vector3 _center;

		private Vector3 _marginSize;
		private Vector3 _marginCenter;

		private Vector3 _paddingSize;
		private Vector3 _paddingCenter;

		private void Update () {

			CalculateBounds();
		}

		private void CalculateBounds () {

			// bounds
			_size = PreferredSize;
			_center = transform.position;
			_center -= new Vector3(
				x: ( Pivot.x - 0.5f ) * PreferredSize.x,
				y: ( Pivot.y - 0.5f ) * PreferredSize.y,
				z: ( Pivot.z - 0.5f ) * PreferredSize.z
			);

			// margin bounds
			_marginSize = _size;
			_marginSize.x -= Margin.WidthDelta;
			_marginSize.y -= Margin.HeightDelta;
			_marginSize.z -= Margin.DepthDelta;
			_marginCenter = _center;
			_marginCenter.x += ( Margin.Left - Margin.Right ) / 2f;
			_marginCenter.y += ( Margin.Bottom - Margin.Top ) / 2f;
			_marginCenter.z += ( Margin.Front - Margin.Back ) / 2f;

			// padding bounds
			_paddingSize = _size;
			_paddingSize.x += Padding.WidthDelta;
			_paddingSize.y += Padding.HeightDelta;
			_paddingSize.z += Padding.DepthDelta;
			_paddingCenter = _center;
			_paddingCenter.x -= ( Padding.Left - Padding.Right ) / 2f;
			_paddingCenter.y -= ( Padding.Bottom - Padding.Top ) / 2f;
			_paddingCenter.z -= ( Padding.Front - Padding.Back ) / 2f;
		}
		private float GetScale ( ScaleContext scaleContext ) {

			return scaleContext switch {
				ScaleContext.Global => Global.Instance.ScaleUnit.MetersPerUnit(),
				ScaleContext.Local => LocalScaleUnit.MetersPerUnit(),
				_ => 1f
			};
		}


		// visualization
		private void OnDrawGizmos () {

			var color = Gizmos.color;

			CalculateBounds();
			var displayScale = GetScale( this.ScaleContext );

			Gizmos.color = Color.red;
			Gizmos.DrawWireCube( _marginCenter, _marginSize * displayScale );

			Gizmos.color = Color.green;
			Gizmos.DrawWireCube( _paddingCenter, _paddingSize * displayScale );

			Gizmos.color = Color.white;
			Gizmos.DrawWireCube( _center, _size * displayScale );

			Gizmos.color = color;
		}
	}
}
