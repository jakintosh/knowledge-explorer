using Jakintosh.Observable;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Explorer.View {

	public enum CornerType {
		RoundedRect,
		Superellipse
	}

	[RequireComponent( typeof( MeshFilter ) )]
	[RequireComponent( typeof( MeshRenderer ) )]
	public class Panel : MonoBehaviour {

		public void SetSize ( Vector3 size ) {

			Width = size.x;
			Height = size.y;
			Thickness = size.z;
			GenerateMesh( _mesh );

			if ( _canvas != null ) {
				var contentW = GetWidth - ( 2 * GetChamfer );
				var contentH = GetHeight - ( 2 * GetChamfer );
				var canvasRT = _canvas.gameObject.GetRectTransform();
				canvasRT.pivot = new Vector2( 0.5f, 0.0f );
				canvasRT.sizeDelta = new Vector2( contentW / _canvasScale, contentH / _canvasScale );
				_canvas.transform.localScale = new Vector3( _canvasScale, _canvasScale, _canvasScale );
				_canvas.transform.localRotation = Quaternion.AngleAxis( 180f, Vector3.up );
				_canvas.transform.localPosition = new Vector3( 0f, -contentH / 2, ( Thickness / 2f ) + _canvasHover );
			}
		}

		[SerializeField] private Canvas _canvas;
		[SerializeField] private float _canvasScale;
		[SerializeField] private float _canvasHover;
		[SerializeField] private Material _material;

		public float GetWidth => Width.WithFloor( 0.01f );
		public float GetHeight => Height.WithFloor( 0.01f );
		public float GetThickness => Thickness.WithFloor( 0.01f );
		public float GetCornerRadius => CornerRadius.ClampedBetween( 0f, SmallestDimension / 2f );
		public int GetSegments => CornerResolution.ClampedBetween( 0, 50 );
		public float GetChamfer => ChamferDepth.ClampedBetween( 0f, Mathf.Min( GetCornerRadius, ( GetThickness / 2f ) ) );

		public float Width = 1.0f;
		public float Height = 5f / 3f;
		public float Thickness = 0.1f;
		public float CornerRadius = 0.2f;
		public int CornerResolution = 10;
		public float ChamferDepth = 0.075f;

		public float SmallestDimension => Mathf.Min( Width, Height );

		[SerializeField] private CornerType _cornerType = CornerType.Superellipse;
		[SerializeField] private float _power = 4;

		private Mesh _mesh;
		private MeshFilter _filter;
		private MeshRenderer _renderer;

		private void Init () {

			_mesh = new Mesh();
			_filter = GetComponent<MeshFilter>();
			_renderer = GetComponent<MeshRenderer>();

			_renderer.material = _material;

			_filter.mesh = _mesh;
		}

		private void GenerateMesh ( Mesh mesh ) {

			GenerateMesh(
				mesh: mesh,
				width: GetWidth,
				height: GetHeight,
				thickness: GetThickness,
				cornerRadius: GetCornerRadius,
				segments: GetSegments,
				chamfer: GetChamfer
			);
		}

		private void GenerateMesh ( Mesh mesh,
			float width, float height, float thickness,
			float cornerRadius, int segments, float chamfer ) {

			// things that remove verts
			// 0 chamfer (no chamfer strips, - 4 vert edges and - 2 tri edges)
			// maxed chamfer ( no edge strips, - 2 vert edges and - 1 tri edges)
			// 0 corner radius ( no corners, - 2 vert edges and - 1 tri edges)
			// maxed corner radius ( no connectors on any dimension that is maxed)
			// the problem here is these verts should be shared...
			// but only on the edge connectors

			bool hasChamfer = chamfer > 0f;
			bool hasCorners = cornerRadius > 0f;
			bool hasVerticalConnector = cornerRadius < height / 2;
			bool hasHorizontalConnector = cornerRadius < width / 2;

			var numVerts = GetVertsPerCorner( segments ) * 4;
			// if ( !hasChamfer ) {
			// 	numVerts -= GetVerticesPerCornerEdge( segments ) * 4;
			// }
			var vertices = new Vector3[numVerts];
			var tris = new List<int>();

			// build corner center points
			var UL = new Vector3(
				x: ( width / 2f ) - cornerRadius,
				y: ( height / 2f ) - cornerRadius,
				z: 0f
			);
			Vector3 UR = UL, LL = UL, LR = UL;
			UR.x *= -1;
			LL.y *= -1;
			LR.x *= -1;
			LR.y *= -1;
			var centerPoints = new[] { UL, UR, LR, LL };

			var angleIncrement = ( 2f * Mathf.PI ) / ( segments * 4 );

			for ( int cornerIndex = 0; cornerIndex < 4; cornerIndex++ ) {

				bool isHorizontalCorner = cornerIndex.IsEven();
				int nextCornerStart = GetCornerStartIndex( ( cornerIndex + 1 ) % 4, segments );

				var angleOffset = ( Mathf.PI / 2f ) * cornerIndex;
				var centerPoint = centerPoints[cornerIndex];

				var faceOffset = GetVerticesPerCornerFace( segments );
				var frontFaceStartIndex = GetCornerStartIndex( cornerIndex, segments );
				var rearFaceStartIndex = frontFaceStartIndex + faceOffset;

				var edgeOffset = GetVerticesPerCornerEdge( segments );
				var frontEdgeStartIndex = rearFaceStartIndex + faceOffset;
				var rearEdgeStartIndex = frontEdgeStartIndex + edgeOffset;

				var frontChamferStartIndex = rearEdgeStartIndex + edgeOffset;
				var rearChamferStartIndex = frontChamferStartIndex + ( 2 * edgeOffset );

				var frontZ = centerPoint.z + ( thickness / 2f );
				var rearZ = centerPoint.z - ( thickness / 2f );

				// front/rear inner corners
				vertices[frontFaceStartIndex] = new Vector3( centerPoint.x, centerPoint.y, frontZ );
				vertices[rearFaceStartIndex] = new Vector3( centerPoint.x, centerPoint.y, rearZ );

				// corner radius segments
				for ( int cornerSegment = 0; cornerSegment <= segments; cornerSegment++ ) {

					var ø = ( angleIncrement * cornerSegment ) + angleOffset;
					var x = _cornerType switch {
						CornerType.RoundedRect => Mathf.Cos( ø ),
						CornerType.Superellipse => supercos( ø, 1 )
					};
					var y = _cornerType switch {
						CornerType.RoundedRect => Mathf.Sin( ø ),
						CornerType.Superellipse => supersin( ø, 1 )
					};
					var frontVert = new Vector3(
						centerPoint.x + ( x * cornerRadius ),
						centerPoint.y + ( y * cornerRadius ),
						frontZ
					);
					var rearVert = new Vector3(
						centerPoint.x + ( x * cornerRadius ),
						centerPoint.y + ( y * cornerRadius ),
						rearZ
					);

					// chamfer info
					var chamferZOffset = new Vector3( 0, 0, chamfer );
					var chamferXYOffset = new Vector3( x * chamfer, y * chamfer, 0 );

					// faces
					var frontIndex = frontFaceStartIndex + 1 + cornerSegment;
					var rearIndex = rearFaceStartIndex + 1 + cornerSegment;
					var frontFaceVert = frontVert - chamferXYOffset;
					var rearFaceRimVert = rearVert - chamferXYOffset;
					vertices[frontIndex] = frontFaceVert;
					vertices[rearIndex] = rearFaceRimVert;
					if ( cornerSegment < segments ) {
						tris.AddRange( new[] { frontFaceStartIndex, frontIndex, frontIndex + 1 } ); // ccw, forward
						tris.AddRange( new[] { rearFaceStartIndex, rearIndex + 1, rearIndex } ); // cw, backwards
					}

					// edge
					var frontEdgeIndex = frontEdgeStartIndex + cornerSegment;
					var rearEdgeIndex = rearEdgeStartIndex + cornerSegment;
					var frontEdgeVert = frontVert - chamferZOffset;
					var rearEdgeRimVert = rearVert + chamferZOffset;
					vertices[frontEdgeIndex] = frontEdgeVert;
					vertices[rearEdgeIndex] = rearEdgeRimVert;
					if ( cornerSegment > 0 ) {
						var leadingFrontIndex = frontEdgeIndex;
						var trailingFrontIndex = frontEdgeIndex - 1;
						var leadingRearIndex = rearEdgeIndex;
						var trailingRearIndex = rearEdgeIndex - 1;
						if ( cornerSegment == segments ) { // last one
							if ( ( isHorizontalCorner && !hasHorizontalConnector ) || ( !isHorizontalCorner && !hasVerticalConnector ) ) { // should share verts
								leadingFrontIndex = nextCornerStart + ( faceOffset * 2 );
								leadingRearIndex = nextCornerStart + ( faceOffset * 2 ) + edgeOffset;
							}
						}
						tris.AddRange( new[] { leadingFrontIndex, trailingFrontIndex, leadingRearIndex } );
						tris.AddRange( new[] { leadingRearIndex, trailingFrontIndex, trailingRearIndex } );
					}

					// chamfer
					// if ( hasChamfer ) {
					var frontChamferNormal = ( frontEdgeVert - ( frontFaceVert - chamferZOffset ) ).normalized;
					var rearChamferNormal = Vector3.Scale( frontChamferNormal, new Vector3( 1, 1, -1 ) );
					var frontChamferFaceIndex = frontChamferStartIndex + cornerSegment;
					var frontChamferEdgeIndex = frontChamferStartIndex + cornerSegment + edgeOffset;
					var rearChamferFaceIndex = rearChamferStartIndex + cornerSegment;
					var rearChamferEdgeIndex = rearChamferStartIndex + cornerSegment + edgeOffset;
					vertices[frontChamferFaceIndex] = frontFaceVert;
					vertices[frontChamferEdgeIndex] = frontEdgeVert;
					vertices[rearChamferFaceIndex] = rearFaceRimVert;
					vertices[rearChamferEdgeIndex] = rearEdgeRimVert;
					if ( cornerSegment > 0 ) {
						tris.AddRange( new[] { frontChamferFaceIndex, frontChamferFaceIndex - 1, frontChamferEdgeIndex } );
						tris.AddRange( new[] { frontChamferEdgeIndex, frontChamferFaceIndex - 1, frontChamferEdgeIndex - 1 } );
						tris.AddRange( new[] { rearChamferEdgeIndex, rearChamferEdgeIndex - 1, rearChamferFaceIndex } );
						tris.AddRange( new[] { rearChamferFaceIndex, rearChamferEdgeIndex - 1, rearChamferFaceIndex - 1 } );
					}
					// }
				}

				// connectors

				if ( ( isHorizontalCorner && hasHorizontalConnector ) ||
					( !isHorizontalCorner && hasVerticalConnector ) ) {

					// front face connector
					DrawQuadCCW( tris,
						frontFaceStartIndex, // current front center point
						frontFaceStartIndex + segments + 1, // current last vert of arc
						nextCornerStart + 1, // next first vert of arc
						nextCornerStart // next center
					);

					// rear face connector
					DrawQuadCW( tris,
						rearFaceStartIndex, // current rear center point
						rearFaceStartIndex + segments + 1, // current last vert of arc
						nextCornerStart + faceOffset + 1, // next first vert of arc
						nextCornerStart + faceOffset// next center
					);

					// edge connector
					DrawQuadCCW( tris,
						frontEdgeStartIndex + edgeOffset - 1, // current last front edge vert
						rearEdgeStartIndex + edgeOffset - 1, // current last rear edge vert
						nextCornerStart + faceOffset + faceOffset + edgeOffset, // next first rear edge vert
						nextCornerStart + faceOffset + faceOffset // next first front edge vert
					);

					// chamfer connector
					DrawQuadCCW( tris,
						frontChamferStartIndex + edgeOffset - 1, // current last front face vert
						frontChamferStartIndex + ( 2 * edgeOffset ) - 1, // current last front edge vert
						nextCornerStart + ( 2 * faceOffset ) + ( 2 * edgeOffset ) + edgeOffset, // next first front edge vert
						nextCornerStart + ( 2 * faceOffset ) + ( 2 * edgeOffset ) // next first front face vert
					);
					DrawQuadCW( tris,
						rearChamferStartIndex + edgeOffset - 1, // current last rear face vert
						rearChamferStartIndex + ( 2 * edgeOffset ) - 1, // current last rear edge vert
						nextCornerStart + ( 2 * faceOffset ) + ( 4 * edgeOffset ) + edgeOffset, // next first rear edge vert
						nextCornerStart + ( 2 * faceOffset ) + ( 4 * edgeOffset ) // next first rear face vert
					);
				}

			}

			if ( hasHorizontalConnector && hasVerticalConnector ) {

				// interior of front face
				DrawQuadCCW( tris,
					GetCornerStartIndex( 0, segments ),
					GetCornerStartIndex( 1, segments ),
					GetCornerStartIndex( 2, segments ),
					GetCornerStartIndex( 3, segments )
				);

				// interior of rear face
				var faceVertOffset = GetVerticesPerCornerFace( segments );
				DrawQuadCW( tris,
					GetCornerStartIndex( 0, segments ) + faceVertOffset,
					GetCornerStartIndex( 1, segments ) + faceVertOffset,
					GetCornerStartIndex( 2, segments ) + faceVertOffset,
					GetCornerStartIndex( 3, segments ) + faceVertOffset
				);
			}

			mesh.vertices = vertices;
			mesh.triangles = tris.ToArray();

			mesh.RecalculateNormals();
		}

		private int GetCornerStartIndex ( int cornerNumber, int cornerSegments ) {

			var totalCornerVerts = GetVertsPerCorner( cornerSegments );
			return cornerNumber * totalCornerVerts;
		}
		private int GetVertsPerCorner ( int cornerSegments ) {

			var numFaces = 2;
			var vertsPerFace = GetVerticesPerCornerFace( cornerSegments );

			var numEdges = 8;
			var vertsPerEdge = GetVerticesPerCornerEdge( cornerSegments );

			return ( vertsPerFace * numFaces ) + ( vertsPerEdge * numEdges );
		}
		private int GetVerticesPerCornerFace ( int cornerSegments ) {

			var centerVert = 1;
			var edgeVerts = GetVerticesPerCornerEdge( cornerSegments );
			return edgeVerts + centerVert;
		}
		private int GetVerticesPerCornerEdge ( int cornerSegments ) {

			var edgeVerts = cornerSegments + 1;
			return edgeVerts;
		}

		private void DrawQuadCW ( List<int> tris, int a, int b, int c, int d ) {

			DrawQuadCCW( tris, a, d, c, b );
		}
		private void DrawQuadCCW ( List<int> tris, int a, int b, int c, int d ) {

			tris.AddRange( new int[] { a, b, c } );
			tris.AddRange( new int[] { a, c, d } );
		}
		private void DrawCorner ( Vector3[] verts, List<int> tris, int cornerIndex, Vector3 centerPoint, int segments, float radius, float chamfer, float width, float height, float thickness ) {

			var angle = ( 2f * Mathf.PI ) / ( segments * 4 );
			var angleOffset = ( Mathf.PI / 2f ) * cornerIndex;

			var faceOffset = GetVerticesPerCornerFace( segments );
			var frontFaceStartIndex = GetCornerStartIndex( cornerIndex, segments );
			var rearFaceStartIndex = frontFaceStartIndex + faceOffset;

			var edgeOffset = GetVerticesPerCornerEdge( segments );
			var frontEdgeStartIndex = rearFaceStartIndex + faceOffset;
			var rearEdgeStartIndex = frontEdgeStartIndex + edgeOffset;

			var frontChamferStartIndex = rearEdgeStartIndex + edgeOffset;
			var rearChamferStartIndex = frontChamferStartIndex + ( 2 * edgeOffset );

			var frontZ = centerPoint.z + ( thickness / 2f );
			var rearZ = centerPoint.z - ( thickness / 2f );

			// front/rear inner corners
			verts[frontFaceStartIndex] = new Vector3( centerPoint.x, centerPoint.y, frontZ );
			verts[rearFaceStartIndex] = new Vector3( centerPoint.x, centerPoint.y, rearZ );

			// corner radius segments
			for ( int i = 0; i <= segments; i++ ) {

				var ø = ( angle * i ) + angleOffset;
				var x = _cornerType switch {
					CornerType.RoundedRect => Mathf.Cos( ø ),
					CornerType.Superellipse => supercos( ø, 1 )
				};
				var y = _cornerType switch {
					CornerType.RoundedRect => Mathf.Sin( ø ),
					CornerType.Superellipse => supersin( ø, 1 )
				};
				var frontVert = new Vector3(
					centerPoint.x + ( x * radius ),
					centerPoint.y + ( y * radius ),
					frontZ
				);
				var rearVert = new Vector3(
					centerPoint.x + ( x * radius ),
					centerPoint.y + ( y * radius ),
					rearZ
				);

				// chamfer info
				var chamferZOffset = new Vector3( 0, 0, chamfer );
				var chamferXYOffset = new Vector3( x * chamfer, y * chamfer, 0 );

				// faces
				var frontIndex = frontFaceStartIndex + 1 + i;
				var rearIndex = rearFaceStartIndex + 1 + i;
				var frontFaceVert = frontVert - chamferXYOffset;
				var rearFaceRimVert = rearVert - chamferXYOffset;
				verts[frontIndex] = frontFaceVert;
				verts[rearIndex] = rearFaceRimVert;
				if ( i < segments ) { tris.AddRange( new[] { frontFaceStartIndex, frontIndex, frontIndex + 1 } ); } // ccw, forward
				if ( i < segments ) { tris.AddRange( new[] { rearFaceStartIndex, rearIndex + 1, rearIndex } ); } // cw, backwards

				// edge
				var frontEdgeIndex = frontEdgeStartIndex + i;
				var rearEdgeIndex = rearEdgeStartIndex + i;
				var frontEdgeVert = frontVert - chamferZOffset;
				var rearEdgeRimVert = rearVert + chamferZOffset;
				verts[frontEdgeIndex] = frontEdgeVert;
				verts[rearEdgeIndex] = rearEdgeRimVert;
				if ( i > 0 ) {
					tris.AddRange( new[] { frontEdgeIndex, frontEdgeIndex - 1, rearEdgeIndex } );
					tris.AddRange( new[] { rearEdgeIndex, frontEdgeIndex - 1, rearEdgeIndex - 1 } );
				}

				// chamfer
				var frontChamferNormal = ( frontEdgeVert - ( frontFaceVert - chamferZOffset ) ).normalized;
				var rearChamferNormal = Vector3.Scale( frontChamferNormal, new Vector3( 1, 1, -1 ) );
				var frontChamferFaceIndex = frontChamferStartIndex + i;
				var frontChamferEdgeIndex = frontChamferStartIndex + i + edgeOffset;
				var rearChamferFaceIndex = rearChamferStartIndex + i;
				var rearChamferEdgeIndex = rearChamferStartIndex + i + edgeOffset;
				verts[frontChamferFaceIndex] = frontFaceVert;
				verts[frontChamferEdgeIndex] = frontEdgeVert;
				verts[rearChamferFaceIndex] = rearFaceRimVert;
				verts[rearChamferEdgeIndex] = rearEdgeRimVert;
				if ( i > 0 ) {
					tris.AddRange( new[] { frontChamferFaceIndex, frontChamferFaceIndex - 1, frontChamferEdgeIndex } );
					tris.AddRange( new[] { frontChamferEdgeIndex, frontChamferFaceIndex - 1, frontChamferEdgeIndex - 1 } );

					tris.AddRange( new[] { rearChamferEdgeIndex, rearChamferEdgeIndex - 1, rearChamferFaceIndex } );
					tris.AddRange( new[] { rearChamferFaceIndex, rearChamferEdgeIndex - 1, rearChamferFaceIndex - 1 } );
				}
			}

			// connectors
			int nextCornerStart = GetCornerStartIndex( ( cornerIndex + 1 ) % 4, segments );

			// front face connector
			DrawQuadCCW( tris,
				frontFaceStartIndex, // current front center point
				frontFaceStartIndex + segments + 1, // current last vert of arc
				nextCornerStart + 1, // next first vert of arc
				nextCornerStart // next center
			);

			// rear face connector
			DrawQuadCW( tris,
				rearFaceStartIndex, // current rear center point
				rearFaceStartIndex + segments + 1, // current last vert of arc
				nextCornerStart + faceOffset + 1, // next first vert of arc
				nextCornerStart + faceOffset// next center
			);

			// edge connector
			DrawQuadCCW( tris,
				frontEdgeStartIndex + edgeOffset - 1, // current last front edge vert
				rearEdgeStartIndex + edgeOffset - 1, // current last rear edge vert
				nextCornerStart + faceOffset + faceOffset + edgeOffset, // next first rear edge vert
				nextCornerStart + faceOffset + faceOffset // next first front edge vert
			);

			// chamfer connector
			DrawQuadCCW( tris,
				frontChamferStartIndex + edgeOffset - 1, // current last front face vert
				frontChamferStartIndex + ( 2 * edgeOffset ) - 1, // current last front edge vert
				nextCornerStart + ( 2 * faceOffset ) + ( 2 * edgeOffset ) + edgeOffset, // next first front edge vert
				nextCornerStart + ( 2 * faceOffset ) + ( 2 * edgeOffset ) // next first front face vert
			);
			DrawQuadCW( tris,
				rearChamferStartIndex + edgeOffset - 1, // current last rear face vert
				rearChamferStartIndex + ( 2 * edgeOffset ) - 1, // current last rear edge vert
				nextCornerStart + ( 2 * faceOffset ) + ( 4 * edgeOffset ) + edgeOffset, // next first rear edge vert
				nextCornerStart + ( 2 * faceOffset ) + ( 4 * edgeOffset ) // next first rear face vert
			);
		}

		private void OnDrawGizmos () {

			Init();
			SetSize( new Vector3( Width, Height, Thickness ) );
		}

		private float supercos ( float ø, float width ) {

			var pow = width * _power;
			var cosØ = Mathf.Cos( ø );
			var sign = Mathf.Sign( cosØ );
			return Mathf.Pow( Mathf.Abs( cosØ ), 2 / pow ) * width * sign;
		}
		private float supersin ( float ø, float height ) {

			var pow = height * _power;
			var sinØ = Mathf.Sin( ø );
			var sign = Mathf.Sign( sinØ );
			return Mathf.Pow( Mathf.Abs( sinØ ), 2 / pow ) * height * sign;
		}
	}

}
