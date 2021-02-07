using UnityEngine;

public enum Directions {
	Invalid = -1,
	North = 0,
	East = 1,
	South = 2,
	West = 3,
	Count = 4
}

public static class Directions_Extensions {

	// ********** extensions on other types that use Directions **********

	public static Directions? ToDirection ( this Vector2 fromAxis ) {

		if ( fromAxis.x > 0.9 ) { return Directions.East; }
		if ( fromAxis.x < -0.9 ) { return Directions.West; }
		if ( fromAxis.y > 0.9 ) { return Directions.North; }
		if ( fromAxis.y < -0.9 ) { return Directions.South; }

		return null;
	}

	public static Vector2Int MoveInDirection ( this Vector2Int position, Directions direction )
		=> position + direction.ToVector();


	// *******************************************************************


	public static Directions ConvertToBasis ( this Directions directions, Directions basis )
		=> (Directions)( ( (int)directions + (int)basis ).Modulo( (int)Directions.Count ) );


	public static Vector2Int ToVector ( this Directions direction ) {

		switch ( direction ) {

			case Directions.North:
				return new Vector2Int( 0, 1 );

			case Directions.South:
				return new Vector2Int( 0, -1 );

			case Directions.East:
				return new Vector2Int( 1, 0 );

			case Directions.West:
				return new Vector2Int( -1, 0 );

			default:
				return Vector2Int.zero;
		}
	}

	public static Vector3 ToHeading ( this Directions direction ) {

		switch ( direction ) {

			case Directions.North:
				return Vector3.forward;

			case Directions.East:
				return Vector3.right;

			case Directions.South:
				return Vector3.back;

			case Directions.West:
				return Vector3.left;

			default:
				return Vector3.zero;
		}
	}

	public static Quaternion ToQuaternion ( this Directions direction )
		=> Quaternion.LookRotation( forward: direction.ToHeading() );

	public static Directions RotatedCW ( this Directions direction )
		=> (Directions)( ( (int)direction + 1 ).Modulo( (int)Directions.Count ) );

	public static Directions RotatedCCW ( this Directions direction )
		=> (Directions)( ( (int)direction - 1 ).Modulo( (int)Directions.Count ) );
}
