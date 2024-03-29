using Newtonsoft.Json;

namespace Jakintosh.Graph {

	internal interface IEditableLink {
		void SetTypeUID ( string typeUID );
	}

	public class Link : IdentifiableResource<string, Link>,
		IEditableLink {

		[JsonProperty] public string TypeUID { get; private set; }
		[JsonProperty] public string FromUID { get; private set; }
		[JsonProperty] public string ToUID { get; private set; }

		public Link ( string uid, string typeUID, string fromUID, string toUID ) : base( uid ) {

			TypeUID = typeUID;
			FromUID = fromUID;
			ToUID = toUID;
		}


		public override string ToString () {
			return $"Graph.Link {{\n    uid: {Identifier},\n    TypeUID: {TypeUID},\n    FromUID: {FromUID},\n    ToUID: {ToUID}\n}}";
		}

		// ********** IEditableLink **********

		void IEditableLink.SetTypeUID ( string typeUID ) => TypeUID = typeUID;
	}
}