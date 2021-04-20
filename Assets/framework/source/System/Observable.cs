using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework {

	[Serializable]
	public class Observable<T> {

		public Observable ( T initialValue, Action<T> onChange ) {

			SetValue( _value, getter: () => _lastValue, setter: v => _lastValue = v );
			SetValue( initialValue, getter: () => _value, setter: v => _value = v );
			_onChange = onChange;
			_onChange?.Invoke( _value );
		}

		public T Previous () => _lastValue;
		public T Get () => _value;
		public virtual void Set ( T value ) {

			if ( CheckEquality( value, _value ) ) {
				return;
			}

			SetValue( _value, getter: () => _lastValue, setter: v => _lastValue = v );
			SetValue( value, getter: () => _value, setter: v => _value = v );
			_onChange?.Invoke( _value );
		}

		protected T _lastValue;
		protected T _value;
		protected Action<T> _onChange;

		protected virtual void SetValue ( T value, Func<T> getter, Action<T> setter ) {
			setter( value );
		}
		protected virtual bool CheckEquality ( T a, T b ) {
			return EqualityComparer<T>.Default.Equals( a, b );
		}
	}

	[Serializable]
	public class ListObservable<T> : Observable<IList<T>>
		where T : IEquatable<T> {

		public ListObservable ( IList<T> initialValue, Action<IList<T>> onChange )
			: base( initialValue, onChange ) { }

		protected override bool CheckEquality ( IList<T> a, IList<T> b ) {

			if ( a != null && b != null ) {
				return Enumerable.SequenceEqual( a, b );
			}
			return ( a == null && b == null );
		}
		protected override void SetValue ( IList<T> value, Func<IList<T>> get, Action<IList<T>> set ) {

			if ( value != null ) {
				if ( get() == null ) { set( new List<T>() ); }
				var list = get();
				list.Clear();
				foreach ( var v in value ) { list.Add( v ); }
			} else {
				set( null );
			}
		}
	}

	[Serializable]
	public class HashSetObservable<T> : Observable<HashSet<T>>
		where T : IEquatable<T> {

		public HashSetObservable ( HashSet<T> initialValue, Action<HashSet<T>> onChange )
			: base( initialValue, onChange ) { }

		protected override bool CheckEquality ( HashSet<T> a, HashSet<T> b ) {

			if ( a != null && b != null ) {
				return a.SetEquals( b );
			}
			return ( a == null && b == null );
		}
		protected override void SetValue ( HashSet<T> value, Func<HashSet<T>> get, Action<HashSet<T>> set ) {

			if ( value != null ) {
				if ( get() == null ) { set( new HashSet<T>() ); }
				var hashSet = get();
				hashSet.Clear();
				hashSet.UnionWith( value );
			} else {
				set( null );
			}
		}
	}

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
		public void SetValidators ( Func<T, bool>[] validators )
			=> SetValidators( new List<Func<T, bool>>( validators ) );
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