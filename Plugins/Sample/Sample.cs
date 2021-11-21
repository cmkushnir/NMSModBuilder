//=============================================================================

using System;
using System.Windows;

//=============================================================================

namespace cmk.NMS.ModBuilder.Sample
{
	public class Plugin
	: cmk.NMS.ModBuilder.Plugin
	{
		public Plugin() : base()
		{
		}

		//...........................................................

		public bool Load()
		{
			var app = Application.Current as cmk.NMS.ModBuilder.App;

			Log.Default.AddInformation("cmk.NMS.ModBuilder.Sample.Plugin.Load");

			return true;
		}

		//...........................................................

		public void Unload()
		{
		}
	}
}

//=============================================================================
