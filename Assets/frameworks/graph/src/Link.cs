using Newtonsoft.Json;
using System;

namespace Graph {

	internal interface IEditableLink {
		void SetTypeUID ( string typeUID );
	}

	public class Link : IdentifiableResource<Link>,
		IEditableLink {

		[JsonProperty] public string TypeUID { get; private set; }
		[JsonProperty] public string FromUID { get; private set; }
		[JsonProperty] public string ToUID { get; private set; }

		public Link ( string uid, string typeUID, string fromUID, string toUID ) : base( uid ) {

			TypeUID = typeUID;
			FromUID = fromUID;
			ToUID = toUID;
		}


		// ********** IEditableLink **********

		void IEditableLink.SetTypeUID ( string typeUID ) => TypeUID = typeUID;
	}
}