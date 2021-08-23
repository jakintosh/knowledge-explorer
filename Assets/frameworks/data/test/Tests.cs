using Jakintosh.Data;
using NUnit.Framework;
using System;
using UnityEngine.Events;

public class Data_Tests {

	public interface IReadOnlyPerson {
		string Name { get; }
		int Age { get; }
		string Quote { get; }
	}

	[Serializable]
	public class Person : IReadOnlyPerson,
		IReadOnlyConvertible<IReadOnlyPerson>,
		IDuplicatable<Person>,
		IUpdatable<Person> {

		// interfaces
		public UnityEvent<Person> OnUpdated
			=> _onUpdated;
		public IReadOnlyPerson ToReadOnly ()
			=> this as IReadOnlyPerson;
		public Person Duplicate () {

			var duplicate = new Person();
			duplicate._name = this._name;
			duplicate._age = this._age;
			duplicate._quote = this._quote;
			return duplicate;
		}

		// properties
		public string Name {
			get => _name;
			set {
				if ( _name == value ) return;
				_name = value;
				OnUpdated?.Invoke( this );
			}
		}
		public int Age {
			get => _age;
			set {
				if ( _age == value ) return;
				_age = value;
				OnUpdated?.Invoke( this );
			}
		}
		public string Quote {
			get => _quote;
			set {
				if ( _quote == value ) return;
				_quote = value;
				OnUpdated?.Invoke( this );
			}
		}

		public override string ToString ()
			=> $"name: {_name}, age: {_age}, quote: \"{_quote}\"";

		// data
		private string _name = null;
		private int _age = -1;
		private string _quote = null;

		// runtime data
		[NonSerialized] private UnityEvent<Person> _onUpdated = new UnityEvent<Person>();
	}

	// ****************************************

	/*
		Creating new data:

		New temp data from New()
			- expect address is correct (parent == null)
			- expect data exists
			- expect events fire

		New static data from Commit()
			- expect address is correct
			- expect data exists
			- expect events fire
			- expect temp data is cleared

		New temp data from Fork()
			- expect address is correct (parent)
			- expect data exists
			- expect events fire

	*/

