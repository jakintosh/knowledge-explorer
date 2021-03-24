using System.Text;

namespace Library.Model {

	public class Content {

		// data
		public string UserEditableString; // what the user types to edit
		public string ModelString; // the data model version of the string

		public Content ( string userString ) {

			UserEditableString = userString;
			ModelString = ConvertUserEditableStringToDataModel( UserEditableString );
		}

		// helpers
		private static string ConvertUserEditableStringToDataModel ( string userEditableString ) {

			// "This is a [[string]] that has a [[link]]." => "This is a $0123abc that has a $4567def."

			if ( string.IsNullOrEmpty( userEditableString ) ) {
				return "";
			}

			var modelSB = new StringBuilder();
			for ( int i = 0; i < userEditableString.Length; i++ ) {

				var c = userEditableString[i];

				// look for link start
				if ( c == '[' && i + 1 < userEditableString.Length && userEditableString[i + 1] == '[' ) {

					var titleStringBuilder = new StringBuilder();
					var j = i + 2; // skip the brackets
					var isReadingTitle = true;
					do {

						// build link title
						var titleChar = userEditableString[j];

						// look for link end
						if ( titleChar == ']' && j + 1 < userEditableString.Length && userEditableString[j + 1] == ']' ) {
							isReadingTitle = false;
						} else {
							titleStringBuilder.Append( titleChar );
						}

						j++;

					} while ( isReadingTitle && j < userEditableString.Length );

					var title = titleStringBuilder.ToString();
					i = j;

					// if the string is not real abort
					if ( string.IsNullOrWhiteSpace( title ) ) {
						continue;
					}

					// append link ID in string
					// TODO: fix this
					// var id = Bucket.Instance.GetIDForTitle( title );
					// modelSB.Append( $"${id}" );

				} else {

					modelSB.Append( c );
				}
			}

			return modelSB.ToString();
		}
	}
}
