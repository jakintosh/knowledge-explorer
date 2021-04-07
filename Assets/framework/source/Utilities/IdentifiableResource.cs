using Newtonsoft.Json;
using System;

public class IdentifiableResource : IEquatable<IdentifiableResource> {

	// *********** Public Interface ***********

	public IdentifiableResource ( string uid ) => this.uid = uid;

	// ********** Private Interface ***********

	[JsonProperty] private string uid = null;


	// *** IEquatable<IdentifiableResource> ***

	public bool Equals ( IdentifiableResource other ) =>
		other?.uid.Equals( uid ) ?? false;

	// ********** Equality Overrides **********

	public override bool Equals ( object obj ) =>
		obj is IdentifiableResource ?
			this.Equals( obj as IdentifiableResource ) :
			false;
	public override int GetHashCode () =>
		uid.GetHashCode();
}