using Framework;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Frame {

		// ********** OUTPUTS ***********

		[JsonIgnore] public Output<Vector3> Position = new Output<Vector3>();
		[JsonIgnore] public Output<Vector3> Size = new Output<Vector3>();

		// *****************************

		[OnDeserialized]
		private void OnAfterDeserialize ( StreamingContext context ) {
			Position.Set( new Vector3( position.x, position.y, position.z ) );
			Size.Set( new Vector3( size.x, size.y, size.z ) );
		}

		[JsonProperty] private (float x, float y, float z) position;
		[JsonProperty] private (float x, float y, float z) size;
	}
}