// ========================================================
namespace MB.Tools
{
	using global::System;

	// =====================================================
	/// <summary>
	/// Represents a given date in a calendar format. It accepts AC and BC dates, and it does also take into consideration
	/// the change from the Julian calendar into the Gregorian calendar.
	/// </summary>
	[Serializable]
	public class CalendarDate : ICloneable, IComparable, IComparable<CalendarDate>, IEquatable<CalendarDate>
	{
		public static bool ValidateYear( int year, bool raise = false )
		{
			if( year == 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Year cannot be 0." );
				return false;
			}
			return true;
		}
		public static bool ValidateMonth( int month, bool raise = false )
		{
			if( month <= 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Month cannot be <= 0." );
				return false;
			}
			if( month > 12 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Month cannot be > 12." );
				return false;
			}
			return true;
		}
		public static bool ValidateDay( int year, int month, int day, bool raise = false )
		{
			// The fact that this method validates the three values given is used across the other methods.

			if( day <= 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Day cannot be <= 0." );
				return false;
			}

			// Obtaining the max also implicitly validates year and month...
			int max = DaysInMonth( year, month ); if( day > max ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Day cannot be > " + max + "." );
				return false;
			}

			// This is the gap between Julian and Gregorian calendars: to thursday 4.10.1582 follows friday 15.10.1582
			if( year == 1582 && month == 10 ) {
				if( day > 4 && day < 15 ) {
					if( raise ) throw new ArgumentOutOfRangeException( "Dates between thursday 4.Oct.1582 and friday 15.Oct.1582 do not exist." );
					return false;
				}
			}

			return true;
		}

		public static bool IsLeapYear( int year, bool raise = false )
		{
			// DateTime.IsLeapYear is easier and faster, but cannot be used for the extended range of dates that
			// this Date class can.

			if( !ValidateYear( year, raise ) ) return false;

			if( year >= 1582 ) { // Gregorian calendar
				bool r = ( year % 4 ) == 0 && ( ( year % 100 ) != 0 || ( year % 400 ) == 0 );
				return r;
			}
			else { // Julian calendar
				return false;
			}
		}
		public static int DaysInMonth( int year, int month, bool raise = false )
		{
			// DateTime.DaysInMonth is easier and faster, but cannot be used for the extended range of dates that
			// this Date class can.

			if( !ValidateYear( year, raise ) ) return -1;
			if( !ValidateMonth( month, raise ) ) return -1;

			switch( month ) {
				case 1: // january
				case 3: // march
				case 5: // may
				case 7: // july
				case 8: // august
				case 10: // october:
				case 12: // december
					return 31;

				case 4: // april
				case 6: // june
				case 9: // september
				case 11: // november
					return 30;
			}
			// february
			if( CalendarDate.IsLeapYear( year ) ) return 29;
			return 28;
		}

		public static DateTime ToDateTime( CalendarDate date )
		{
			if( date == null ) throw new ArgumentNullException( "date", "CalendarDate cannot be null." );
			return new DateTime( date.Year, date.Month, date.Day );
		}
		public static DateTime ToDateTime( CalendarDate date, ClockTime time )
		{
			if( date == null ) throw new ArgumentNullException( "date", "CalendarDate be null." );
			if( time == null ) throw new ArgumentNullException( "time", "ClockTime cannot be null." );
			return new DateTime( date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond );
		}

		public static bool Equals( CalendarDate left, CalendarDate right )
		{
			if( left == null && right == null ) return true;
			if( left == null && right != null ) return false;
			if( left != null && right == null ) return false;

			if( left.Year != right.Year ) return false;
			if( left.Month != right.Month ) return false;
			if( left.Day != right.Day ) return false;
			return true;
		}
		public static int Compare( CalendarDate left, CalendarDate right )
		{
			if( left == null && right == null ) return 0;
			if( left == null && right != null ) return +1;
			if( left != null && right == null ) return -1;

			if( left._Year != right._Year ) return ( left._Year > right._Year ) ? -1 : 1;
			if( left._Month != right._Month ) return ( left._Month > right._Month ) ? -1 : 1;
			if( left._Day != right._Day ) return ( left._Day > right._Day ) ? -1 : 1;
			return 0;
		}