	[Test]
	public void AddressableData_New () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		Assert.NotNull( mutablePersonAddress );
		Assert.NotNull( mutablePersonAddress.Identifier );
		Assert.Null( mutablePersonAddress.Parent );
	}

	[Test]
	public void AddressableData_Get_ReturnsDataForTempAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		var person = people.Get( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_Get_ReturnsDataForContentAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var contentAddress = people.Commit( new Person() );
		var person = people.Get( contentAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_Get_ReturnsNullAfterDrop () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		people.Drop( mutablePersonAddress );

		var person = people.Get( mutablePersonAddress );
		Assert.Null( person );
	}

	[Test]
	public void AddressableData_GetLatest_ReturnsDataForTempAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		var person = people.GetLatest( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetLatest_ReturnsDataForContentAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var contentAddress = people.Commit( new Person() );
		var person = people.GetLatest( contentAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetLatest_ReturnsDataForForkedContent () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var contentAddress = people.Commit( new Person() );
		var forkedAddress = people.Fork( contentAddress );

		var personFromContent = people.GetLatest( contentAddress );
		Assert.NotNull( personFromContent );

		var personFromFork = people.GetLatest( forkedAddress );
		Assert.NotNull( personFromFork );
	}


	[Test]
	public void AddressableData_GetMutable_ReturnsDataForTempAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		var person = people.GetMutable( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetMutable_ReturnsNullAfterDrop () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		people.Drop( mutablePersonAddress );

		var person = people.GetMutable( mutablePersonAddress );
		Assert.Null( person );
	}

	[Test]
	public void AddressableData_GetLatestMutable_ReturnsDataForTempAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var mutablePersonAddress = people.New();
		var person = people.GetLatestMutable( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetLatestMutable_ReturnsNullForContentAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var contentAddress = people.Commit( new Person() );
		var success = people.GetLatestMutable( contentAddress, out var person );
		Assert.False( success );
		Assert.Null( person );
	}

	[Test]
	public void AddressableData_GetLatestMutable_ReturnsDataForForkedContent () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var contentAddress = people.Commit( new Person() );
		var forkedAddress = people.Fork( contentAddress );

		var success = people.GetLatestMutable( contentAddress, out var personFromContent );
		Assert.True( success );
		Assert.NotNull( personFromContent );

		var personFromFork = people.GetLatestMutable( forkedAddress );
		Assert.NotNull( personFromFork );
	}

	[Test]
	public void AddressableData_Fork_CreatesProperAddress () {

		var people = new AddressableData<Person, IReadOnlyPerson>();
		var contentAddress = people.Commit( new Person() );
		var forkedAddress = people.Fork( contentAddress );

		Assert.NotNull( forkedAddress );
		Assert.NotNull( forkedAddress.Identifier );
		Assert.AreEqual( forkedAddress.Parent, contentAddress.Identifier );
	}

	[Test]
	public void AddressableData_Commit_DataIsCreated () {

		var people = new AddressableData<Person, IReadOnlyPerson>();

		// test with committing data
		var person = new Person();
		person.Name = "Test";
		person.Age = 10;
		person.Quote = "Quote";
		var contentAddress = people.Commit( person );
		var savedPerson = people.Get( contentAddress );
		Assert.AreEqual( savedPerson.Name, "Test" );
		Assert.AreEqual( savedPerson.Age, 10 );
		Assert.AreEqual( savedPerson.Quote, "Quote" );

		// test with committing from temp address
		var tempPersonAddress = people.New();
		var tempPerson = people.GetMutable( tempPersonAddress );
		tempPerson.Name = "Test";
		tempPerson.Age = 10;
		tempPerson.Quote = "Quote";
		var tempPersonContentAddress = people.Commit( tempPersonAddress );
		var savedTempPerson = people.Get( tempPersonContentAddress );
		Assert.AreEqual( savedTempPerson.Name, "Test" );
		Assert.AreEqual( savedTempPerson.Age, 10 );
		Assert.AreEqual( savedTempPerson.Quote, "Quote" );
	}

	[Test]
	public void AddressableData_Commit_TempDataIsCleared () {

		var people = new AddressableData<Person, IReadOnlyPerson>();

		// test with committing from temp address
		var tempPersonAddress = people.New();
		var tempPerson = people.GetMutable( tempPersonAddress );
		tempPerson.Name = "Test";
		tempPerson.Age = 10;
		tempPerson.Quote = "Quote";
		var tempPersonContentAddress = people.Commit( tempPersonAddress );
		var clearedPerson = people.Get( tempPersonAddress );
		Assert.Null( clearedPerson );
	}


	// [Test]
	// public void AddressableData_Test () {


	// 	var people = new AddressableData<Person, IReadOnlyPerson>();

	// 	// create a new mutable person, and store its address
	// 	var mutablePersonAddress = people.New();
	// 	people.Subscribe( mutablePersonAddress, person => {

	// 	} );

	// 	// get the actual mutable person data
	// 	var mutablePerson = people.GetLatestMutable( mutablePersonAddress );
	// 	mutablePerson.Age = 27;
	// 	mutablePerson.Name = "Jak";
	// 	mutablePerson.Quote = "Sic parvis magna";

	// 	// commit temp person into static data, subscribe to changes on that
	// 	var staticAddress = people.Commit( mutablePersonAddress );
	// 	people.Subscribe( staticAddress, person => {

	// 	} );

	// 	// fork the static and get latest mutable from the static address
	// 	var mutablePersonAddressV2 = people.Fork( staticAddress );
	// 	if ( people.GetLatestMutable( staticAddress, out var mutablePersonV2 ) ) {
	// 		// this won't get called here, but lets you handle case where its not there
	// 	}

	// 	// these will fire events to the subscribe to Static person
	// 	mutablePersonV2.Name = "Daisy";
	// 	mutablePersonV2.Age = 25;
	// 	mutablePersonV2.Quote = "Mochi is cute";

	// 	// using "get" without mutable is always read only
	// 	var staticPerson = people.Get( staticAddress );
	// 	// staticPerson.Name = "Mochi";	 // Compile error

	// 	var readOnlyMutablePerson = people.Get( mutablePersonAddressV2 );
	// 	// readOnlyMutablePerson.Name = "Mochi";  // Compile error

	// 	var okayMutablePerson = people.GetMutable( mutablePersonAddressV2 );
	// 	okayMutablePerson.Name = "Mochi"; // okay, because "GetMutable"


	// 	// Assert.That( intFlag == true );
	// }
}