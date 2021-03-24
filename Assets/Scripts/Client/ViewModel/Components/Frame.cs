using Framework;
using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Frame {

		// ********** OUTPUTS ***********

		[SerializeField] public Output<Vector3> Position = new Output<Vector3>();
		[SerializeField] public Output<Vector3> Size = new Output<Vector3>();

		// *****************************
	}
}