using Newtonsoft.Json;

namespace Knowledge.Metadata {

	public class RelationType {

		// ********** Constructor **********

		public RelationType () {

			Display = new DisplayProperties();
		}

		public override string ToString () => JsonConvert.SerializeObject( this );


		// ********** Fields **********

		[JsonProperty( propertyName: "display" )] public DisplayProperties Display { get; private set; }


		// ********** Types **********

		public class DisplayProperties {
			[JsonProperty( propertyName: "hexColor" )] public string HexColor { get; set; }

			public DisplayProperties () {
				HexColor = "#FF00FFFF";
			}
		}
	}
}