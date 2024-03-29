using Newtonsoft.Json;

namespace Jakintosh.Graph {

	internal interface IEditableRelationType {
		void SetName ( string name );
	}

	public class RelationType : IdentifiableResource<string, RelationType>, IEditableRelationType {

		[JsonProperty] public string Name { get; private set; }

		public RelationType ( string uid, string name ) : base( uid ) {

			Name = name;
		}

		public override string ToString () {
			return $"Graph.RelationType {{\n    uid: {Identifier},\n    Name: {Name}\n}}";
		}

		// ********** IEditableRelationshipType **********

		void IEditableRelationType.SetName ( string name ) => Name = name;
	}
}