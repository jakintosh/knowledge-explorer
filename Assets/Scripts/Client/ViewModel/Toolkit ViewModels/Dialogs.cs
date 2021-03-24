using Framework;
using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Dialog {

		// ********** OUPUTS ***********

		[SerializeField] public Output<string> Title = new Output<string>();
		[SerializeField] public Output<bool> IsOpen = new Output<bool>();

		// ********** INPUTS ***********

		public void Open () {
			IsOpen.Set( true );
		}
		public void Close () {
			IsOpen.Set( false );
		}

		// *****************************
	}

	[Serializable]
	public class ValidatedTextEntryDialog : Dialog {

		// ********** OUPUTS ***********

		[SerializeField] public ValidatedOutput<string> ValidatedText = new ValidatedOutput<string>();

		// ********** INPUTS ***********


		// *****************************


		// runtime model
		public ValidatedTextEntryDialog ( Func<string, bool> stringValidator ) {
			ValidatedText.AddValidator( stringValidator );
		}
	}
}