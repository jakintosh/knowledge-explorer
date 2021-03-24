using Framework;
using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Edit {

		// data types
		public enum States {
			View = 0,
			Edit = 1
		}


		// ********** OUTPUTS ***********

		[SerializeField] public Output<States> EditState = new Output<States>();

		// *****************************
	}
}