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

using System.Collections.Generic;

//=============================================================================

// https://developer.valvesoftware.com/wiki/KeyValues

namespace cmk.Steam.File
{
    public class KeyValue
	{
		public KeyValue( KeyValue PARENT )
		{
			Parent = PARENT;
		}

		public KeyValue Parent = null;
		public string   Key    = "";
		public object   Object = null;   // string or List

		public string Value {
			get => Object as string;
			set => Object = value;
		}

		public List<KeyValue> Values {
			get => Object as List<KeyValue>;
			set => Object = value;
		}
	}

	//=========================================================================

	public class Loader
	{
		protected struct Span {
			public Span( int START = 0 ) { Start = START; End = START; }
			public int    Start  = 0;
			public int    End    = 0;  // not included in span
			public int    Length => End - Start;
			public string Text( string SOURCE )
			{
				return SOURCE.Substring(Start, Length)
					.Trim().Trim('\"')      // leading|trailing whitespace then "
					.Replace("\\\\", "\\")  // \\ => \
					.Replace("\r", "")
					.Replace("\n", "")
				;
			}
		}		 

		//...........................................................

		public Loader( cmk.IO.Path PATH )
		{
			Path = PATH ?? new();

			try   { Text = System.IO.File.OpenText(Path).ReadToEnd(); }
			catch { return; }

			var token = new Span();
			Root = Parse(null, ref token);
		}

		//...........................................................

		public readonly cmk.IO.Path    Path;
		public readonly string         Text;
		public readonly List<KeyValue> Root;

		//...........................................................

		protected Span NextToken( Span CURRENT )
		{
			var  next      = new Span( CURRENT.End );
			var  in_token  = false;
			var  in_quoted = false;

			for( next.End = CURRENT.End; next.End < Text.Length; ++next.End ) {
				var c = Text[next.End];

				// eat leading whitespace
				if( !in_token && (
					char.IsWhiteSpace(c) ||
					char.IsControl(c)
				)) {
					++next.Start;
					continue;
				}

				in_token = true;
				switch( c ) {
					case '\\':  // possibly quoted char in key|value string
						var j  = next.End + 1;
						if( j >= Text.Length ) return next;  // unexpected end of file
						var cj = Text[j];
						switch( cj ) {  // eat escape char '\\' for following
							case '{':
							case '}':
							case '\"': ++next.End; break;
						}
						break;
					case '\"':  // start or end of quoted key|value string
						if( in_quoted ) {
							++next.End;
							return next;
						}
						else {
							in_quoted = true;
						}
						break;
					case '{':  // start of value list
					case '}':  // end of value list
						// if brace first char then it is this token,
						// else it's the start of the following token
						if( next.Length <= 0 ) ++next.End;
						return next;
				}
			}

			return next;
		}

		//...........................................................

		protected List<KeyValue> Parse( KeyValue PARENT, ref Span CURRENT )
		{
			var list = new List<KeyValue>();
			for(;;) {
				var kv = new KeyValue(PARENT);

				CURRENT = NextToken(CURRENT);
				if( CURRENT.Length <= 0 ) break;
				kv.Key = CURRENT.Text(Text);
				if( kv.Key == "}" ) {
					break;
				}

				CURRENT = NextToken(CURRENT);
				var obj = CURRENT.Text(Text);

				if( obj == "{" ) {
					kv.Values = Parse(kv, ref CURRENT);
					list.Add(kv);
				}
				else {
					kv.Value = obj;
					list.Add(kv);
				}
			}
			return list;
		}
	}
}

//=============================================================================
