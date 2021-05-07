using Newtonsoft.Json;
using System;

namespace Graph {

	internal interface IEditableRelationType {
		void SetName ( string name );
	}

	public class RelationType : IdentifiableResource<RelationType>,
		IEditableRelationType {

		[JsonProperty] public string Name { get; private set; }

		public RelationType ( string uid, string name ) : base( uid ) {

			Name = name;
		}


		// ********** IEditableRelationshipType **********

		void IEditableRelationType.SetName ( string name ) => Name = name;
	}
}