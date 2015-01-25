﻿using System;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Processes;
using NzbDrone.Core.Backup;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Update.Commands;

namespace NzbDrone.Core.Update
{
    public class InstallUpdateService : IExecute<ApplicationUpdateCommand>, IExecute<InstallUpdateCommand>
    {
        private readonly ICheckUpdateService _checkUpdateService;
        private readonly Logger _logger;
        private readonly IAppFolderInfo _appFolderInfo;

        private readonly IDiskProvider _diskProvider;
        private readonly IHttpClient _httpClient;
        private readonly IArchiveService _archiveService;
        private readonly IProcessProvider _processProvider;
        private readonly IVerifyUpdates _updateVerifier;
        private readonly IStartupContext _startupContext;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly IBackupService _backupService;


        public InstallUpdateService(ICheckUpdateService checkUpdateService, IAppFolderInfo appFolderInfo,
                                    IDiskProvider diskProvider, IHttpClient httpClient,
                                    IArchiveService archiveService, IProcessProvider processProvider,
                                    IVerifyUpdates updateVerifier,
                                    IStartupContext startupContext,
                                    IConfigFileProvider configFileProvider,
                                    IRuntimeInfo runtimeInfo,
                                    IBackupService backupService,
                                    Logger logger)
        {
            if (configFileProvider == null)
            {
                throw new ArgumentNullException("configFileProvider");
            }
            _checkUpdateService = checkUpdateService;
            _appFolderInfo = appFolderInfo;
            _diskProvider = diskProvider;
            _httpClient = httpClient;
            _archiveService = archiveService;
            _processProvider = processProvider;
            _updateVerifier = updateVerifier;
            _startupContext = startupContext;
            _configFileProvider = configFileProvider;
            _runtimeInfo = runtimeInfo;
            _backupService = backupService;
            _logger = logger;
        }

        private void InstallUpdate(UpdatePackage updatePackage)
        {
            try
            {
                EnsureAppDataSafety();

                if (OsInfo.IsWindows || _configFileProvider.UpdateMechanism != UpdateMechanism.Script)
                {
                    if (!_diskProvider.FolderWritable(_appFolderInfo.StartUpFolder))
                    {
                        throw new UpdateFolderNotWritableException("Cannot install update because startup folder '{0}' is not writable by the user '{1}'.", _appFolderInfo.StartUpFolder, Environment.UserName);
                    }
                }

                var updateSandboxFolder = _appFolderInfo.GetUpdateSandboxFolder();

                var packageDestination = Path.Combine(updateSandboxFolder, updatePackage.FileName);

                if (_diskProvider.FolderExists(updateSandboxFolder))
                {
                    _logger.Info("Deleting old update files");
                    _diskProvider.DeleteFolder(updateSandboxFolder, true);
                }

                _logger.ProgressInfo("Downloading update {0}", updatePackage.Version);
                _logger.Debug("Downloading update package from [{0}] to [{1}]", updatePackage.Url, packageDestination);
                _httpClient.DownloadFile(updatePackage.Url, packageDestination);

                _logger.ProgressInfo("Verifying update package");

                if (!_updateVerifier.Verify(updatePackage, packageDestination))
                {
                    _logger.Error("Update package is invalid");
                    throw new UpdateVerificationFailedException("Update file '{0}' is invalid", packageDestination);
                }

                _logger.Info("Update package verified successfully");

                _logger.ProgressInfo("Extracting Update package");
                _archiveService.Extract(packageDestination, updateSandboxFolder);
                _logger.Info("Update package extracted successfully");

                _backupService.Backup(BackupType.Update);

                if (OsInfo.IsNotWindows && _configFileProvider.UpdateMechanism == UpdateMechanism.Script)
                {
                    InstallUpdateWithScript(updateSandboxFolder);
                    return;
                }

                _logger.Info("Preparing client");
                _diskProvider.MoveFolder(_appFolderInfo.GetUpdateClientFolder(),
                                            updateSandboxFolder);

                _logger.Info("Starting update client {0}", _appFolderInfo.GetUpdateClientExePath());
                _logger.ProgressInfo("Sonarr will restart shortly.");

                _processProvider.Start(_appFolderInfo.GetUpdateClientExePath(), GetUpdaterArgs(updateSandboxFolder));

                return;
            }
            catch (UpdateFailedException ex)
            {
                _logger.ErrorException("Update process failed", ex);
                throw;
            }
        }

        private void InstallUpdateWithScript(String updateSandboxFolder)
        {
            var scriptPath = _configFileProvider.UpdateScriptPath;

            if (scriptPath.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Update Script has not been defined");
            }

            if (!_diskProvider.FileExists(scriptPath, StringComparison.Ordinal))
            {
                var message = String.Format("Update Script: '{0}' does not exist", scriptPath);
                throw new FileNotFoundException(message, scriptPath);
            }

            _logger.Info("Removing NzbDrone.Update");
            _diskProvider.DeleteFolder(_appFolderInfo.GetUpdateClientFolder(), true);

            _logger.ProgressInfo("Starting update script: {0}", _configFileProvider.UpdateScriptPath);
            _processProvider.Start(scriptPath, GetUpdaterArgs(updateSandboxFolder));
        }

        private string GetUpdaterArgs(string updateSandboxFolder)
        {
            var processId = _processProvider.GetCurrentProcess().Id.ToString();
            var executingApplication = _runtimeInfo.ExecutingApplication;

            return String.Join(" ", processId, updateSandboxFolder.TrimEnd(Path.DirectorySeparatorChar).WrapInQuotes(), executingApplication.WrapInQuotes(), _startupContext.PreservedArguments);
        }

        private void EnsureAppDataSafety()
        {
            if (_appFolderInfo.StartUpFolder.IsParentPath(_appFolderInfo.AppDataFolder) ||
                _appFolderInfo.StartUpFolder.PathEquals(_appFolderInfo.AppDataFolder))
            {
                throw new NotSupportedException("Update will cause AppData to be deleted, correct you configuration before proceeding");
            }
        }

        private void ExecuteInstallUpdate(Command message, UpdatePackage package)
        {
            try
            {
                InstallUpdate(package);

                message.Completed("Restarting Sonarr to apply updates");
            }
            catch (UpdateFolderNotWritableException ex)
            {
                message.Failed(ex, string.Format("Startup folder not writable by user '{0}'", Environment.UserName));
            }
            catch (UpdateVerificationFailedException ex)
            {
                message.Failed(ex, "Update verification failed");
            }
        }

        public void Execute(ApplicationUpdateCommand message)
        {
            _logger.ProgressDebug("Checking for updates");
            var latestAvailable = _checkUpdateService.AvailableUpdate();

            if (latestAvailable != null)
            {
                ExecuteInstallUpdate(message, latestAvailable);
            }
        }

        public void Execute(InstallUpdateCommand message)
        {
            var latestAvailable = _checkUpdateService.AvailableUpdate();

            if (latestAvailable == null || latestAvailable.Hash != message.UpdatePackage.Hash)
            {
                throw new ApplicationException("Unknown or invalid update specified");
            }

            ExecuteInstallUpdate(message, latestAvailable);
        }
    }
}
