//=============================================================================
/*
cmk NMS Common
Copyright (C) 2021  Chris Kushnir

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
//=============================================================================

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

//=============================================================================

namespace cmk
{
    public static partial class _x_
	{
		public static bool IsNullOrEmpty( this string STRING )
		{
			return string.IsNullOrWhiteSpace(STRING) || ((object)STRING == DBNull.Value);
		}

		//...........................................................

		/// <summary>
		/// Return # of leading CHAR.
		/// </summary>
		public static int LengthLeading( this string STRING, char CHAR )
		{
			int length  = 0 ;
			if( STRING != null ) {
				for( var i = 0; i < STRING.Length && STRING[i] == CHAR; ++i, ++length ) ;
			}
			return length;
		}

		//...........................................................

		/// <summary>
		/// Null safe STRING.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries)
		/// </summary>
		public static string[] SplitEx( this string STRING, params char[] SEPARATORS )
		{
			return STRING.IsNullOrEmpty() ?
				null : STRING.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries)
			;
		}

		//...........................................................

		/// <summary>
		/// Null safe STRING.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries)
		/// </summary>
		public static string[] SplitEx( this string STRING, params string[] SEPARATORS )
		{
			return STRING.IsNullOrEmpty() ?
				null : STRING.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries)
			;
		}

		//...........................................................

		/// <summary>
		/// Replace "*" with ".*" and "?" with a ".".
		/// </summary>
		public static string AsRegexPattern( this string STRING )
		{
			return Regex.Escape(STRING ?? "*").Replace(@"\*", ".*").Replace(@"\?", ".");
		}

		//...........................................................

		/// <summary>
		/// Return new compiled multiline Regex with specified options.
		/// If PATTERN_IS_REGEX == true  then PATTERN used as-is, needs to be valid regex.
		/// If PATTERN_IS_REGEX == false then PATTERN = PATTERN.AsRegexPattern.
		/// </summary>
		public static Regex CreateRegex(
			this string PATTERN,
			     bool   CASE_SENSITIVE   = true,
			     bool   PATTERN_IS_REGEX = false,
			     double TIMEOUT_MILLISEC = 1000
		){
			if( !PATTERN_IS_REGEX ) PATTERN = AsRegexPattern(PATTERN);

			var options = RegexOptions.Compiled | RegexOptions.Multiline;
			if( !CASE_SENSITIVE ) options |= RegexOptions.IgnoreCase;

			return new(PATTERN, options,
				System.TimeSpan.FromMilliseconds(TIMEOUT_MILLISEC)
			);
		}

		//...........................................................

		/// <summary>
		/// Null safe string.Format.
		/// </summary>
		public static string Format( this string STRING, params object[] ARG )
		{
			return STRING.IsNullOrEmpty() || ARG.IsNullOrEmpty() ?
				STRING : string.Format(STRING, ARG)
			;
		}

		//...........................................................

		/// <summary>
		/// Convert string to byte[].
		/// </summary>
		public static byte[] ToBytes( this string STRING, Encoding ENC = null )
		{
			if( ENC == null ) ENC = Encoding.Default;

			byte[] bytes = null;
			if( !STRING.IsNullOrEmpty() ) bytes = ENC.GetBytes(STRING);

			return bytes;
		}

		//...........................................................

		/// <summary>
		/// Convert string to char[].
		/// </summary>
		public static char[] ToChars( this string STRING, Encoding ENC = null )
		{
			if( ENC == null ) ENC = Encoding.Default;

			char[] chars = null;
			if( !STRING.IsNullOrEmpty() ) chars = ENC.GetChars(STRING.ToBytes(ENC));

			return chars;
		}

		//...........................................................

		/// <summary>
		/// Convert string to non-resizable MemoryStream.
		/// </summary>
		public static Stream ToStream( this string STRING, Encoding ENC = null )
		{
			var bytes  = STRING.ToBytes(ENC);
			var stream = new MemoryStream(bytes);
			stream.Position = 0;
			return stream;
		}

		//...........................................................

		/// <summary>
		/// Insert a space before each capital or '_' in STRING.
		/// Removes any '_' used to insert a space.
		/// Given:
		/// ThisIsA_String -> This Is A String
		/// </summary>
		public static string Expand( this string STRING )
		{
			if( STRING.IsNullOrEmpty() ) return STRING;

			var s = STRING.ToCharArray();
			int i = s.Length;
			foreach( var c in s ) if( char.IsUpper(c) || c == '_' ) ++i;

			var e = new char[i];
			var j = 0;
			var b = false;

			foreach( var c in s ) {
				if( c == '\0' ) break;
				if( char.IsWhiteSpace(c) || c == '_' ) b = true;
				else {
					if( char.IsUpper(c) ) b = true;
					if( b && j > 0 ) e[j++] = ' ';
					e[j++] = c;
					b = false;
				}
			}

			return new(e, 0, j);
		}

		//...........................................................

		/// <summary>
		/// Split string on SEPARATORS, removing empty entries, and recombine
		/// by injecting SPACE between split words.
		/// Similar to just replace SEPARATORS w/ SPACE, but not for all cases.
		/// Given:
		/// SPACE = "",  SEPARATORS = null: This Is A String -> ThisIsAString
		/// SPACE = "_", SEPARATORS = null: This Is A String -> This_Is_A_String
		/// </summary>
		public static string Collapse( this string STRING, string SPACE = "", params char[] SEPARATORS )
		{
			if( SEPARATORS.IsNullOrEmpty() ) SEPARATORS = new[] { ' ' };

			var a = STRING.SplitEx(SEPARATORS);
			if( a.IsNullOrEmpty() ) return "";

			var b = new StringBuilder(a[0]);

			if( SPACE.IsNullOrEmpty() ) {
				for( int i = 1; i < a.Length; ++i ) {
					b.Append(a[i]);
				}
			}
			else {
				for( int i = 1; i < a.Length; ++i ) {
					b.Append(SPACE);
					b.Append(a[i]);
				}
			}

			return b.ToString();
		}

		//...........................................................

		/// <summary>
		/// Split string on SEPARATORS, removing empty entries, and recombine
		/// by injecting SPACE between split words.
		/// Similar to just replace SEPARATORS w/ SPACE, but not for all cases.
		/// Given:
		/// SPACE = "---":  SEPARATORS = null: This Is A String -> This---Is---A---String
		/// </summary>
		public static string Collapse( this string STRING, string SPACE, params string[] SEPARATORS )
		{
			if( SEPARATORS.IsNullOrEmpty() ) SEPARATORS = new[] { " " };

			var a = STRING.SplitEx(SEPARATORS);
			if( a.IsNullOrEmpty() ) return "";

			var b = new StringBuilder(a[0]);

			if( SPACE.IsNullOrEmpty() ) {
				for( int i = 1; i < a.Length; ++i ) {
					b.Append(a[i]);
				}
			}
			else {
				for( int i = 1; i < a.Length; ++i ) {
					b.Append(SPACE);
					b.Append(a[i]);
				}
			}

			return b.ToString();
		}

		//...........................................................

		/// <summary>
		/// If INCLUDE_CARRIAGE_RETURN = true:  Replace all   "\n" with "\r\n".
		/// If INCLUDE_CARRIAGE_RETURN = false: Replace all "\r\n" with   "\n".
		/// Not cheap.  Two or three string.Replace() calls.
		/// </summary>
		public static string NormalizeNewLine( this string STRING, bool INCLUDE_CARRIAGE_RETURN = false )
		{
			if( string.IsNullOrEmpty(STRING) ) return STRING;

			var normalized = STRING.Replace("\r\n", "\n").Replace('\r', '\n');
			if( INCLUDE_CARRIAGE_RETURN ) normalized = normalized.Replace("\n", "\r\n");

			return normalized;
		}

		//...........................................................

		/// <summary>
		/// https://en.wikipedia.org/wiki/Jenkins_hash_function
		/// </summary>
		public static uint JenkinsHash( this string STRING )
		{
			uint hash = 0;
			if( STRING != null ) {
				foreach( var c in STRING ) {
					hash += c;
					hash += (hash << 10);
					hash ^= (hash >>  6);
				}
				hash += (hash <<  3);
				hash ^= (hash >> 11);
				hash += (hash << 15);
			}
			return hash;
		}

		//...........................................................

		// https://stackoverflow.com/questions/11121936/dotnet-soundex-function
		public static string Soundex( this string STRING )
		{
			var soundex = new StringBuilder();

			if( STRING != null && STRING.Length > 0 ) {
				var previousCode  = "";
				var currentCode   = "";
				var currentLetter = "";

				soundex.Append(STRING[0]); // keep initial char

				// start at 0 in order to correctly encode "Pf..."
				for( int i = 0; i < STRING.Length; ++i ) {
					currentLetter = STRING[i].ToString().ToLower();
					currentCode   = "";

					     if( "bfpv"    .Contains(currentLetter) ) currentCode = "1";
					else if( "cgjkqsxz".Contains(currentLetter) ) currentCode = "2";
					else if( "dt"      .Contains(currentLetter) ) currentCode = "3";
					else if( currentLetter == "l" )               currentCode = "4";
					else if( "mn".Contains(currentLetter) )       currentCode = "5";
					else if( currentLetter == "r" )               currentCode = "6";

					// do not add first code to result string
					if( currentCode != previousCode && i > 0 ) soundex.Append(currentCode);

					if( soundex.Length == 4 ) break;

					// always retain previous code, even empty
					previousCode = currentCode;
				}
			}
			if( soundex.Length < 4 ) {
				soundex.Append(new string('0', 4 - soundex.Length));
			}

			return soundex.ToString().ToUpper();
		}

		//...........................................................

		// https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560
		public static int Levenshtein( this string LHS, string RHS )
		{
			var lhs_length = LHS.Length;
			var rhs_length = RHS.Length;
			if( lhs_length == 0 ) return rhs_length;
			if( rhs_length == 0 ) return lhs_length;

			var matrix = new int[lhs_length + 1, rhs_length + 1];
			for( var i = 0; i <= lhs_length; matrix[i, 0] = i++ ) {}
			for( var j = 0; j <= rhs_length; matrix[0, j] = j++ ) {}

			for( var i = 1; i <= lhs_length; i++ ) {
				for( var j = 1; j <= rhs_length; j++ ) {
					var cost = (RHS[j - 1] == LHS[i - 1]) ? 0 : 1;
					matrix[i, j] = Math.Min(
						Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
						matrix[i - 1, j - 1] + cost);
				}
			}

			return matrix[lhs_length, rhs_length];
		}
	}
}

//=============================================================================
