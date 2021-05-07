using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Graph {

	// ********** Base Node **********

	public class Node : IdentifiableResource<Node> {

		[JsonProperty] public virtual NodeDataTypes Type => NodeDataTypes.Node;
		[JsonProperty] public List<string> LinkUIDs { get; private set; }
		[JsonProperty] public List<string> BacklinkUIDs { get; private set; }

		public Node ( string uid ) : base( uid ) {

			LinkUIDs = new List<string>();
			BacklinkUIDs = new List<string>();
		}
	}


	// ********** Data Node **********

	internal interface IEditableNode<T> {
		void SetValue ( T value );
	}
	public class Node<T> : Node,
		IEditableNode<T> {

		[JsonProperty] public override NodeDataTypes Type => _dataType;
		[JsonProperty] public T Value { get; private set; }

		public Node ( string uid, T value, NodeDataTypes dataType ) : base( uid ) {

			_dataType = dataType;
			Value = value;
		}

		private NodeDataTypes _dataType;


		// ********** IEditableNode<T> **********

		void IEditableNode<T>.SetValue ( T value ) => Value = value;
	}


	// ***** Custom Serialization Converter *****

	public class NodeConverter : JsonConverter {

		public override bool CanConvert ( Type objectType ) => typeof( Node ).IsAssignableFrom( objectType );

		public override bool CanRead => true;
		public override object ReadJson ( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer ) {

			var jObject = JObject.Load( reader );
			var nodeType = (NodeDataTypes)jObject["Type"].Value<int>();
			var node = nodeType switch {
				NodeDataTypes.String => new Node<string>( "", "", NodeDataTypes.String ),
				NodeDataTypes.Integer => new Node<int>( "", 0, NodeDataTypes.Integer ),
				_ => new Node( "" )
			};
			serializer.Populate( jObject.CreateReader(), node );

			return node;
		}

		public override bool CanWrite => false;
		public override void WriteJson ( JsonWriter writer, object value, JsonSerializer serializer ) => throw new NotImplementedException();
	}

}