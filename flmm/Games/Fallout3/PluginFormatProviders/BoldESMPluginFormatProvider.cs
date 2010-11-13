﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Fomm.Games.Fallout3.PluginFormatProviders
{
	/// <summary>
	/// A plugin format provider that makes ESM plugins bi=old if the plugin list.
	/// </summary>
	public class BoldESMPluginFormatProvider : IPluginFormatProvider
	{
		private PluginFormat.PluginFormatterManager m_pfmManager = null;
		
		#region IPluginFormatProvider Members

		/// <summary>
		/// Sets the <see cref="PluginFormat.PluginFormatterManager"/> to use.
		/// </summary>
		/// <value>The <see cref="PluginFormat.PluginFormatterManager"/> to use.</value>
		public PluginFormat.PluginFormatterManager PluginFormatterManager
		{
			set
			{
				m_pfmManager = value;
			}
		}

		/// <summary>
		/// Determins if the provider has a format for the specified plugin.
		/// </summary>
		/// <param name="p_strPluginName">The name of the plugin for which to check if there is a format.</param>
		/// <returns><lang cref="true"/> if this provider has a format for the specified plugin;
		/// <lang cref="false"/> otherwise.</returns>
		public bool HasFormat(string p_strPluginName)
		{
			return Properties.Settings.Default.fallout3BoldifyESMs && Path.GetExtension(p_strPluginName).Equals(".esm", StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Gets the provider's format for the specified plugin.
		/// </summary>
		/// <param name="p_strPluginName">The name of the plugin whose format is to be returned.</param>
		/// <returns>The provider's format for the specified plugin, or <lang cref="null"/> if the
		/// provider does not have a format for the speficied plugin.</returns>
		public PluginFormat GetFormat(string p_strPluginName)
		{
			return m_pfmManager.CreateFormat(null, null, FontStyle.Bold, null, null, null);
		}

		#endregion
	}
}
