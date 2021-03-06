﻿using System;
using System.IO;
using System.Windows.Forms;
using Fomm.PackageManager.ModInstallLog;
using Fomm.PackageManager.XmlConfiguredInstall;

namespace Fomm.PackageManager
{
  /// <summary>
  ///   Installs a <see cref="fomod" />.
  /// </summary>
  public class ModInstaller : ModInstallerBase
  {
    #region Properties

    protected BackgroundWorkerProgressDialog ProgressDialog { get; private set; }

    /// <seealso cref="ModInstallScript.ExceptionMessage" />
    protected override string ExceptionMessage
    {
      get
      {
        return "A problem occurred during install: " + Environment.NewLine + "{0}" + Environment.NewLine +
               "The mod was not installed.";
      }
    }

    /// <seealso cref="ModInstallScript.SuccessMessage" />
    protected override string SuccessMessage
    {
      get
      {
        return "The mod was successfully installed.";
      }
    }

    /// <seealso cref="ModInstallScript.FailMessage" />
    protected override string FailMessage
    {
      get
      {
        return "The mod was not installed.";
      }
    }

    #endregion

    #region Constructors

    /// <summary>
    ///   A simple constructor that initializes the object.
    /// </summary>
    /// <param name="p_fomodMod">The <see cref="fomod" /> to be installed.</param>
    internal ModInstaller(fomod p_fomodMod)
      : base(p_fomodMod) {}

    #endregion

    #region Install Methods

    /// <summary>
    ///   Indicates that this script's work has already been completed if
    ///   the <see cref="Fomod" /> is already active.
    /// </summary>
    /// <returns>
    ///   <lang langref="true" /> if the <see cref="Fomod" /> is active;
    ///   <lang langref="false" /> otherwise.
    /// </returns>
    /// <seealso cref="ModInstallScript.CheckAlreadyDone()" />
    protected override bool CheckAlreadyDone()
    {
      return Fomod.IsActive;
    }

    /// <summary>
    ///   Installs the mod and activates it.
    /// </summary>
    internal void Install()
    {
      Run();
    }

    /// <summary>
    ///   Installs the mod and activates it.
    /// </summary>
    protected override bool DoScript()
    {
      foreach (var strSettingsFile in Program.GameMode.SettingsFiles.Values)
      {
        TransactionalFileManager.Snapshot(strSettingsFile);
      }
      foreach (var strAdditionalFile in Program.GameMode.AdditionalPaths.Values)
      {
        if (File.Exists(strAdditionalFile))
        {
          TransactionalFileManager.Snapshot(strAdditionalFile);
        }
      }
      TransactionalFileManager.Snapshot(InstallLog.Current.InstallLogPath);

      try
      {
        MergeModule = new InstallLogMergeModule();
        if (Fomod.HasInstallScript)
        {
          var fscInstallScript = Fomod.GetInstallScript();
          switch (fscInstallScript.Type)
          {
            case FomodScriptType.CSharp:
              Fomod.IsActive = RunCustomInstallScript();
              break;
            case FomodScriptType.XMLConfig:
              Fomod.IsActive = RunXmlInstallScript();
              break;
          }
        }
        else
        {
          Fomod.IsActive = RunBasicInstallScript("Installing Fomod");
        }

        if (Fomod.IsActive)
        {
          InstallLog.Current.Merge(Fomod, MergeModule);
          Script.CommitActivePlugins();
        }
      }
      catch (Exception e)
      {
        Fomod.IsActive = false;
        throw e;
      }
      if (!Fomod.IsActive)
      {
        return false;
      }
      return true;
    }

    /// <summary>
    ///   Runs the XML configured install script.
    /// </summary>
    /// <returns>
    ///   <lang langref="true" /> if the installation was successful;
    ///   <lang langref="false" /> otherwise.
    /// </returns>
    protected bool RunXmlInstallScript()
    {
      var xmlScript = new XmlConfiguredScript(Script);
      return xmlScript.Install();
    }

    /// <summary>
    ///   Runs the custom install script included in the fomod.
    /// </summary>
    /// <returns>
    ///   <lang langref="true" /> if the installation was successful;
    ///   <lang langref="false" /> otherwise.
    /// </returns>
    protected bool RunCustomInstallScript()
    {
      var strScript = Fomod.GetInstallScript().Text;
      return ScriptCompiler.Execute(strScript, this);
    }

    /// <summary>
    ///   Runs the basic install script.
    /// </summary>
    /// <param name="p_strMessage">The message to display in the progress dialog.</param>
    /// <returns>
    ///   <lang langref="true" /> if the installation was successful;
    ///   <lang langref="false" /> otherwise.
    /// </returns>
    protected bool RunBasicInstallScript(string p_strMessage)
    {
      try
      {
        using (ProgressDialog = new BackgroundWorkerProgressDialog(PerformBasicInstall))
        {
          ProgressDialog.OverallMessage = p_strMessage;
          ProgressDialog.ShowItemProgress = false;
          ProgressDialog.OverallProgressStep = 1;
          if (ProgressDialog.ShowDialog() == DialogResult.Cancel)
          {
            return false;
          }
        }
      }
      finally
      {
        ProgressDialog = null;
      }
      return true;
    }

    /// <summary>
    ///   Performs a basic install of the mod.
    /// </summary>
    /// <remarks>
    ///   A basic install installs all of the file in the mod to the Data directory
    ///   or activates all esp and esm files.
    /// </remarks>
    public void PerformBasicInstall()
    {
      var chrDirectorySeperators = new[]
      {
        Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar
      };
      var lstFiles = Fomod.GetFileList();
      if (ProgressDialog != null)
      {
        ProgressDialog.OverallProgressMaximum = lstFiles.Count;
      }
      foreach (var strFile in lstFiles)
      {
        if ((ProgressDialog != null) && ProgressDialog.Cancelled())
        {
          return;
        }
        Script.InstallFileFromFomod(strFile);
        if (Program.GameMode.IsPluginFile(strFile) && strFile.IndexOfAny(chrDirectorySeperators) == -1)
        {
          Script.SetPluginActivation(strFile, true);
        }
        if (ProgressDialog != null)
        {
          ProgressDialog.StepOverallProgress();
        }
      }
    }

    #endregion
  }
}