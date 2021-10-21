using System;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Jakintosh.Data {

	public enum TypePrimitives {
		// ??? = 0x00,
		Bool = 0x01,
		UInt8 = 0x02,
		UInt16 = 0x03,
		UInt32 = 0x04,
		UInt64 = 0x05,
		Int8 = 0x06,
		Int16 = 0x07,
		Int32 = 0x08,
		Int64 = 0x09,
		Float32 = 0x0A,
		Float64 = 0x0B,
		String = 0x0C,
		// ============
		External = 0xFF
	}

	public class TypeBuilder {

		private List<(byte[] name, byte[] type)> _fields;
		private Dictionary<string, int> _externalTypes;

		public TypeBuilder () {
			_fields = new List<(byte[] name, byte[] type)>();
			_externalTypes = new Dictionary<string, int>();
		}

		public TypeBuilder AddField ( string name, TypePrimitives primitive ) {

			var normalizedFieldName = name.Normalize( NormalizationForm.FormC );
			var nameBytes = Encoding.UTF8.GetBytes( normalizedFieldName );
			_fields.Add( (nameBytes, new[] { (byte)primitive }) );
			return this;
		}
		public TypeBuilder AddField ( string name, string externalTypeHash ) {

			// if unseen external type, track it
			if ( !_externalTypes.ContainsKey( externalTypeHash ) ) {
				_externalTypes.Add( externalTypeHash, _externalTypes.Count );
			}

			var normalizedFieldName = name.Normalize( NormalizationForm.FormC );
			var nameBytes = Encoding.UTF8.GetBytes( normalizedFieldName );
			_fields.Add( (nameBytes, new[] { (byte)0xFF, (byte)_externalTypes[externalTypeHash] }) );
			return this;
		}
		public byte[] Build () {

			var bytes = new List<byte>();

			// external type array
			var externalTypesArray = new byte[32 * _externalTypes.Count];
			_externalTypes.ForEach( pair => {
				var index = pair.Value;
				var hashBytes = Convert.FromBase64String( pair.Key );
				hashBytes.CopyTo( externalTypesArray, index * 32 );
			} );
			if ( _externalTypes.Count > 255 ) { /* error */ }
			bytes.Add( (byte)_externalTypes.Count );
			bytes.AddRange( externalTypesArray );

			// fields
			_fields.Sort( ( a, b ) => CompareByteArrays( a.name, b.name ) ); // sort by name for determinism
			_fields.ForEach( field => {
				if ( field.name.Length > 255 ) { /* error */ }
				bytes.Add( (byte)field.name.Length );   // name length prefix
				bytes.AddRange( field.name );           // name bytes
				bytes.AddRange( field.type );           // type id bytes
			} );

			return bytes.ToArray();
		}
		public string Hash () {

			var bytes = Build();
			return Hasher.HashBytesToBase64String( bytes );
		}

		public void Import ( byte[] typeDef ) {

			int currentByte = 0;

			// load external types
			var numExternalTypes = (int)typeDef[currentByte++];
			for ( int i = 0; i < numExternalTypes; i++ ) {
				var hash = typeDef.GetRange( index: currentByte, count: 32 ).ToArray();
				var str = Convert.ToBase64String( hash );
				_externalTypes.Add( str, i );
				currentByte += 32;
			}

			// load fields
			while ( currentByte < typeDef.Length ) {
				var nameLength = typeDef[currentByte++];
				var nameBytes = typeDef.GetRange( index: currentByte, count: nameLength ).ToArray();
				currentByte += nameLength;
				var type = new List<byte>() { typeDef[currentByte++] };
				if ( (TypePrimitives)type[0] == TypePrimitives.External ) {
					var externalIndex = typeDef[currentByte++];
					type.Add( externalIndex );
				}
				_fields.Add( (name: nameBytes, type: type.ToArray()) );
			}
		}
		private int CompareByteArrays ( byte[] a, byte[] b ) {

			for ( int i = 0; i < a.Length; i++ ) {

				// b is empty; a > b
				if ( i >= b.Length ) {
					return 1;
				}

				if ( a[i] == b[i] ) {
					continue;
				} else {
					return a[i] < b[i] ? -1 : 1;
				}
			}

			// if we made it here, a is empty and both are equal so far

			// if lengths are the same, they are equal
			if ( a.Length == b.Length ) {
				return 0;
			}

			// otherwise, b is longer, meaning b is greater
			else {
				return 1;
			}
		}

		public override string ToString () {

			var sb = new StringBuilder();

			sb.AppendLine( $"hash: {Hash()}" );
			sb.AppendLine( $"bytes: {BitConverter.ToString( Build() )}\n" );

			sb.AppendLine( $"type definition" );
			sb.AppendLine( $"===============" );

			// external types
			var externalTypeArray = new string[_externalTypes.Count];
			_externalTypes.ForEach( ext => externalTypeArray[ext.Value] = ext.Key );
			sb.AppendLine( $"  external-types (count: {_externalTypes.Count})" );
			externalTypeArray.ForEach( ext => {
				sb.AppendLine( $"    - {ext}" );
			} );

			// fields
			sb.AppendLine( $"  fields" );
			_fields.Sort( ( a, b ) => CompareByteArrays( a.name, b.name ) ); // sort by name for determinism
			_fields.ForEach( field => {
				var name = Encoding.UTF8.GetString( field.name );
				var type = field.type.Length == 1 ? ( (TypePrimitives)field.type[0] ).ToString() : $"ext[{field.type[1]}] | {externalTypeArray[(int)field.type[1]]}";
				sb.AppendLine( $"    - {name}: {type}" );
			} );

			return sb.ToString();
		}


		static public TypeBuilder GenerateTypeBuilder ( Type type ) {

			// get cached type if exists
			if ( _hashForType.TryGetValue( type, out var hash ) ) {
				return _builtTypeForHash[hash];
			}

			// build type
			var builtType = new TypeBuilder();
			GetSerializableFields( type ).ForEach( field => {
				var name = field.Name.ToLower();
				var primitive = GetTypePrimitive( field.FieldType );
				if ( primitive == TypePrimitives.External ) {
					builtType.AddField( name, GetHashForType( field.FieldType ) );
				} else {
					builtType.AddField( name, primitive );
				}
			} );

			// cache type
			var typeHash = builtType.Hash();
			_hashForType.Add( type, typeHash );
			_builtTypeForHash.Add( typeHash, builtType );

			return builtType;
		}

		static readonly HashSet<Type> serializableUnityTypes = new HashSet<Type> {
			typeof(Color), typeof(Color32),
			typeof(Vector2), typeof(Vector3), typeof(Vector4),
			typeof(Quaternion),
			#if UNITY_2017_2_OR_NEWER
			typeof(Vector2Int), typeof(Vector3Int),
			#endif
		};
		static private bool IsSerializable ( Type type ) => serializableUnityTypes.Contains( type ) ? true : type.IsSerializable;
		static private bool AttributesExist ( MemberInfo info, Type attributeType ) => info.GetCustomAttributes( attributeType, true ).Length > 0;
		static private bool IsFieldSerializable ( FieldInfo info ) {

			if ( AttributesExist( info, typeof( NonSerializedAttribute ) ) ) { return false; }
			if ( info.Name.StartsWith( "<" ) ) { return false; }
			return IsSerializable( info.FieldType );
		}
		static private List<FieldInfo> GetSerializableFields ( Type type ) {

			var nodeFields = type.GetFields(
				BindingFlags.Instance |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.GetField |
				BindingFlags.SetField
			 );
			return nodeFields.Filter( field => IsFieldSerializable( field ) );
		}
		static private string GetHashForType ( Type type ) {

			if ( !_hashForType.ContainsKey( type ) ) {
				GenerateTypeBuilder( type );
			}
			return _hashForType[type];
		}
		static TypePrimitives GetTypePrimitive ( Type type ) {

			return true switch {
				_ when type == typeof( bool ) => TypePrimitives.Bool,

				_ when type == typeof( byte ) => TypePrimitives.Int8,
				_ when type == typeof( short ) => TypePrimitives.Int16,
				_ when type == typeof( int ) => TypePrimitives.Int32,
				_ when type == typeof( long ) => TypePrimitives.Int64,

				_ when type == typeof( float ) => TypePrimitives.Float32,
				_ when type == typeof( double ) => TypePrimitives.Float64,
				_ when type == typeof( string ) => TypePrimitives.String,

				_ => TypePrimitives.External
			};
		}

		static Dictionary<Type, string> _hashForType = new Dictionary<Type, string>();
		static Dictionary<string, TypeBuilder> _builtTypeForHash = new Dictionary<string, TypeBuilder>();
	}
}