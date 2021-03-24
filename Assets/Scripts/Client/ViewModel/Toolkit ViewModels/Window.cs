using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Window {

		// view model components
		[SerializeField] public Frame Frame = new Frame();
		[SerializeField] public Presence Presence = new Presence();
	}
}
