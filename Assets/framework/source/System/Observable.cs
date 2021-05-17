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

				var list = get();
				if ( list == null ) {
					list = new List<T>();
					set( list );
				}
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
				var hashSet = get();
				if ( hashSet == null ) {
					hashSet = new HashSet<T>();
					set( hashSet );
				}
				hashSet.Clear();
				hashSet.UnionWith( value );
			} else {
				set( null );
			}
		}
	}


	[Serializable]
	public class DictionaryObservable<K, V> : Observable<Dictionary<K, V>>
		where K : IEquatable<K>
		where V : IEquatable<V> {

		public DictionaryObservable ( Dictionary<K, V> initialValue, Action<Dictionary<K, V>> onChange )
			: base( initialValue, onChange ) { }

		protected override bool CheckEquality ( Dictionary<K, V> a, Dictionary<K, V> b ) {

			if ( a != null && b != null ) {
				if ( a.Count == b.Count && a.Keys.All( b.Keys.Contains ) ) {
					foreach ( var kvp in a ) {
						var value = kvp.Value;
						var other = b[kvp.Key];
						if ( !value.Equals( other ) ) {
							return false;
						}
					}
					return true;
				} else {
					return false;
				}
			}
			return ( a == null && b == null );
		}
		protected override void SetValue ( Dictionary<K, V> value, Func<Dictionary<K, V>> get, Action<Dictionary<K, V>> set ) {

			if ( value != null ) {

				var dictionary = get();
				if ( dictionary == null ) {
					dictionary = new Dictionary<K, V>();
					set( dictionary );
				}
				dictionary.Clear();
				value.ForEach( ( k, v ) => dictionary[k] = v );
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
			=> SetValidators( validators != null ? new List<Func<T, bool>>( validators ) : null );
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