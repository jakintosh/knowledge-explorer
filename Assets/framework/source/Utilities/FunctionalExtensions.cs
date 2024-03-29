using System;
using System.Collections.Generic;

public static class List_T_FunctionalExtensions {

	public static void ForEach<T> ( this IEnumerable<T> collection, Action<T> action ) {

		foreach ( var element in collection ) {
			action( element );
		}
	}

	public static bool IsEmpty<T> ( this IList<T> collection ) {

		return ( collection.Count == 0 );
	}
	public static int LastIndex<T> ( this IList<T> collection ) {

		return ( collection.Count == 0 ) ? throw new IndexOutOfRangeException() : collection.Count - 1;
	}
	public static T First<T> ( this IList<T> collection ) {

		return ( collection.Count == 0 ) ? throw new IndexOutOfRangeException() : collection[0];
	}
	public static T Last<T> ( this IList<T> collection ) {

		return ( collection.Count == 0 ) ? throw new IndexOutOfRangeException() : collection[collection.Count - 1];
	}
	public static List<List<T>> Subdivide<T> ( this List<T> list, int maxLength ) {

		var results = new List<List<T>>();
		int position = 0;
		while ( position < list.Count ) {
			var end = ( position + maxLength ).WithCeiling( list.Count );
			results.Add( new List<T>( list.GetRange( position, end - position ) ) );
			position = end;
		}
		return results;
	}
	public static List<T> Map<T> ( this IList<T> list, Func<T, T> modify ) {

		var results = new List<T>();
		foreach ( var element in list ) {
			results.Add( modify( element ) );
		}
		return results;
	}
	public static List<T> Filter<T> ( this IList<T> list, Func<T, bool> compare ) {

		var results = new List<T>();
		foreach ( var element in list ) {
			if ( compare( element ) ) {
				results.Add( element );
			}
		}
		return results;
	}
	public static T Reduce<T> ( this IList<T> list, T startValue, Func<T, T, T> accumulate ) {

		var value = startValue;
		foreach ( var element in list ) {
			value = accumulate( value, element );
		}
		return value;
	}
	public static U Reduce<T, U> ( this IList<T> list, U startValue, Func<U, T, U> accumulate ) {

		var value = startValue;
		foreach ( var element in list ) {
			value = accumulate( value, element );
		}
		return value;
	}
	public static List<U> Convert<T, U> ( this IList<T> list, Func<T, U> convert ) {

		var results = new List<U>();
		foreach ( var element in list ) {
			results.Add( convert( element ) );
		}
		return results;
	}
	public static List<U> Convert<T, U> ( this IEnumerable<T> list, Func<T, U> convert ) {

		var results = new List<U>();
		foreach ( var element in list ) {
			results.Add( convert( element ) );
		}
		return results;
	}
}

public static class HashSet_T_FunctionalExtensions {

	public static HashSet<T> Map<T> ( this HashSet<T> set, Func<T, T> modify ) {

		var results = new HashSet<T>();
		foreach ( var element in set ) {
			results.Add( modify( element ) );
		}
		return results;
	}
	public static HashSet<T> Filter<T> ( this HashSet<T> set, Func<T, bool> evaluate ) {

		var results = new HashSet<T>();
		foreach ( var element in set ) {
			if ( evaluate( element ) ) {
				results.Add( element );
			}
		}
		return results;
	}
	public static HashSet<U> Convert<T, U> ( this HashSet<T> list, Func<T, U> convert ) {

		var results = new HashSet<U>();
		foreach ( var element in list ) {
			results.Add( convert( element ) );
		}
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

	public static void ForEach<K, V> ( this IReadOnlyDictionary<K, V> dictionary, Action<K, V> action ) {

		foreach ( var kvp in dictionary ) {
			action( kvp.Key, kvp.Value );
		}
	}

	public static List<T> ConvertToList<K, V, T> ( this IReadOnlyDictionary<K, V> dictionary, Func<K, V, T> converter ) {

		var results = new List<T>();
		dictionary.ForEach( ( k, v ) => results.Add( converter( k, v ) ) );
		return results;
	}

	public static void EnsureValue<K, V> ( this IDictionary<K, V> dictionary, K key, Func<V> createValue ) {

		if ( dictionary.TryGetValue( key, out var value ) ) {
			if ( value != null ) {
				return;
			}
		}
		dictionary[key] = createValue();
	}

	public static bool KeyIsUnique<K, V> ( this IReadOnlyDictionary<K, V> dictionary, K key ) {

		return !dictionary.ContainsKey( key );
	}

	public static void CopyToSerializableList<K, V> ( this IReadOnlyDictionary<K, V> dictionary, List<SerializableKeyValuePair<K, V>> list ) {

		list.Clear();
		foreach ( var pair in dictionary ) {
			list.Add( new SerializableKeyValuePair<K, V> { Key = pair.Key, Value = pair.Value } );
		}
	}

	public static void CopyToDictionary<K, V> ( this List<SerializableKeyValuePair<K, V>> list, IDictionary<K, V> dictionary ) {

		dictionary.Clear();
		foreach ( var entry in list ) {
			dictionary.Add( entry.Key, entry.Value );
		}
	}
}


