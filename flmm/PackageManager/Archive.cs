﻿using System;
using SevenZip;
using System.Collections.Generic;
using System.IO;
using Fomm.Util;
using System.Text;

namespace Fomm.PackageManager
{
	/// <summary>
	/// Encapsulates the interactions with an archive file.
	/// </summary>
	public class Archive : IDisposable
	{
		/// <summary>
		/// The path prefix use to identify a file as being contained in an archive.
		/// </summary>
		public const string ARCHIVE_PREFIX = "arch:";

		private string m_strPath = null;
		//private SevenZipExtractor m_szeExtractor = null;
		private SevenZipCompressor m_szcCompressor = null;
		private List<string> m_strFiles = new List<string>();
		private Dictionary<string, Int32> m_dicFileIndex = new Dictionary<string, int>();
		private bool m_booCanEdit = false;

		#region Constructors

		/// <summary>
		/// A simple constructor the initializes the object with the given values.
		/// </summary>
		/// <param name="p_strPath">The path to the archive file.</param>
		public Archive(string p_strPath)
		{
			m_strPath = p_strPath;
			using (SevenZipExtractor szeExtractor = new SevenZipExtractor(m_strPath))
			{
				if (Enum.IsDefined(typeof(OutArchiveFormat), szeExtractor.Format.ToString()))
				{
					m_szcCompressor = new SevenZipCompressor();
					m_szcCompressor.CompressionMode = CompressionMode.Append;
					m_szcCompressor.ArchiveFormat = (OutArchiveFormat)Enum.Parse(typeof(OutArchiveFormat), szeExtractor.Format.ToString());
					m_booCanEdit = true;
				}
			}
			LoadFileIndices();
		}

		#endregion

