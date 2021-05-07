using Newtonsoft.Json;
using System;

public abstract class IdentifiableResource<T> : IEquatable<T>
	where T : IdentifiableResource<T> {

	// *********** Public Interface ***********

	public string UID => uid;
	public IdentifiableResource ( string uid ) => this.uid = uid;


	// ********** Private Interface ***********

	[JsonProperty] private string uid = null;


	// *** IEquatable<IdentifiableResource> ***

	public bool Equals ( T other ) =>
		other?.uid.Equals( uid ) ?? false;


	// ********** Equality Overrides **********

	public override bool Equals ( object obj ) =>
		obj is IdentifiableResource<T> ?
			this.Equals( obj as IdentifiableResource<T> ) :
			false;
	public override int GetHashCode () =>
		uid.GetHashCode();
}