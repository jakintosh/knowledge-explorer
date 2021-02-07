using System;

public class Lazy<T> {

	private T _value;
	private bool _isInitialized = false;
	private Func<T> _get;

	public Lazy ( Func<T> get ) {
		_get = get;
	}

	public T Value {
		get {
			if ( !_isInitialized ) {
				_value = _get();
				_isInitialized = true;
			}
			return _value;
		}
	}
}