// ========================================================
namespace MB.Tools
{
	using global::System;

	// =====================================================
	/// <summary>
	/// Represents a given moment of time in a 24 hours clock format.
	/// </summary>
	[Serializable]
	public class ClockTime : ICloneable, IComparable, IComparable<ClockTime>, IEquatable<ClockTime>
	{
		public static bool ValidateHour( int hour, bool raise = false )
		{
			if( hour < 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Hour cannot be < 0." );
				return false;
			}
			if( hour > 23 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Hour cannot be > 23." );
				return false;
			}
			return true;
		}
		public static bool ValidateMinute( int minute, bool raise = false )
		{
			if( minute < 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Minute cannot be < 0." );
				return false;
			}
			if( minute > 59 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Minute cannot be > 59." );
				return false;
			}
			return true;
		}
		public static bool ValidateSecond( int second, bool raise = false )
		{
			if( second < 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Second cannot be < 0." );
				return false;
			}
			if( second > 59 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Second cannot be > 59." );
				return false;
			}
			return true;
		}
		public static bool ValidateMillisecond( int millisecond, bool raise = false )
		{
			if( millisecond < 0 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Millisecond cannot be < 0." );
				return false;
			}
			if( millisecond > 999 ) {
				if( raise ) throw new ArgumentOutOfRangeException( "Millisecond cannot be > 999." );
				return false;
			}
			return true;
		}

		public static TimeSpan ToTimeSpan( ClockTime time )
		{
			if( time == null ) throw new ArgumentNullException( "time", "ClockTime cannot be null." );
			return new TimeSpan( 0, time.Hour, time.Minute, time.Second, time.Millisecond );
		}
		public static DateTime ToDateTime( CalendarDate date, ClockTime time )
		{
			if( date == null ) throw new ArgumentNullException( "date", "CalendarDate be null." );
			if( time == null ) throw new ArgumentNullException( "time", "ClockTime cannot be null." );
			return new DateTime( date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, time.Millisecond );
		}

		public static bool Equals( ClockTime left, ClockTime right )
		{
			if( left == null && right == null ) return true;
			if( left == null && right != null ) return false;
			if( left != null && right == null ) return false;

			if( left.Hour != right.Hour ) return false;
			if( left.Minute != right.Minute ) return false;
			if( left.Second != right.Second ) return false;
			if( left.Millisecond != right.Millisecond ) return false;
			return true;
		}
		public static int Compare( ClockTime left, ClockTime right )
		{
			if( left == null && right == null ) return 0;
			if( left == null && right != null ) return +1;
			if( left != null && right == null ) return -1;

			if( left.Hour != right.Hour ) return ( left.Hour > right.Hour ) ? -1 : 1;
			if( left.Minute != right.Minute ) return ( left.Minute > right.Minute ) ? -1 : 1;
			if( left.Second != right.Second ) return ( left.Second > right.Second ) ? -1 : 1;
			if( left.Millisecond != right.Millisecond ) return ( left.Millisecond > right.Millisecond ) ? -1 : 1;
			return 0;
		}

		public static implicit operator TimeSpan( ClockTime time )
		{
			return ClockTime.ToTimeSpan( time );
		}
		public static implicit operator ClockTime( TimeSpan ts )
		{
			return new ClockTime( ts );
		}

		public static ClockTime Parse( string val, char[] separators = null )
		{
			if( string.IsNullOrEmpty( val ) ) throw new ArgumentNullException( "val", "String to parse cannot be null." );
			if( separators == null || separators.Length == 0 ) separators = new char[] { ':', '.' };

			string[] args = val.Split( separators );
			int hour = int.Parse( args[0] );
			int minute = int.Parse( args[1] );
			int second = int.Parse( args[2] );
			int millisecond = 0; if( args.Length > 3 ) millisecond = int.Parse( args[3] );

			return new ClockTime( hour, minute, second, millisecond );
		}

		public static ClockTime Add( ClockTime time, out int days, int hours, int minutes = 0, int seconds = 0, int milliseconds = 0 )
		{
			if( time == null ) throw new ArgumentNullException( "time", "ClockTime cannot be null." );
			days = 0;

			milliseconds += time.Millisecond;
			if( milliseconds > 999 ) { seconds += milliseconds / 1000; milliseconds = milliseconds % 1000; }
			if( milliseconds < 0 ) { seconds += milliseconds / 1000 - 1; milliseconds = 1000 + milliseconds % 1000; }

			seconds += time.Second;
			if( seconds > 59 ) { minutes += seconds / 60; seconds = seconds % 60; }
			if( seconds < 0 ) { minutes += seconds / 60 - 1; seconds = 60 + seconds % 60; }

			minutes += time.Minute;
			if( minutes > 59 ) { hours += minutes / 60; minutes = minutes % 60; }
			if( minutes < 0 ) { hours += minutes / 60 - 1; minutes = 60 + minutes % 60; }

			hours += time.Hour;
			if( hours > 23 ) { days = hours / 24; hours = hours % 24; }
			if( hours < 0 ) { days = hours / 24 - 1; hours = 24 + hours % 24; }

			return new ClockTime( hours, minutes, seconds, milliseconds );
		}

		// --------------------------------------------------
		int _Hour = 0, _Minute = 0, _Second = 0, _Millisecond = 0;

		public ClockTime( int hour, int minute, int second, int millisecond = 0 )
		{
			ValidateHour( hour, raise: true ); _Hour = hour;
			ValidateMinute( minute, raise: true ); _Minute = minute;
			ValidateSecond( second, raise: true ); _Second = second;
			ValidateMillisecond( millisecond, raise: true ); _Millisecond = millisecond;
		}
		public ClockTime( DateTime dt ) : this( dt.Hour, dt.Minute, dt.Second, dt.Millisecond ) { }
		public ClockTime( TimeSpan ts ) : this( ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds ) { }

		public ClockTime Clone()
		{
			return new ClockTime( _Hour, _Minute, _Second, _Millisecond );
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public int Hour
		{
			get { return _Hour; }
		}
		public int Minute
		{
			get { return _Minute; }
		}
		public int Second
		{
			get { return _Second; }
		}
		public int Millisecond
		{
			get { return _Millisecond; }
		}

		public override string ToString()
		{
			string s = string.Format( "{0,2:00}:{1,2:00}:{2,2:00}", _Hour, _Minute, _Second );
			if( _Millisecond != 0 ) s = string.Format( "{0}.{1,3:000}", s, _Millisecond );
			return s;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool Equals( ClockTime other )
		{
			return ClockTime.Equals( this, other );
		}
		public override bool Equals( object obj )
		{
			if( obj == null ) return false;
			if( !typeof( ClockTime ).IsAssignableFrom( obj.GetType() ) ) return false;
			return ClockTime.Equals( this, (ClockTime)obj );
		}

		public int CompareTo( ClockTime other )
		{
			return ClockTime.Compare( this, other );
		}
		public int CompareTo( object obj )
		{
			if( obj == null ) return -1;
			if( !typeof( ClockTime ).IsAssignableFrom( obj.GetType() ) )
				throw new ArgumentException( "Object is not ClockTime" );

			return ClockTime.Compare( this, (ClockTime)obj );
		}

		public ClockTime Add( out int days, int hours, int minutes = 0, int seconds = 0, int milliseconds = 0 )
		{
			return ClockTime.Add( this, out days, hours, minutes, seconds, milliseconds );
		}
	}
}
// ========================================================
