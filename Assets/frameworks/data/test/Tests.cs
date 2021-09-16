using Jakintosh.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

public class AddressableData_Tests {

	[Serializable]
	public class Person :
		IBytesSerializable,
		IDuplicatable<Person>,
		IUpdatable<Person> {

		// interfaces
		public byte[] GetSerializedBytes ()
			=> Serializer.GetSerializedBytes( this );
		public UnityEvent<Person> OnUpdated
			=> _onUpdated;
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
				_onUpdated?.Invoke( this );
			}
		}
		public int Age {
			get => _age;
			set {
				if ( _age == value ) return;
				_age = value;
				_onUpdated?.Invoke( this );
			}
		}
		public string Quote {
			get => _quote;
			set {
				if ( _quote == value ) return;
				_quote = value;
				_onUpdated?.Invoke( this );
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

		Deleting data:

		Delete data via Drop()
			- expect data at address is deleted
			- expect events no longer fire on old refs
			- (what happens to old refs? maybe they need invalidate event)

		Other:

		Do subscriptions carry over to new addresses? They probably should? But
		right now versioning isn't really built in
	*/

	private AddressableData<Person> GetPeople () => new AddressableData<Person>();

	[Test]
	public void AddressableData_Get_ReturnsDataForTempAddress () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		var person = people.GetCopy( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_Get_ReturnsDataForContentAddress () {

		var people = GetPeople();
		var contentAddress = people.Commit( new Person() );
		var person = people.GetCopy( contentAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_Get_ReturnsNullAfterDrop () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		people.Drop( mutablePersonAddress );

		var person = people.GetCopy( mutablePersonAddress );
		Assert.Null( person );
	}

	[Test]
	public void AddressableData_GetLatest_ReturnsDataForTempAddress () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		var person = people.GetLatestCopy( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetLatest_ReturnsDataForContentAddress () {

		var people = GetPeople();
		var contentAddress = people.Commit( new Person() );
		var person = people.GetLatestCopy( contentAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetLatest_ReturnsDataForForkedContent () {

		var people = GetPeople();
		var contentAddress = people.Commit( new Person() );
		var forkedAddress = people.Fork( contentAddress );

		var personFromContent = people.GetLatestCopy( contentAddress );
		Assert.NotNull( personFromContent );

		var personFromFork = people.GetLatestCopy( forkedAddress );
		Assert.NotNull( personFromFork );
	}

	[Test]
	public void AddressableData_GetMutable_ReturnsDataForTempAddress () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		var person = people.GetMutable( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetMutable_ReturnsNullAfterDrop () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		people.Drop( mutablePersonAddress );

		var person = people.GetMutable( mutablePersonAddress );
		Assert.Null( person );
	}

	[Test]
	public void AddressableData_GetLatestMutable_ReturnsDataForTempAddress () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		var person = people.GetLatestMutable( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_GetLatestMutable_ReturnsNullForContentAddress () {

		var people = GetPeople();
		var contentAddress = people.Commit( new Person() );
		var success = people.GetLatestMutable( contentAddress, out var person );
		Assert.False( success );
		Assert.Null( person );
	}

	[Test]
	public void AddressableData_GetLatestMutable_ReturnsDataForForkedContent () {

		var people = GetPeople();
		var contentAddress = people.Commit( new Person() );
		var forkedAddress = people.Fork( contentAddress );

		var success = people.GetLatestMutable( contentAddress, out var personFromContent );
		Assert.True( success );
		Assert.NotNull( personFromContent );

		var personFromFork = people.GetLatestMutable( forkedAddress );
		Assert.NotNull( personFromFork );
	}

	[Test]
	public void AddressableData_New_DataIsCreated () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		var person = people.GetCopy( mutablePersonAddress );
		Assert.NotNull( person );
	}

	[Test]
	public void AddressableData_New_CreatesProperAddress () {

		var people = GetPeople();
		var mutablePersonAddress = people.New();
		Assert.NotNull( mutablePersonAddress );
		Assert.NotNull( mutablePersonAddress.Identifier );
		Assert.Null( mutablePersonAddress.Parent );
	}

	[Test]
	public void AddressableData_New_EventsFire () {

		int eventFlag = 0;
		var people = GetPeople();
		var mutablePersonAddress = people.New();
		people.Subscribe( mutablePersonAddress, updatedPerson => {
			eventFlag++;
		} );
		var person = people.GetMutable( mutablePersonAddress );
		person.Name = "Jak";
		person.Age = 27;
		person.Name = "sic parvis magna";
		Assert.AreEqual( eventFlag, 3 );
	}

	[Test]
	public void AddressableData_Commit_DataIsCreated () {

		var people = GetPeople();

		// test with committing data
		var person = new Person();
		person.Name = "Test";
		person.Age = 10;
		person.Quote = "Quote";
		var contentAddress = people.Commit( person );
		var savedPerson = people.GetCopy( contentAddress );
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
		var savedTempPerson = people.GetCopy( tempPersonContentAddress );
		Assert.AreEqual( savedTempPerson.Name, "Test" );
		Assert.AreEqual( savedTempPerson.Age, 10 );
		Assert.AreEqual( savedTempPerson.Quote, "Quote" );
	}

	[Test]
	public void AddressableData_Commit_TempDataIsCleared () {

		var people = GetPeople();

		// test with committing from temp address
		var tempPersonAddress = people.New();
		var tempPerson = people.GetMutable( tempPersonAddress );
		tempPerson.Name = "Test";
		tempPerson.Age = 10;
		tempPerson.Quote = "Quote";
		var tempPersonContentAddress = people.Commit( tempPersonAddress );
		var clearedPerson = people.GetCopy( tempPersonAddress );
		Assert.Null( clearedPerson );
	}

	[Test]
	public void AddressableData_Commit_Fork_EventsFire () {

		var people = GetPeople();

		int contentEventFlag = 0;
		Action<Person> contentAddressHandler = person => contentEventFlag++;

		var tempEventFlags = 0;
		Action<Person> tempAddressHandler = person => tempEventFlags++;

		// commit data and subscribe
		var contentAddress = people.Commit( new Person() );
		people.Subscribe( contentAddress, contentAddressHandler );

		// fork data and subscribe
		var tempPersonAddress = people.Fork( contentAddress );
		people.Subscribe( tempPersonAddress, tempAddressHandler );

		// make sure both events fire
		var tempPerson = people.GetMutable( tempPersonAddress );
		tempPerson.Name = "Test";
		tempPerson.Age = 10;
		tempPerson.Quote = "Quote";
		Assert.AreEqual( contentEventFlag, 3 );
		Assert.AreEqual( tempEventFlags, 3 );

		// make sure unsubscribe from content address works
		people.Unsubscribe( contentAddress, contentAddressHandler );
		tempPerson.Name = "Test 2";
		Assert.AreEqual( contentEventFlag, 3 );
		Assert.AreEqual( tempEventFlags, 4 );

		// make sure unsubscribe from temp address works
		people.Unsubscribe( tempPersonAddress, tempAddressHandler );
		tempPerson.Name = "Test 3";
		Assert.AreEqual( contentEventFlag, 3 );
		Assert.AreEqual( tempEventFlags, 4 );

		// resubscribe to temp, commit, change
		people.Subscribe( tempPersonAddress, tempAddressHandler );
		var contentAddressV2 = people.Commit( tempPersonAddress );
		var tempAddressV2 = people.Fork( contentAddress );
		var tempPerson2 = people.GetLatestMutable( tempAddressV2 );
		tempPerson2.Name = "Test 4";
		Assert.AreEqual( contentEventFlag, 3 );
		Assert.AreEqual( tempEventFlags, 4 );
	}

	[Test]
	public void AddressableData_Fork_CreatesProperAddress () {

		var people = GetPeople();
		var contentAddress = people.Commit( new Person() );
		var forkedAddress = people.Fork( contentAddress );

		Assert.NotNull( forkedAddress );
		Assert.NotNull( forkedAddress.Identifier );
		Assert.AreEqual( forkedAddress.Parent, contentAddress.Identifier );
	}

}


public class DiffableData_Tests {

	[Serializable]
	public class Item :
		IBytesSerializable,
		IDiffable<Item, ItemDiff>,
		IDuplicatable<Item>,
		IUpdatable<Item> {

		public string Name {
			get => _name;
			set {
				if ( _name == value ) { return; }
				_name = value;
				_onUpdated?.Invoke( this );
			}
		}
		public int InventoryId {
			get => _inventoryId;
			set {
				if ( _inventoryId == value ) { return; }
				_inventoryId = value;
				_onUpdated?.Invoke( this );
			}
		}
		public float Price {
			get => _price;
			set {
				if ( _price == value ) { return; }
				_price = value;
				_onUpdated?.Invoke( this );
			}
		}

		public byte[] GetSerializedBytes ()
			=> Serializer.GetSerializedBytes( this );
		public ItemDiff Diff ( Item from )
			=> new ItemDiff() {
				NameDiff = DiffUtil.CreateStringDiff( from.Name, Name ),
				InventoryIdDiff = _inventoryId - from.InventoryId,
				PriceChange = _price - from.Price
			};
		public Item Apply ( ItemDiff diff )
			=> new Item(
				name: diff.NameDiff.ApplyTo( Name ),
				id: this._inventoryId + diff.InventoryIdDiff,
				price: this.Price + diff.PriceChange
			);
		public Item Duplicate ()
			=> new Item(
				name: _name,
				id: _inventoryId,
				price: _price
			);
		public UnityEvent<Item> OnUpdated
			=> _onUpdated;

		public Item () { }
		public Item ( string name, int id, float price ) {
			_name = name;
			_inventoryId = id;
			_price = price;
		}

		// serialized data
		private string _name;
		private int _inventoryId;
		private float _price;

		// runtime data
		[NonSerialized] private UnityEvent<Item> _onUpdated = new UnityEvent<Item>();
	}

	[Serializable]
	public class ItemDiff : IBytesSerializable {

		public StringDiff NameDiff;
		public int InventoryIdDiff;
		public float PriceChange;

		public byte[] GetSerializedBytes ()
			=> Serializer.GetSerializedBytes( this );

		public override string ToString () => $"NameDiff: {NameDiff}; IdDiff: {InventoryIdDiff}; PriceChange: {PriceChange}";
	}

	private AddressableData<Item> GetInventory () => new AddressableData<Item>();
	private DiffableData<Item, ItemDiff> GetInventoryDeltas ( AddressableData<Item> inventory ) => new DiffableData<Item, ItemDiff>( inventory );

	private Item GetItem () => new Item( name: "Part", id: 001, price: 4.99f );

	[Test]
	public void DiffableData_Commit () {

		var inventory = GetInventory();
		var inventoryDeltas = GetInventoryDeltas( inventory );
		var item = GetItem();

		// create delta
		var deltaAdress = inventoryDeltas.Commit( item, "" );
		Assert.NotNull( deltaAdress );

		// check delta
		var delta = inventoryDeltas.GetDelta( deltaAdress );
		Assert.AreEqual( delta.Diff.InventoryIdDiff, 1 );
		Assert.AreEqual( delta.Diff.PriceChange, 4.99f );
		Assert.AreEqual( delta.Author, "" );
		Assert.AreEqual( delta.Previous, null );

		// check data
		var data = inventoryDeltas.GetData( deltaAdress );
		Assert.AreEqual( data.InventoryId, 1 );
		Assert.AreEqual( data.Price, 4.99f );

		// check data exists in content
		var dataHash = Hasher.HashDataToBase64String( data );
		data = inventory.GetCopy( new Address( dataHash ) );
		Assert.NotNull( data );

		// change item
		item.Name = "Darts";
		item.InventoryId += 1;
		item.Price += 1;

		// update chain
		var deltaAdress2 = inventoryDeltas.Commit( item, "", compile: false, previousDeltaAddress: deltaAdress );
		delta = inventoryDeltas.GetDelta( deltaAdress2 );
		Assert.AreEqual(
			expected: new StringDiff( new List<StringDiffOperation>() {
				new StringDiffOperation( DiffOperationTypes.Addition, 4, 0, "s"),
				new StringDiffOperation( DiffOperationTypes.Addition, 1, 0, "D"),
				new StringDiffOperation( DiffOperationTypes.Removal, 0, 1, null)
			} ),
			actual: delta.Diff.NameDiff
		);
		Assert.AreEqual( delta.Diff.InventoryIdDiff, 1 );
		Assert.AreEqual( delta.Diff.PriceChange, 1 );
		Assert.AreEqual( delta.Author, "" );
		Assert.AreEqual( delta.Previous, deltaAdress.Identifier );
	}
}