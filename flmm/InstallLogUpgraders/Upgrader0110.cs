﻿using System;
using System.Xml;
using Fomm.PackageManager.ModInstallLog;

namespace Fomm.InstallLogUpgraders
{
  /// <summary>
  ///   Upgrades the Install Log to the current version from version 0.1.1.0.
  /// </summary>
  internal class Upgrader0110 : Upgrader
  {
    /// <summary>
    ///   Upgrades the Install Log to the current version from version 0.1.1.0.
    /// </summary>
    /// <remarks>
    ///   This method is called by a background worker to perform the actual upgrade.
    /// </remarks>
    protected override void DoUpgrade()
    {
      InstallLog.Current.SetInstallLogVersion(InstallLog.CURRENT_VERSION);
      InstallLog.Current.Save();

      var xmlInstallLog = new XmlDocument();
      xmlInstallLog.Load(InstallLog.Current.InstallLogPath);

      var xndRoot = xmlInstallLog.SelectSingleNode("installLog");
      var xndSdpEdits = xndRoot.SelectSingleNode("sdpEdits");

      ProgressWorker.OverallProgressStep = 1;
      ProgressWorker.OverallProgressMaximum = xndSdpEdits.ChildNodes.Count;
      ProgressWorker.ShowItemProgress = false;

      //remove the sdp edit node...
      xndSdpEdits.ParentNode.RemoveChild(xndSdpEdits);
      //...and replace it with the game-specific edits node
      var xndGameSpecificsValueEdits = xndRoot.AppendChild(xmlInstallLog.CreateElement("gameSpecificEdits"));
      foreach (XmlNode xndSdpEdit in xndSdpEdits.ChildNodes)
      {
        ProgressWorker.StepOverallProgress();
        var xndGameSpecificsValueEdit = xndGameSpecificsValueEdits.AppendChild(xmlInstallLog.CreateElement("edit"));
        var strValueKey = String.Format("sdp:{0}/{1}", xndGameSpecificsValueEdits.Attributes["package"].Value,
                                        xndGameSpecificsValueEdits.Attributes["shader"].Value);
        xndGameSpecificsValueEdit.Attributes.Append(xmlInstallLog.CreateAttribute("key")).Value = strValueKey;
        xndGameSpecificsValueEdit.AppendChild(xndSdpEdit.FirstChild.Clone());
      }
      xmlInstallLog.Save(InstallLog.Current.InstallLogPath);
    }
  }
}