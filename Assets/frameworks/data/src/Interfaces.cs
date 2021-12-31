using SouthPointe.Serialization.MessagePack;
using System;
using System.Security.Cryptography;
using UnityEngine.Events;

namespace Jakintosh.Data {

	public interface IBytesSerializable {
		byte[] GetSerializedBytes ();
	}
	public interface IDiffable<T, TDiff> {
		TDiff Diff ( T from );
		T Apply ( TDiff diff );
	}
	public interface IDuplicatable<T> {
		T Duplicate ();
	}
	public interface IHashable {
		string GetBase64Hash ();
		byte[] GetBytesHash ();
	}
	public interface IReadOnlyConvertible<TInterface> {
		TInterface ToReadOnly ();
	}
	public interface IUpdatable<T> {
		UnityEvent<T> OnUpdated { get; }
	}

	public static class Serializer {

		public static byte[] GetSerializedBytes<T> ( T data ) {

			var formatter = new MessagePackFormatter();
			var bytes = formatter.Serialize<T>( data );
			return bytes;
		}
		public static T DeserializeBytes<T> ( byte[] bytes ) where T : new() {

			var formatter = new MessagePackFormatter();
			var data = formatter.Deserialize<T>( bytes );
			return data;
		}
	}
	public static class Hasher {

		public static byte[] HashData<T> ( T data ) where T : IBytesSerializable {

			var bytes = data.GetSerializedBytes();
			return HashBytes( bytes );
		}
		public static byte[] HashBytes ( params byte[][] byteArrays ) {

			// combine byte arrays into one
			var length = byteArrays.Reduce( startValue: 0, ( totalBytes, byteArray ) => totalBytes + byteArray.Length );
			var bytes = new byte[length];
			var offset = 0;
			foreach ( var array in byteArrays ) {
				Buffer.BlockCopy(
					src: array,
					srcOffset: 0,
					dst: bytes,
					dstOffset: offset,
					count: array.Length
				);
				offset += array.Length;
			}

			var sha256 = new SHA256CryptoServiceProvider();
			var hash = sha256.ComputeHash( bytes );
			return hash;
		}
		public static string HashBytesToBase64String ( byte[] bytes ) {

			var hash = HashBytes( bytes );
			return ConvertHashToBase64String( hash );
		}
		public static string HashDataToBase64String<T> ( T data ) where T : IBytesSerializable {

			var bytes = data.GetSerializedBytes();
			return HashBytesToBase64String( bytes );
		}
		public static string ConvertHashToBase64String ( byte[] hash ) {

			return Convert.ToBase64String( hash );
		}
		public static byte[] ConvertBase64StringHashToBytes ( string hash ) {

			return Convert.FromBase64String( hash );
		}
	}
}