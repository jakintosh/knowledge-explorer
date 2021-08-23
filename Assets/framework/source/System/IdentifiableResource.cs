using Newtonsoft.Json;
using System;

public interface IIdentifiable<T> {
	T Identifier { get; }
}
public interface IIdentifiableLink<T> {
	T LinkedIdentifier { get; }
	void Link ( IIdentifiable<T> identifiable );
}

public abstract class IdentifiableResource<TIdentifier, TResource> :
	IIdentifiable<TIdentifier>,
	IEquatable<TResource>
	where TResource : IdentifiableResource<TIdentifier, TResource> {

	// *********** Public Interface ***********

	public TIdentifier Identifier => uid;
	public IdentifiableResource ( TIdentifier uid ) => this.uid = uid;


	// ********** Private Interface ***********

	[JsonProperty] private TIdentifier uid = default( TIdentifier );


	// *** IEquatable<IdentifiableResource> ***

	public bool Equals ( TResource other ) =>
		other?.uid.Equals( uid ) ?? false;


	// ********** Equality Overrides **********

	public override bool Equals ( object obj ) =>
		obj is IdentifiableResource<TIdentifier, TResource> ?
			this.Equals( obj as IdentifiableResource<TIdentifier, TResource> ) :
			false;
	public override int GetHashCode () =>
		uid.GetHashCode();
}