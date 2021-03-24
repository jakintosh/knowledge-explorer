using UnityEngine;

public static class InputHelpers {

	public static Vector3 GetSteeringVector ( Vector2 stick, Vector3 cameraForward ) {

		// convert input to XZ plane
		var inputXZ = new Vector3( stick.x, 0, stick.y );

		// get the world-centric movement vector
		var camXZForward = Vector3.ProjectOnPlane( cameraForward, Vector3.up ).normalized;
		var camXZRotation = Quaternion.LookRotation( camXZForward, Vector3.up );
		return camXZRotation * inputXZ;
	}

	public static Vector3 GetRelativeDirection ( Vector3 referenceForward, Vector3 worldDirection )
		=> Quaternion.FromToRotation( referenceForward, worldDirection ) * Vector3.forward;
}