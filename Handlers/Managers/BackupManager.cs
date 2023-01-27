﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Flarial.Launcher.Managers
{
    public class BackupManager
    {
        public class BackupConfiguration
        {
            public DateTime BackupTime
            {
                get;
                set;
            }
            public string MinecraftVersion
            {
                get;
                set;
            }
            public Guid BackupId
            {
                get;
                set;
            }
        }

        public static async Task DeleteBackup(string backupName)
        {
            await DeleteDirectory(
                   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
               "\\Flarial\\Launcher" +
                "\\Backup\\" +
                backupName
            );
        }

        public static async Task DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                await DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        public static async Task<List<string>> FilterByName(string filterName)
        {
            var UnFilteredBackups = await GetAllBackupsAsync();
            var filteredBackups = UnFilteredBackups
                .Where(backup => backup.StartsWith(filterName))
                .ToList();

            return filteredBackups;
        }
        public static async Task<List<string>> GetAllBackupsAsync()
        {
            List<string> list = new List<string>();
            foreach (
                var backup in Directory.GetDirectories(
                       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                   "\\Flarial\\Launcher" +
                    "\\Backup"
                )
            )
            {
                list.Add(new DirectoryInfo(backup).Name);
                await Task.Delay(1);
            }
            return list;
        }
        public static async Task Backupdata(string backupName)
        {
            try
            {
                if (
                    Directory.Exists(
                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                       "\\Flarial\\Launcher" +
                        "\\Backup\\" +
                        backupName
                    )
                )
                {
                    return;
                }
                #region Paths
                var mcpath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\LocalState\\games"
                );
                var FlarialPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState"
                );
                #endregion

                var dirm = Directory.CreateDirectory(
                       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                   "\\Flarial\\Launcher" +
                    "\\Backup"
                );

                #region Com.mojang
                if (Directory.Exists(mcpath))
                {
                    // string Name;
                    foreach (var dir in Directory.GetDirectories(mcpath))
                    {
                        FileAttributes attributes = File.GetAttributes(dir);

                        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // Make the file modifiable.
                            attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                            File.SetAttributes(dir, attributes);
                            Console.WriteLine("The {0} file is no longer read only.", dir);
                        }

                        await DirectoryCopy(
                            dir,
                            Path.Combine(
                                   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                               "\\Flarial\\Launcher" +
                                "\\Backup" +
                                "\\" +
                                backupName +
                                "\\com.mojang"
                            ),
                            true
                        );
                    }
                }
                else
                {
                    MessageBox.Show("Minecraft Data Path is invalid!", "Failed To Backup");
                }
                #endregion

                if (Directory.Exists(FlarialPath))
                {
                    foreach (var dir in Directory.GetDirectories(FlarialPath))
                    {
                        var DirecInfo = new DirectoryInfo(dir);

                        FileAttributes attributes = File.GetAttributes(dir);

                        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            // Make the file modifiable.
                            attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                            File.SetAttributes(dir, attributes);
                            // Console.WriteLine("The {0} file is no longer read
                            // only.", mcpath);
                        }

                        Directory.CreateDirectory(Path.Combine(
                                Environment.GetFolderPath(
                                 (Environment.SpecialFolder.LocalApplicationData)
                                ) +
                               "\\Flarial\\Launcher" +
                                "\\Backup" +
                                "\\" +
                                backupName,
                              "RoamingState"));

                        await DirectoryCopy(
                            dir,
                            Path.Combine(
                                Environment.GetFolderPath(
                                 (Environment.SpecialFolder.LocalApplicationData)
                                ) +
                               "\\Flarial\\Launcher" +
                                "\\Backup" +
                                "\\" +
                                backupName +
                              "\\RoamingState",
                                DirecInfo.Name
                            ),
                            true
                        );

                    }
                }
                else
                {
                    MessageBox.Show("Roaming State Data Path is invalid!", "Failed To Backup");
                }


                await Task.Delay(1000);
                var text = await CreateBackupConfig();

                File.WriteAllText(
                    Path.Combine(
                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                       "\\Flarial\\Launcher" +
                        "\\Backup" +
                        "\\" +
                        backupName,
                        "BackupConfig.json"
                    ),
                    text
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public static async Task<string> CreateBackupConfig()
        {

            var version = Minecraft.GetVersion();
            await Task.Delay(1);
            var backupConfig = new BackupConfiguration
            {
                BackupTime = DateTime.Now,
                MinecraftVersion = version.ToString(),
                BackupId = Guid.NewGuid(),
            };

            string jsonString = JsonSerializer.Serialize(backupConfig);

            return jsonString;
        }

        public static async Task<BackupConfiguration> GetBackupConfig(string BackupId)
        {
            var Path =
                   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
               "\\Flarial\\Launcher" +
                "\\Backup\\" +
                BackupId +
                "\\BackupConfig.json";

            // convert string to stream
            FileStream openStream = File.OpenRead(Path);

            var backupConfig = await JsonSerializer.DeserializeAsync<BackupConfiguration>(
                openStream
            );

            if (backupConfig == null)
            {
                return null;
            }
            else
            {
                return backupConfig;
            }
        }

        public static async Task LoadBackedupData(string backupName)
        {
            Console.WriteLine(backupName);
            try
            {
                var mcpath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\LocalState\\games\\com.mojang"
                );
                if (
                    !Directory.Exists(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                           "\\Flarial\\Launcher" +
                            "\\Backup\\" +
                            backupName,
                            "com.mojang"
                        )
                    )
                )
                {
                    MessageBox.Show(
                        "You have no Minecraft backups available with the Id given.",
                        "Failed to Load Backup"
                    );
                    return;
                }
                else
                {
                    await DirectoryCopy(
                        Path.Combine(
                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                           "\\Flarial\\Launcher" +
                            "\\Backup\\" +
                            backupName,
                            "com.mojang"
                        ),
                        mcpath,
                        true
                    );
                }

                var FlarialPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState"
                );

                if (
                    !Directory.Exists(
                        Path.Combine(
                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                           "\\Flarial\\Launcher" +
                            "\\Backup\\" +
                            backupName,
                            "RoamingState"
                        )
                    )
                )
                {
                    MessageBox.Show(
                        "You have no client backups available with the Id given.",
                        "Failed to Load Backup"
                    );
                    return;
                }
                else
                {
                    await DirectoryCopy(
                        Path.Combine(
                               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                           "\\Flarial\\Launcher" +
                            "\\Backup\\" +
                            backupName,
                            "RoamingState"
                        ),
                        FlarialPath,
                        true
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static FileAttributes RemoveAttribute(
            FileAttributes attributes,
            FileAttributes attributesToRemove
        )
        {
            return attributes & ~attributesToRemove;
        }

        private static async Task DirectoryCopy(
            string sourceDirName,
            string destDirName,
            bool copySubDirs
        )
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: " + sourceDirName
                );
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
                Console.WriteLine("Copying " + file + " to " + tempPath);
            }

            // If copying subdirectories, copy them and their contents to new
            // location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    await DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}