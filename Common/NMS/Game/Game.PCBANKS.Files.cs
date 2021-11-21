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
using System.Threading;
using System.Threading.Tasks;

//=============================================================================

namespace cmk.NMS.Game.PCBANKS
{
	/// <summary>
	/// Manage a collection of game .pak files.
	/// </summary>
	public class Files
	: cmk.NMS.PAK.Files
	{
		protected readonly NMS.PAK.Item.Info.Node m_info_tree       = new();
		protected readonly ManualResetEventSlim   m_info_tree_built = new(false);

		//...........................................................

		/// <summary>
		/// When constructor returns the merged InfoTree may still be building.
		/// </summary>
		public Files( Game.Data GAME ) : base(GAME, "GAMEDATA\\PCBANKS\\")
		{
			_ = Task.Run(() => BuildTree());
		}

		//...........................................................

		/// <summary>
		/// Object node in merged .pak item tree.
		/// Will block until built.
		/// </summary>
		public NMS.PAK.Item.Info.Node InfoTree {
			get {
				return m_info_tree_built.Wait(Int32.MaxValue) ?
					m_info_tree : null
				;
			}
		}

		//...........................................................

		protected void BuildTree()
		{
			if( m_info_tree_built.IsSet ) return;
			Log.Default.AddInformation($"Building merged item info tree from {SubPath}*.pak");

			Lock.AcquireRead(int.MaxValue);
			try {
				foreach( var file in List ) {
					var items  = file.InfoList;
					if( items == null ) continue;

					for( int index = 0; index < items.Count; ++index ) {
						// will recursivly build branch as needed to add leaf.
						// change info.TreeNode from it's pak tree to this merged tree, ok to do in read lock.
						items[index].TreeNode = m_info_tree.Insert(items[index].Path, items[index]);
					}
				}
			}
			finally { Lock.ReleaseRead(); }

			m_info_tree_built.Set();
			Log.Default.AddInformation($"Built merged item info tree from {SubPath}*.pak");
		}
	}
}

//=============================================================================
