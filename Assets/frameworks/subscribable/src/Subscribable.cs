using Jakintosh.Observable;
using System;
using System.Collections.Generic;

namespace Jakintosh.Subscribable {

	[Serializable]
	public class Subscribable<T> {

		public Subscribable ( T initialValue, Action<T> onChange ) {

			_handlers = new List<Action<T>>();
			_observable = new Observable<T>(
				initialValue: initialValue,
				onChange: value => {
					onChange( value );
					_handlers.ForEach( h => h( value ) );
				}
			);
		}

		public T Previous ()
			=> _observable.Previous();
		public T Get ()
			=> _observable.Get();
		public void Set ( T value )
			=> _observable.Set( value );
		public void Subscribe ( Action<T> handler )
			=> _handlers.Add( handler );
		public void Unsubscribe ( Action<T> handler )
			=> _handlers.Remove( handler );


		private List<Action<T>> _handlers;
		private Observable<T> _observable;

	}

}
