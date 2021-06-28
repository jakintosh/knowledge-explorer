using UnityEngine;

public enum Sign {
	Negative = -1,
	Positive = 1
}

public enum Comparison {
	LessThan = -1,
	Equal = 0,
	GreaterThan = 1
}

public static class Bool_Extensions {

	public static bool Toggled ( this bool b ) {
		return !b;
	}
}

public static class Int_Extensions {

	public static int Modulo ( this int from, int operand ) {

		// positive modulo
		if ( from >= 0 ) {
			return from % operand;
		}

		// negative modulo
		do {
			from += operand;
		} while ( from < 0 );
		return from;
	}

	public static bool IsEven ( this int num )
		=> num % 2 == 0;

	public static bool IsOdd ( this int num )
		=> !IsEven( num );

	public static int WithCeiling ( this int value, int other )
		=> value <= other ? value : other;

	public static int WithFloor ( this int value, int other )
		=> value >= other ? value : other;

	public static int WithSign ( this int value, Sign sign )
		=> value * (int)sign;

	public static int ClampedBetween ( this int value, int min, int max )
		=> value.WithCeiling( max ).WithFloor( min );

}

public static class Float_Extensions {

	// utility
	public static float AbsoluteValue ( this float value )
		=> Mathf.Abs( value );


	public static float WithCeiling ( this float value, float other )
		=> value < other ? value : other;

	public static float WithFloor ( this float value, float other )
		=> value > other ? value : other;

	public static float ClampedBetween ( this float value, float min, float max )
		=> value.WithCeiling( max ).WithFloor( min );

	public static float WithSign ( this float value, Sign sign )
		=> value * (int)sign;

	public static (float magnitude, Sign sign) ToMagnitudeAndSign ( this float value )
		=> (magnitude: value.AbsoluteValue(), sign: value >= 0 ? Sign.Positive : Sign.Negative);

	public static Sign ToSign ( this float value )
		=> value >= 0f ? Sign.Positive : Sign.Negative;

	public static float Sin ( this float value, float min, float max )
		=> min + ( ( ( Mathf.Sin( value ) / 2f ) + 0.5f ) * ( max - min ) );

	// comparison
	public static bool IsApproximately ( this float value, float to )
		=> Mathf.Approximately( value, to );


	// mapping
	public static float Lerp ( this float from, float to, float t )
		=> Mathf.Lerp( from, to, t );

	public static float Map ( this float value, float fromStart, float fromEnd, float toStart, float toEnd )
		=> ( ( ( value - fromStart ) / ( fromEnd - fromStart ) ) * ( toEnd - toStart ) ) + toStart;

	public static float Normalized ( this float t, float fromTotal )
		=> t / fromTotal;

	public static float Normalized01 ( this float t, float fromTotal )
		=> Mathf.Clamp01( t.Normalized( fromTotal ) );


	// physics
	public static float IntegrateAcceleration ( this float velocity, float accleration, float deltaTime, float min = float.MinValue, float max = float.MaxValue )
		=> Mathf.Clamp( velocity.Integrate( accleration, deltaTime ), min, max );

	public static float IntegrateVelocity ( this float distance, float velocity, float deltaTime )
		=> distance.Integrate( velocity, deltaTime );

	public static float Integrate ( this float a, float b, float step )
		=> a + ( b * step );
}

public static class Vector2Int_Extensions {

	public static Vector3 MapToXZVector3 ( this Vector2Int vector, float y = 0f )
		=> new Vector3( vector.x, y, vector.y );
}

public static class Vector2_Extensions {

	public static Vector2 Clamp ( this Vector2 vector, Vector2 min, Vector2 max )
		=> new Vector2( vector.x.ClampedBetween( min.x, max.x ), vector.y.ClampedBetween( min.y, max.y ) );
}

public static class Vector3_Extensions {

	// acceleration
	public static Vector3 IntegrateAcceleration ( this Vector3 velocity, Vector3 acceleration, float deltaTime, float terminalVelocity = float.MaxValue )
		=> Vector3.ClampMagnitude( velocity + ( acceleration * deltaTime ), terminalVelocity );

	public static Vector3 IntegrateVelocity ( this Vector3 distance, Vector3 velocity, float deltaTime )
		=> distance + ( velocity * deltaTime );

	public static Vector3 RelativeTo ( this Vector3 worldDirection, Vector3 referenceDirection )
		=> Quaternion.FromToRotation( referenceDirection, worldDirection ) * ( Vector3.forward * worldDirection.magnitude );
}

public static class RectTransform_Extensions {

	public static Vector3 ConvertPointToOtherSpace ( this RectTransform rt, Vector3 point, RectTransform otherRT )
		=> otherRT.InverseTransformPoint( rt.TransformPoint( point ) );
}

public static class GameObject_Extensions {

	public static RectTransform GetRectTransform ( this GameObject gameObject )
		=> ( gameObject.transform as RectTransform );
}

public static class Quaternion_Extensions {

	public static Quaternion SlerpTo ( this Quaternion q, Vector3 heading, float t )
		=> heading.magnitude > 0 ? Quaternion.Slerp( q, Quaternion.LookRotation( heading, Vector3.up ), t ) : q;
}

public static class AnimationCurve_Extensions {

	public static float GetDelta ( this AnimationCurve curve, float from, float to ) => curve.Evaluate( to ) - curve.Evaluate( from );
}

// CUSTOM TYPES

public struct IncrementorWithMemory {

	public float Value { get; private set; }
	public float LastValue { get; private set; }

	public IncrementorWithMemory ( float value ) {
		Value = value;
		LastValue = value;
	}
	public void Increment ( float by ) {
		LastValue = Value;
		Value += by;
	}
	public void Reset ( float to = 0 ) {
		LastValue = to;
		Value = to;
	}
}

public struct Range {

	public Range ( float min, float max ) {

		Min = min;
		Max = max;
	}

	public bool Contains ( float value ) {

		return value >= Min && value <= Max;
	}
	public float Percentage ( float value ) {

		return ( value - Min ) / ValueRange;
	}
	public float ConvertFrom ( float value, Range range ) {

		var otherPct = range.Percentage( value );
		var converted = ( otherPct * ValueRange ) + Min;
		return converted;
	}

	private float Min;
	private float Max;
	private float ValueRange { get => Max - Min; }
}


