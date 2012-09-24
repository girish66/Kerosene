// ========================================================
namespace MB.Tools
{
	using global::System;
	using global::System.Runtime.InteropServices;

	// =====================================================
	/// <summary>
	/// Utilities for the System.Console class
	/// </summary>
	public static class ConsoleHelper
	{
		struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		const UInt32 WM_MOVE = 0x0003;

		[DllImport( "kernel32.dll" )]
		static extern IntPtr GetConsoleWindow();

		[DllImport( "user32.dll", CharSet = CharSet.Auto, SetLastError = false )]
		static extern IntPtr SendMessage( IntPtr hWnd, Int32 Msg, IntPtr wParam, IntPtr lParam );

		[DllImport( "user32.dll", SetLastError = true )]
		static extern bool MoveWindow( IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint );

		[DllImport( "user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true )]
		static extern bool GetWindowRect( IntPtr hWnd, ref RECT rect );
		
		/// <summary>
		/// Sets the console window's position to the absolute coordinates given.
		/// </summary>
		/// <param name="leftPixel">The left position in pixels.</param>
		/// <param name="topPixel">The top position in pixeles.</param>
		public static void SetWindowPosition( int leftPixel, int topPixel )
		{
			IntPtr windowHandle = GetConsoleWindow();
			RECT rect = new RECT();
			GetWindowRect( windowHandle, ref rect );

			MoveWindow( windowHandle, leftPixel, topPixel,
				rect.right - rect.left, rect.bottom - rect.top, true );
		}
	}
}
// ========================================================
