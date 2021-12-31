using Newtonsoft.Json;
using SouthPointe.Serialization.MessagePack;
using System;
using System.IO;
using UnityEngine;

namespace Framework.Data {

	public static class PersistentStore {

		// TODO: Rewrite this with async in mind


		// ******** Exception Definitions ********

		public class FileNotFoundException : Exception { }
		public class InvalidPathException : Exception { }
		public class InvalidJsonException : Exception { }
		public class InvalidMoveSourceException : Exception { }
		public class MoveDestinationConflictException : Exception { }


		// ********** Public Interface **********

		public static bool IsLoggingEnabled { get; set; } = true;

		public static void WriteToJson_Throws<T> ( string path, T data ) {

			// get json and bytes
			var json = JsonConvert.SerializeObject( data );
			var bytes = System.Text.Encoding.UTF8.GetBytes( json );

			// write out the actual file
			WriteBytes_Throws( path, bytes );
		}
		public static void WriteToMsgPackBytes_Throws<T> ( string path, T data ) {

			// get bytes
			var formatter = new MessagePackFormatter();
			var bytes = formatter.Serialize( data );

			// write out the actual file
			WriteBytes_Throws( path, bytes );
		}
		public static void WriteBytes_Throws ( string path, byte[] bytes ) {

			// parse path
			// if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: WriteBytes_Throws.path = {path}" ); }
			var fullpath = FullPath( path );
			// if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: WriteBytes_Throws.fullpath = {fullpath}" ); }
			var lastSlash = fullpath.LastIndexOf( '/' );
			if ( lastSlash == -1 ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't save, path is invalid." ); }
				throw new InvalidPathException();
			}

			// make sure the directory we are saving to exists
			EnsureDirectory( fullpath.Substring( 0, lastSlash ) );

			// write bytes
			File.WriteAllBytes( fullpath, bytes );
		}

		public static T LoadFromJson_Throws<T> ( string path ) where T : new() {

			string json;

			try {
				json = LoadJson_Throws( path );
			} catch ( FileNotFoundException ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't load, file not found at path." ); }
				throw;
			}

			if ( json.IsNullOrEmpty() ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't load, json is null." ); }
				throw new InvalidJsonException();
			}

			return JsonConvert.DeserializeObject<T>( json );
		}
		public static void LoadFromJsonInto_Throws<T> ( string path, T obj ) where T : class {

			string json;

			try {
				json = LoadJson_Throws( path );
			} catch ( FileNotFoundException ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't load, file not found at path." ); }
				throw;
			}

			if ( json.IsNullOrEmpty() ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't load into {obj}, json is null." ); }
				throw new InvalidJsonException();
			}

			JsonConvert.PopulateObject( json, obj );
		}
		public static T LoadFromMsgPackBytes_Throws<T> ( string path ) where T : new() {

			var bytes = LoadBytes_Throws( path );
			var formatter = new MessagePackFormatter();

			return formatter.Deserialize<T>( bytes );
		}
		public static byte[] LoadBytes_Throws ( string path ) {

			var fullpath = FullPath( path );
			if ( !File.Exists( fullpath ) ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: File does not exist at {fullpath}" ); }
				throw new FileNotFoundException();
			}

			if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Loading from {fullpath}" ); }

			return File.ReadAllBytes( fullpath );
		}

		public static void Move_Throws ( string path, string newPath ) {

			var sourcePath = FullPath( path );
			if ( !File.Exists( sourcePath ) ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't move file, source doesn't exist." ); }
				throw new InvalidMoveSourceException();
			}

			var destPath = FullPath( newPath );
			if ( File.Exists( destPath ) ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Couldn't move file, destination has conflict." ); }
				throw new MoveDestinationConflictException();
			}

			File.Move( sourcePath, destPath );
		}
		public static void Delete_Throws ( string path ) {

			var fullPath = FullPath( path );
			if ( !File.Exists( fullPath ) ) {
				if ( IsLoggingEnabled ) { Debug.LogError( $"Framework.Data.PersistentStore: Couldn't delete file because file doesn't exist at path {{{fullPath}}}." ); }
				throw new FileNotFoundException();
			}

			File.Delete( fullPath );
		}

		// ********** Private Interface **********

		private static string FullPath ( string subpath )
			=> $"{UnityEngine.Application.persistentDataPath}{subpath}";
		private static void EnsureDirectory ( string path ) {

			if ( !Directory.Exists( path ) ) {
				if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Path does not exist at: {path}" ); }
				var lastSlash = path.LastIndexOf( '/' );
				if ( lastSlash != -1 ) {
					var subPath = path.Substring( 0, lastSlash );
					EnsureDirectory( subPath );
					if ( IsLoggingEnabled ) { Debug.Log( $"Framework.Data.PersistentStore: Creating Directory at: {path}" ); }
					Directory.CreateDirectory( path );
				} else {
					return;
				}
			}
		}
		private static string LoadJson_Throws ( string path ) {

			var bytes = LoadBytes_Throws( path );
			var json = System.Text.Encoding.UTF8.GetString( bytes );
			return json;
		}

	}
}