using System;

namespace SouthPointe.Serialization.MessagePack {

	static class CustomExtensions {

		internal static bool IsNullable ( this Type type ) {

			return type.IsValueType && Nullable.GetUnderlyingType( type ) != null;
		}
	}
}
