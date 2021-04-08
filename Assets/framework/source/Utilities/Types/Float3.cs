using System;
using UnityEngine;

[Serializable]
public struct Float3 {

	[SerializeField] public float x;
	[SerializeField] public float y;
	[SerializeField] public float z;

	public Float3 ( float x, float y, float z ) {
		this.x = x;
		this.y = y;
		this.z = z;
	}
	public Float3 ( float x, float y ) : this( x, y, 0f ) { }

	public Vector3 ToVector3 () => new Vector3( this.x, this.y, this.z );
	public static Float3 From ( Vector3 v ) => new Float3( v.x, v.y, v.z );

	public static Float3 Zero => new Float3( 0, 0, 0 );
	public static Float3 One => new Float3( 1, 1, 1 );

	public static Float3 operator + ( Float3 a ) => a;
	public static Float3 operator + ( Float3 a, Float3 b ) => new Float3( a.x + b.x, a.y + b.y, a.z + b.z );

	public static Float3 operator - ( Float3 a ) => new Float3( -a.x, -a.y, -a.z );
	public static Float3 operator - ( Float3 a, Float3 b ) => a + ( -b );

	public static Float3 operator * ( Float3 a, float b ) => new Float3( a.x * b, a.y * b, a.z * b );
	public static Float3 operator * ( Float3 a, Float3 b ) => new Float3( a.x * b.x, a.y * b.y, a.z * b.z );
	public static Float3 operator / ( Float3 a, float b ) => new Float3( a.x / b, a.y / b, a.z / b );
}