		public static implicit operator DateTime( CalendarDate dt )
		{
			return CalendarDate.ToDateTime( dt );
		}
		public static implicit operator CalendarDate( DateTime dt )
		{
			return new CalendarDate( dt );
		}

		public static CalendarDate Parse( string str, char[] separators = null )
		{
			if( string.IsNullOrEmpty( str ) ) throw new ArgumentNullException( "str", "String to parse cannot be null." );
			if( separators == null || separators.Length == 0 ) separators = new char[] { '/', '-', ':', '.' };

			string[] args = str.Split( separators );
			int year = int.Parse( args[0] );
			int month = int.Parse( args[1] );
			int day = int.Parse( args[2] );

			return new CalendarDate( year, month, day );
		}

		public static CalendarDate Add( CalendarDate date, int ndays )
		{
			if( date == null ) throw new ArgumentNullException( "date", "CalendarDate cannot be null." );

			int year = date.Year;
			int month = date.Month;
			int day = date.Day;

			// Case ndays is negative...
			while( ndays < 0 ) {
				if( ( --day ) < 1 ) {
					if( ( --month ) < 1 ) {
						if( ( --year ) == 0 ) year = -1;
						month = 12;
					}
					day = CalendarDate.DaysInMonth( year, month );
				}
				if( year == 1582 && month == 10 && day == 14 ) day = 4;
				ndays++;
			}

			// Case ndays is positive
			while( ndays > 0 ) {
				int max = CalendarDate.DaysInMonth( year, month );
				if( ( ++day ) > max ) {
					if( ( ++month ) > 12 ) {
						if( ( ++year ) == 0 ) year = 1;
						month = 1;
					}
					day = 1;
				}
				if( year == 1582 && month == 10 && day == 5 ) day = 15;
				ndays--;
			}

			return new CalendarDate( year, month, day );
		}

		// --------------------------------------------------
		int _Year = 0, _Month = 0, _Day = 0;

		public CalendarDate( int year, int month, int day )
		{
			ValidateDay( year, month, day, raise: true );
			_Year = year;
			_Month = month;
			_Day = day;
		}
		public CalendarDate( DateTime dt ) : this( dt.Year, dt.Month, dt.Day ) { }

		public CalendarDate Clone()
		{
			return new CalendarDate( _Year, _Month, _Day );
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public int Year
		{
			get { return _Year; }
		}
		public int Month
		{
			get { return _Month; }
		}
		public int Day
		{
			get { return _Day; }
		}

		public override string ToString()
		{
			string s = string.Format( "{0,4:0000}/{1,2:00}/{2,2:00}", _Year, _Month, _Day );
			return s;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool Equals( CalendarDate other )
		{
			return CalendarDate.Equals( this, other );
		}
		public override bool Equals( object obj )
		{
			if( obj == null ) return false;
			if( !typeof( CalendarDate ).IsAssignableFrom( obj.GetType() ) ) return false;
			return CalendarDate.Equals( this, (CalendarDate)obj );
		}

		public int CompareTo( CalendarDate other )
		{
			return CalendarDate.Compare( this, other );
		}
		public int CompareTo( object obj )
		{
			if( obj == null ) return -1;
			if( !typeof( CalendarDate ).IsAssignableFrom( obj.GetType() ) )
				throw new ArgumentException( "Object is not CalendarDate" );

			return CalendarDate.Compare( this, (CalendarDate)obj );
		}

		public CalendarDate Add( int ndays )
		{
			return CalendarDate.Add( this, ndays );
		}
	}
}
// ========================================================
