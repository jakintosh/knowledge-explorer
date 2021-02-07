using System;
using System.Collections.Generic;

public static class List_T_FunctionalExtensions {

	public static T First<T> ( this List<T> list ) {

		return list.Count > 0 ? list[0] : default( T );
	}
	public static T Last<T> ( this List<T> list ) {

		return list[list.Count - 1];
	}


	public static List<T> Map<T> ( this List<T> list, Func<T, T> modify ) {

		var results = new List<T>();
		list.ForEach( element => {
			results.Add( modify( element ) );
		} );
		return results;
	}
	public static List<T> Filter<T> ( this List<T> list, Func<T, bool> compare ) {

		var results = new List<T>();
		list.ForEach( element => {
			if ( compare( element ) ) {
				results.Add( element );
			}
		} );
		return results;
	}
	public static T Reduce<T> ( this List<T> list, T startValue, Func<T, T, T> accumulate ) {

		var value = startValue;
		list.ForEach( element => {
			value = accumulate( value, element );
		} );
		return value;
	}
	public static U Reduce<T, U> ( this List<T> list, U startValue, Func<U, T, U> accumulate ) {

		var value = startValue;
		list.ForEach( element => {
			value = accumulate( value, element );
		} );
		return value;
	}
	public static List<U> Convert<T, U> ( this List<T> list, Func<T, U> convert ) {

		var results = new List<U>();
		list.ForEach( element => {
			results.Add( convert( element ) );
		} );
		return results;
	}
}

// *************** Dictionary Extensions ***************

[Serializable]
public struct SerializableKeyValuePair<K, V> {
	[UnityEngine.SerializeField] public K Key;
	[UnityEngine.SerializeField] public V Value;
}

public static class Dictionary_K_V_FunctionalExtensions {

	public static void CopyToSerializableList<K, V> ( this Dictionary<K, V> dictionary, List<SerializableKeyValuePair<K, V>> list ) {

		foreach ( var pair in dictionary ) {
			list.Add( new SerializableKeyValuePair<K, V> { Key = pair.Key, Value = pair.Value } );
		}
	}

	public static void CopyToDictionary<K, V> ( this List<SerializableKeyValuePair<K, V>> list, Dictionary<K, V> dictionary ) {

		foreach ( var entry in list ) {
			dictionary.Add( entry.Key, entry.Value );
		}
	}
}


