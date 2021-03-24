using System;
using System.Text;
using UnityEngine;

namespace StringHelpers {

	public static class UID {

		private static char[] ID_CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

		public static string Generate ( int length, Func<string, bool> validateUniqueness ) {

			var random = new System.Random( (int)( Time.time * 1000 ) );
			var sb = new StringBuilder( capacity: length );
			string id;
			do {
				sb.Clear();
				for ( int i = 0; i < length; i++ ) {
					var c = ID_CHARS[random.Next( ID_CHARS.Length )];
					sb.Append( c );
				}
				id = sb.ToString();
			} while ( !validateUniqueness( id ) );
			return id;
		}
	}

	public static class IncrementedString {

		public static string Generate ( string baseString, Func<string, bool> validateUniqueness ) {

			int index = 0;
			var titleBase = baseString;
			var title = titleBase;
			while ( !validateUniqueness( title ) ) {
				title = $"{titleBase}{++index}";
			}
			return title;
		}
	}
}