		/// <summary>
		/// Caches information about the files in the archive.
		/// </summary>
		protected void LoadFileIndices()
		{
			m_dicFileIndex.Clear();
			m_strFiles.Clear();
			using (SevenZipExtractor szeExtractor = new SevenZipExtractor(m_strPath))
			{
				foreach (ArchiveFileInfo afiFile in szeExtractor.ArchiveFileData)
					if (!afiFile.IsDirectory)
					{
						m_dicFileIndex[afiFile.FileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLowerInvariant()] = afiFile.Index;
						m_strFiles.Add(afiFile.FileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
					}
			}
		}

		/// <summary>
		/// Parses the given path to extract the path to the archive file, and the path to
		/// a file within said archive.
		/// </summary>
		/// <param name="p_strPath">The file path to parse.</param>
		/// <returns>The path to an archive file, and the path to a file within said archive.</returns>
		public static KeyValuePair<string, string> ParseArchive(string p_strPath)
		{
			if (!p_strPath.StartsWith(ARCHIVE_PREFIX))
				return new KeyValuePair<string, string>(null, null);
			Int32 intEndIndex = p_strPath.IndexOf("//", ARCHIVE_PREFIX.Length);
			if (intEndIndex < 0)
				intEndIndex = p_strPath.Length;
			string strArchive = p_strPath.Substring(ARCHIVE_PREFIX.Length, intEndIndex - ARCHIVE_PREFIX.Length);
			string strPath = p_strPath.Substring(intEndIndex + 2);
			return new KeyValuePair<string, string>(strArchive, strPath);
		}

		/// <summary>
		/// Determins if the given path is a directory in this archive.
		/// </summary>
		/// <param name="p_strPath">The path to examine.</param>
		/// <returns><lang cref="true"/> if the given path is a directory in this archive;
		/// <lang cref="false"/> otherwise.</returns>
		public bool IsDirectory(string p_strPath)
		{
			string strPathWithSep = p_strPath.Trim(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
			string strPathWithAltSep = strPathWithSep + Path.AltDirectorySeparatorChar;
			strPathWithSep += Path.DirectorySeparatorChar;

			ArchiveFileInfo afiFile = default(ArchiveFileInfo);
			using (SevenZipExtractor szeExtractor = new SevenZipExtractor(m_strPath))
			{
				foreach (ArchiveFileInfo afiTmp in szeExtractor.ArchiveFileData)
					if (afiTmp.FileName.Equals(p_strPath, StringComparison.InvariantCultureIgnoreCase))
					{
						afiFile = afiTmp;
						break;
					}
					else if (afiTmp.FileName.StartsWith(strPathWithSep, StringComparison.InvariantCultureIgnoreCase) || afiTmp.FileName.StartsWith(strPathWithAltSep, StringComparison.InvariantCultureIgnoreCase))
						return true;
			}
			return (afiFile == null) ? false : afiFile.IsDirectory;
		}

		/// <summary>
		/// Gets a list of directories that are in the specified directory in this archive.
		/// </summary>
		/// <param name="p_strDirectory">The directory in the archive whose descendents are to be returned.</param>
		/// <returns>A list of directories that are in the specified directory in this archive.</returns>
		public string[] GetDirectories(string p_strDirectory)
		{
			if (String.IsNullOrEmpty(p_strDirectory))
				return m_strFiles.ToArray();
			string strPrefix = p_strDirectory;
			strPrefix = strPrefix.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			strPrefix = strPrefix.Trim(new char[] { Path.DirectorySeparatorChar });
			if (strPrefix.Length > 0)
				strPrefix += Path.DirectorySeparatorChar;
			Set<string> lstFolders = new Set<string>();
			Int32 intStopIndex = 0;
			foreach (string strFile in m_strFiles)
			{
				if (strFile.StartsWith(strPrefix, StringComparison.InvariantCultureIgnoreCase))
				{
					intStopIndex = strFile.IndexOf(Path.DirectorySeparatorChar, strPrefix.Length);
					if (intStopIndex < 0)
						continue;
					lstFolders.Add(strFile.Substring(0, intStopIndex));
				}
			}
			return lstFolders.ToArray();
		}

		/// <summary>
		/// Gets a list of files that are in the specified directory in this archive.
		/// </summary>
		/// <param name="p_strDirectory">The directory in the archive whose descendents are to be returned.</param>
		/// <returns>A list of files that are in the specified directory in this archive.</returns>
		public string[] GetFiles(string p_strDirectory)
		{
			if (String.IsNullOrEmpty(p_strDirectory))
				return m_strFiles.ToArray();
			string strPrefix = p_strDirectory;
			strPrefix = strPrefix.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			strPrefix = strPrefix.Trim(new char[] { Path.DirectorySeparatorChar });
			if (strPrefix.Length > 0)
				strPrefix += Path.DirectorySeparatorChar;
			Set<string> lstFiles = new Set<string>();
			Int32 intStopIndex = 0;
			foreach (string strFile in m_strFiles)
			{
				if (strFile.StartsWith(strPrefix, StringComparison.InvariantCultureIgnoreCase))
				{
					intStopIndex = strFile.IndexOf(Path.DirectorySeparatorChar, strPrefix.Length);
					if (intStopIndex > 0)
						continue;
					lstFiles.Add(strFile);
				}
			}
			return lstFiles.ToArray();
		}

		/// <summary>
		/// Determins if the archive contains the specified file.
		/// </summary>
		/// <param name="p_strPath">The path of the file whose presence in the archive is to be determined.</param>
		/// <returns><lang cref="true"/> if the file is in the archive;
		/// <lang cref="false"/> otherwise.</returns>
		public bool ContainsFile(string p_strPath)
		{
			string strPath = p_strPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLowerInvariant();
			return m_dicFileIndex.ContainsKey(strPath);
		}

		/// <summary>
		/// Gets the contents of the specified file in the archive.
		/// </summary>
		/// <param name="p_strPath">The file whose contents are to be retrieved.</param>
		/// <returns>The contents of the specified file in the archive.</returns>
		public byte[] GetFileContents(string p_strPath)
		{
			string strPath = p_strPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLowerInvariant();
			if (!m_dicFileIndex.ContainsKey(strPath))
				throw new FileNotFoundException("The requested file does not exist in the archive.", p_strPath);

			byte[] bteFile = null;
			using (SevenZipExtractor szeExtractor = new SevenZipExtractor(m_strPath))
			{
				ArchiveFileInfo afiFile = szeExtractor.ArchiveFileData[m_dicFileIndex[strPath]];
				bteFile = new byte[afiFile.Size];
				using (MemoryStream msmFile = new MemoryStream())
				{
					szeExtractor.ExtractFile(afiFile.Index, msmFile);
					msmFile.Position = 0;
					for (Int32 intOffset = 0, intRead = 0; intOffset < bteFile.Length && ((intRead = msmFile.Read(bteFile, intOffset, bteFile.Length - intOffset)) >= 0); intOffset += intRead) ;
					msmFile.Close();
				}
			}
			return bteFile;
		}

		/// <summary>
		/// Replaces the specified file in the archive with the given data.
		/// </summary>
		/// <remarks>
		/// If the specified file doesn't exist in the archive, the file is added.
		/// </remarks>
		/// <param name="p_strFileName">The path to the file to replace in the archive.</param>
		/// <param name="p_strData">The new file data.</param>
		public void ReplaceFile(string p_strFileName, string p_strData)
		{
			ReplaceFile(p_strFileName, Encoding.Default.GetBytes(p_strData));
		}

		/// <summary>
		/// Replaces the specified file in the archive with the given data.
		/// </summary>
		/// <remarks>
		/// If the specified file doesn't exist in the archive, the file is added.
		/// </remarks>
		/// <param name="p_strFileName">The path to the file to replace in the archive.</param>
		/// <param name="p_bteData">The new file data.</param>
		/// <exception cref="InvalidOperationException">Thrown if modification of archives of the current
		/// archive type is not supported.</exception>
		public void ReplaceFile(string p_strFileName, byte[] p_bteData)
		{
			if (!m_booCanEdit)
				using (SevenZipExtractor szeExtractor = new SevenZipExtractor(m_strPath))
					throw new InvalidOperationException("Cannot modify archive of type: " + szeExtractor.Format);
			string strPath = p_strFileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLowerInvariant();
			if (m_dicFileIndex.ContainsKey(strPath))
			{
				Dictionary<int, string> dicDelete = new Dictionary<int, string>() { { m_dicFileIndex[strPath], null } };
				m_szcCompressor.ModifyArchive(m_strPath, dicDelete);
			}
			using (MemoryStream msmData = new MemoryStream(p_bteData))
			{
				m_szcCompressor.CompressStreamDictionary(new Dictionary<string, Stream>() { { p_strFileName, msmData } }, m_strPath);
				msmData.Close();
			}
			LoadFileIndices();
		}

		/// <summary>
		/// Deletes the specified file from the archive.
		/// </summary>
		/// <remarks>
		/// If the specified file doesn't exist in the archive, nothing is done.
		/// </remarks>
		/// <param name="p_strFileName">The path to the file to delete from the archive.</param>
		/// <exception cref="InvalidOperationException">Thrown if modification of archives of the current
		/// archive type is not supported.</exception>
		public void DeleteFile(string p_strFileName)
		{
			if (!m_booCanEdit)
				using (SevenZipExtractor szeExtractor = new SevenZipExtractor(m_strPath))
					throw new InvalidOperationException("Cannot modify archive of type: " + szeExtractor.Format);
			string strPath = p_strFileName.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLowerInvariant();
			if (m_dicFileIndex.ContainsKey(strPath))
			{
				Dictionary<int, string> dicDelete = new Dictionary<int, string>() { { m_dicFileIndex[strPath], null } };
				m_szcCompressor.ModifyArchive(m_strPath, dicDelete);
			}
			LoadFileIndices();
		}

		#region IDisposable Members

		/// <summary>
		/// Disposes of the resources used by the object.
		/// </summary>
		public void Dispose()
		{
		}

		#endregion
	}
}