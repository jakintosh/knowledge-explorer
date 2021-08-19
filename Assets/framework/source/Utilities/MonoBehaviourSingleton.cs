using UnityEngine;

public abstract class MonoBehaviourSingleton<T> : MonoBehaviour
	where T : MonoBehaviourSingleton<T> {

	// ********** Private Interface **********

	private static bool _isQuitting = false;

	private static T _instance;
	protected static T Instance {
		get {
			if ( _instance == null ) {
				GetNewInstance();
			}
			return _instance;
		}
	}
	private static void GetNewInstance () {

		if ( _isQuitting ) { return; }

		_instance = GameObject.FindObjectOfType<T>();
		if ( _instance == null ) {
			_instance = new GameObject( "API" ).AddComponent<T>();
		}
	}

	protected abstract void Init ();
	protected abstract void Deinit ();

	protected virtual void Awake () {

		if ( _instance != null && _instance != this ) {
			Destroy( this );
		} else {
			_instance = this as T;
			Init();
		}
	}
	protected virtual void OnDestroy () {

		if ( _instance == this ) {
			Deinit();
		}
	}
	protected virtual void OnApplicationQuit () {

		_isQuitting = true;
	}
}
