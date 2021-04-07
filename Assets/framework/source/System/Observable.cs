using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework {

	[Serializable]
	public class Observable<T> {

		public Observable ( T initialValue, Action<T> onChange ) {

			_value = initialValue;
			_onChange = onChange;
			_onChange?.Invoke( _value );
		}

		public T Get () => _value;
		public virtual void Set ( T value ) {

			// check for equality
			if ( EqualityComparer<T>.Default.Equals( _value, value ) ) {
				return;
			}

			_value = value;
			_onChange?.Invoke( _value );
		}

		protected T _value;
		protected Action<T> _onChange;
	}

	[Serializable]
	public class ListObservable<T> : Observable<IList<T>>
		where T : IEquatable<T> {

		public ListObservable ( IList<T> initialValue, Action<IList<T>> onChange )
			: base( initialValue, onChange ) {

			_comparisonArray = new T[0];
		}

		// set is completely-overriden for list equality check
		public override void Set ( IList<T> value ) {

			// check for equality
			if ( _value == null || value == null ) {
				if ( value == null && _value == null ) {
					return;
				}
			} else if ( value.SequenceEqual( _comparisonArray ) ) {
				return;
			}

			_comparisonArray = value.ToArray<T>();
			_value = value;
			_onChange?.Invoke( _value );
		}

		// data
		private T[] _comparisonArray;
	}

	// TODO: HashSetObservable

	[Serializable]
	public class ValidatedObservable<T> : Observable<T> {

		public ValidatedObservable ( T initialValue, Action<T> onChange, Action<bool> onValid ) : base( initialValue, onChange ) {

			_onValid = onValid;
			Validate( sendsNotification: false );
		}

		// set override for validation, still call base
		public override void Set ( T value ) {

			base.Set( value );
			Validate( sendsNotification: true );
		}

		// validation modifiers
		public void SetValidators ( Func<T, bool> validator )
			=> SetValidators( new List<Func<T, bool>> { validator } );
		public void SetValidators ( List<Func<T, bool>> validators ) {

			_validators.Clear();
			if ( validators != null ) { _validators.AddRange( validators ); }
			Validate( sendsNotification: true );
		}

		// data
		private bool _isValid = true;
		private Action<bool> _onValid;
		private List<Func<T, bool>> _validators = new List<Func<T, bool>>();

		// private methods
		private void Validate ( bool sendsNotification ) {

			var isValid = true;

			foreach ( var validator in _validators ) {
				if ( !validator( _value ) ) {
					isValid = false;
					break;
				}
			}

			if ( isValid != _isValid ) {
				_isValid = isValid;
				if ( sendsNotification ) {
					_onValid?.Invoke( _isValid );
				}
			}
		}
	}

}