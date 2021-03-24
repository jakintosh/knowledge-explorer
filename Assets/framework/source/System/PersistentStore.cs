using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Framework.Data {

	public static class PersistentStore {

		// TODO: Rewrite this with async in mind
		// TODO: Add exceptions and error handling

		// ********** Public Interface **********

		public static void Save<T> ( string path, T data ) {

			var fullpath = FullPath( path );

			var lastSlash = fullpath.LastIndexOf( '/' );
			if ( lastSlash == -1 ) {
				// there are no slashes, invalid
				return;
			}

			// make sure the directory we are saving to exists
			EnsureDirectory( fullpath.Substring( 0, lastSlash ) );

			// write out the actual file
			Debug.Log( $"Framework.Data.PersistentStore: Saving to {fullpath}" );
			// var json = JsonUtility.ToJson( data ); // old unity way
			var json = JsonConvert.SerializeObject( data );
			File.WriteAllText( fullpath, json );
		}
		public static T Load<T> ( string path ) where T : new() {

			var json = LoadJson( path );
			if ( string.IsNullOrEmpty( json ) ) {
				return new T();
			}
			// return JsonUtility.FromJson<T>( json ); // old unity way
			return JsonConvert.DeserializeObject<T>( json );
		}
		public static void LoadInto<T> ( string path, T obj ) where T : class {

			var json = LoadJson( path );
			// JsonUtility.FromJsonOverwrite( json, obj );
			if ( json == null ) {
				Debug.Log( $"Framework.Data.PersistentStore: Couldn't load into {obj}, json is null." );
				return;
			}
			JsonConvert.PopulateObject( json, obj );
		}
		public static void Delete ( string path ) {

			if ( File.Exists( path ) ) {
				File.Delete( path );
			}
		}

		// ********** Private Interface **********

		private static string FullPath ( string subpath ) => $"{UnityEngine.Application.persistentDataPath}{subpath}";
		private static void EnsureDirectory ( string path ) {

			if ( !Directory.Exists( path ) ) {
				Debug.Log( $"Framework.Data.PersistentStore: Path does not exist at: {path}" );
				var lastSlash = path.LastIndexOf( '/' );
				if ( lastSlash != -1 ) {
					var subPath = path.Substring( 0, lastSlash );
					EnsureDirectory( subPath );
					Debug.Log( $"Framework.Data.PersistentStore: Creating Directory at: {path}" );
					Directory.CreateDirectory( path );
				} else {
					return;
				}
			}
		}
		private static string LoadJson ( string path ) {

			var fullpath = FullPath( path );
			if ( !File.Exists( fullpath ) ) {
				Debug.Log( $"Framework.Data.PersistentStore: File does not exist at {fullpath}" );
				return null;
			}
			Debug.Log( $"Framework.Data.PersistentStore: Loading from {fullpath}" );
			return File.ReadAllText( fullpath );
		}
	}
}