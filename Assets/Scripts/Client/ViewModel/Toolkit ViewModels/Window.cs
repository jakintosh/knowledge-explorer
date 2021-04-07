using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Window {

		public Window () {

			Frame.Size.Set( new Vector3( 4f, 6f, 0.5f ) );
		}

		// view model components
		[SerializeField] public Frame Frame = new Frame();
		[SerializeField] public Presence Presence = new Presence();
	}
}
