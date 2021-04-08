using Framework;
using NUnit.Framework;
using System.Collections.Generic;

public class Observable_Tests {


	// ****************************************

	private bool intFlag = false;

	[Test]
	public void Observable_Int_InitialCall () {

		// make sure initial firing works
		intFlag = false;
		var o = new Observable<int>(
			initialValue: 0,
			onChange: newValue => {
				intFlag = true;
			}
		);
		Assert.That( intFlag == true );
	}

	[Test]
	public void Observable_Int_ChangeCalls () {

		var o = new Observable<int>(
			initialValue: 0,
			onChange: newValue => {
				intFlag = true;
			}
		);

		// changing the value calls handler
		intFlag = false;
		o.Set( 1 );
		Assert.That( intFlag == true );
	}

	[Test]
	public void Observable_Int_SameDoesntCall () {

		var o = new Observable<int>(
			initialValue: 0,
			onChange: newValue => {
				intFlag = true;
			}
		);

		// setting to same value doesn't call handler
		intFlag = false;
		o.Set( 0 );
		Assert.That( intFlag == false );
	}

	// ****************************************

	bool listFlag = false;
	bool listIsNull = false;
	private void ResetListFlags () => listFlag = listIsNull = false;
	private ListObservable<int> GetNewListObservable ( List<int> initialValue ) =>
		new ListObservable<int>(
			initialValue: initialValue,
			onChange: list => {
				listFlag = true;
				listIsNull = list == null;
			}
		);

	[Test]
	public void Observable_List_InitialValue_DoesntHoldReference () {

		ResetListFlags();
		var myList = new List<int> { 1, 2, 3 };
		var o = GetNewListObservable( myList );
		Assert.That( listFlag == true );
		Assert.That( listIsNull == false );
		Assert.That( o.Get().Count == 3 );

		// make sure changing reference doesn't affect anything
		myList.Add( 4 );
		Assert.That( o.Get().Count == 3 );
	}

	[Test]
	public void Observable_List_OnChangeCalls_WhenConstructedWithNull () {

		// make sure initial null works
		ResetListFlags();
		var o = GetNewListObservable( null );
		Assert.That( listFlag == true );
		Assert.That( listIsNull == true );
	}
	[Test]
	public void Observable_List_OnChangeCalls_WhenConstructedWithNonNull () {

		// make sure initial non-null works
		ResetListFlags();
		var o = GetNewListObservable( new List<int> { 1, 2, 3 } );
		Assert.That( listFlag == true );
		Assert.That( listIsNull == false );
	}

	[Test]
	public void Observable_List_OnChangeCalls_WhenInitialValueNull () {

		var o = GetNewListObservable( null );

		// make sure setting a new list works
		ResetListFlags();
		o.Set( new List<int> { 1, 2, 3 } );
		Assert.That( listFlag == true );
		Assert.That( listIsNull == false );
		Assert.That( o.Get().Count == 3 );
	}

	[Test]
	public void Observable_List_OnChangeCalls_WhenInitialValueNonNull () {

		var o = GetNewListObservable( new List<int> { 1, 2, 3 } );

		// make sure setting to null from non-null works
		ResetListFlags();
		o.Set( null );
		Assert.That( listFlag == true );
		Assert.That( listIsNull == true );
	}

	// ****************************************

	bool setFlag = false;
	bool setIsNull = false;
	private void ResetSetFlags () => setFlag = setIsNull = false;
	private HashSetObservable<int> GetNewSetObservable ( HashSet<int> initialValue ) =>
		new HashSetObservable<int>(
			initialValue: initialValue,
			onChange: set => {
				setFlag = true;
				setIsNull = set == null;
			}
		);

	[Test]
	public void Observable_Set_InitialValue_DoesntHoldReference () {

		ResetSetFlags();
		var mySet = new HashSet<int> { 1, 2, 3 };
		var o = GetNewSetObservable( mySet );
		Assert.That( setFlag == true );
		Assert.That( setIsNull == false );
		Assert.That( o.Get().Count == 3 );

		// make sure changing reference doesn't affect anything
		mySet.Add( 4 );
		Assert.That( o.Get().Count == 3 );
	}

	[Test]
	public void Observable_Set_OnChangeCalls_WhenConstructedWithNull () {

		// make sure initial null works
		ResetSetFlags();
		var o = GetNewSetObservable( null );
		Assert.That( setFlag == true );
		Assert.That( setIsNull == true );
	}
	[Test]
	public void Observable_Set_OnChangeCalls_WhenConstructedWithNonNull () {

		// make sure initial non-null works
		ResetSetFlags();
		var o = GetNewSetObservable( new HashSet<int> { 1, 2, 3 } );
		Assert.That( setFlag == true );
		Assert.That( setIsNull == false );
	}

	[Test]
	public void Observable_Set_OnChangeCalls_WhenInitialValueNull () {

		var o = GetNewSetObservable( null );

		// make sure setting a new set works
		ResetSetFlags();
		o.Set( new HashSet<int> { 1, 2, 3 } );
		Assert.That( setFlag == true );
		Assert.That( setIsNull == false );
		Assert.That( o.Get().Count == 3 );
	}

	[Test]
	public void Observable_Set_OnChangeCalls_WhenInitialValueNonNull () {

		var o = GetNewSetObservable( new HashSet<int> { 1, 2, 3 } );

		// make sure setting to null from non-null works
		ResetSetFlags();
		o.Set( null );
		Assert.That( setFlag == true );
		Assert.That( setIsNull == true );
	}
}