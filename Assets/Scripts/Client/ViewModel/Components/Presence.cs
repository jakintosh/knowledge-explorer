using Framework;
using System;
using UnityEngine;

namespace Client.ViewModel {

	[Serializable]
	public class Presence {

		// data types
		public enum Contexts {
			Floating,
			Focused
		}
		public enum Sizes {
			Expanded,
			Compact
		}


		// ********** OUPUTS ***********

		[SerializeField] public Output<bool> Closed = new Output<bool>();
		[SerializeField] public Output<Sizes> Size = new Output<Sizes>();
		[SerializeField] public Output<Contexts> Context = new Output<Contexts>();

		// ********** INPUTS ***********

		// public void Close () => Closed.Invoke();
		public void Close () => Closed.Set( true );
		public void CycleSize () => Size.Set(
			Size.Get() switch {
				Sizes.Compact => Sizes.Expanded,
				Sizes.Expanded => Sizes.Compact,
				_ => throw new ArgumentOutOfRangeException( nameof( Size ) )
			} );
		public void CycleContext () => Context.Set(
			Context.Get() switch {
				Contexts.Floating => Contexts.Focused,
				Contexts.Focused => Contexts.Floating,
				_ => throw new ArgumentOutOfRangeException( nameof( Context ) )
			} );

		// *****************************
	}
}