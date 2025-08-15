/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using ItemBagEditor.Services;

namespace ItemBagEditor
{
    public partial class SettingsDialog : Window
    {
        private readonly ILogger _logger;
        private readonly RegistrySettingsService _registrySettings;
        private bool _isInitializing = true;

        public SettingsDialog()
        {
            InitializeComponent();
            
            // Get logger from application
            _logger = Log.ForContext<SettingsDialog>();
            
            // Initialize registry settings service
            _registrySettings = new RegistrySettingsService();
            
            InitializeControls();
            LoadSettings();
            _isInitializing = false;
        }

        private void InitializeControls()
        {
            // Initialize log level combo box
            cboLogLevel.Items.Clear();
            cboLogLevel.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Debug", Tag = "Debug" });
            cboLogLevel.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Information", Tag = "Information" });
            cboLogLevel.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Warning", Tag = "Warning" });
            cboLogLevel.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Error", Tag = "Error" });
            cboLogLevel.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Fatal", Tag = "Fatal" });
            
            // Set default log level
            cboLogLevel.SelectedIndex = 1; // Information
            
            // Set default file logging to enabled
            chkEnableFileLogging.IsChecked = true;
            
            // Set log file path
            txtLogFilePath.Text = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "logs", 
                "ItemBagEditor-.log"
            );
        }

        private void LoadSettings()
        {
            try
            {
                // Load settings from registry
                var settings = _registrySettings.LoadSettings();
                
                // Load file paths
                if (settings.ContainsKey("ItemListPath"))
                    txtItemListPath.Text = settings["ItemListPath"];
                
                if (settings.ContainsKey("ItemBagFolderPath"))
                    txtItemBagFolderPath.Text = settings["ItemBagFolderPath"];
                

                
                // Load logging settings
                if (settings.ContainsKey("EnableFileLogging"))
                    chkEnableFileLogging.IsChecked = bool.Parse(settings["EnableFileLogging"]);
                
                if (settings.ContainsKey("LogLevel"))
                {
                    var logLevel = settings["LogLevel"];
                    var item = cboLogLevel.Items.Cast<System.Windows.Controls.ComboBoxItem>()
                        .FirstOrDefault(x => x.Tag.ToString() == logLevel);
                    if (item != null)
                        cboLogLevel.SelectedItem = item;
                }
                
                // Load advanced settings
                if (settings.ContainsKey("AutoSave"))
                    chkAutoSave.IsChecked = bool.Parse(settings["AutoSave"]);
                
                if (settings.ContainsKey("BackupOnSave"))
                    chkBackupOnSave.IsChecked = bool.Parse(settings["BackupOnSave"]);
                
                _logger.Information("Settings loaded successfully from registry");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading settings");
                System.Windows.MessageBox.Show(
                    $"Error loading settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Save settings to registry
                _registrySettings.SaveSettings(new Dictionary<string, string>
                {
                    { "ItemListPath", txtItemListPath.Text ?? "" },
                    { "ItemBagFolderPath", txtItemBagFolderPath.Text ?? "" },
                    { "EnableFileLogging", (chkEnableFileLogging.IsChecked ?? false).ToString() },
                    { "LogLevel", ((System.Windows.Controls.ComboBoxItem)cboLogLevel.SelectedItem)?.Tag?.ToString() ?? "Information" },
                    { "AutoSave", (chkAutoSave.IsChecked ?? false).ToString() },
                    { "BackupOnSave", (chkBackupOnSave.IsChecked ?? true).ToString() }
                });
                
                // Update logging configuration if needed
                UpdateLoggingConfiguration();
                
                _logger.Information("Settings saved successfully to registry");
                
                System.Windows.MessageBox.Show(
                    "Settings saved successfully! Some changes may require restarting the application.",
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving settings");
                System.Windows.MessageBox.Show(
                    $"Error saving settings: {ex.Message}",
                    "Settings Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void UpdateLoggingConfiguration()
        {
            try
            {
                // Create or update logging.config file
                var loggingConfigPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "logging.config"
                );
                
                var enableLogging = chkEnableFileLogging.IsChecked == true;
                File.WriteAllText(loggingConfigPath, enableLogging.ToString().ToLower());
                
                _logger.Information("Logging configuration updated: File logging {Status}", 
                    enableLogging ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating logging configuration");
            }
        }

        private void btnBrowseItemList_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select ItemList.xml",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                FileName = "ItemList.xml"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                txtItemListPath.Text = openFileDialog.FileName;
            }
        }

        private void btnBrowseItemBagFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
            {
                Title = "Select ItemBag folder",
                IsFolderPicker = true
            };
            
            if (folderBrowserDialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                txtItemBagFolderPath.Text = folderBrowserDialog.FileName;
            }
        }



        private void chkEnableFileLogging_Checked(object sender, RoutedEventArgs e)
        {
            if (!_isInitializing)
            {
                cboLogLevel.IsEnabled = true;
                txtLogFilePath.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void chkEnableFileLogging_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_isInitializing)
            {
                cboLogLevel.IsEnabled = false;
                txtLogFilePath.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void cboLogLevel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!_isInitializing && cboLogLevel.SelectedItem != null)
            {
                var selectedLevel = ((System.Windows.Controls.ComboBoxItem)cboLogLevel.SelectedItem).Tag.ToString();
                _logger.Information("Log level changed to {LogLevel}", selectedLevel);
            }
        }

        private void btnResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Reset to defaults
                    txtItemListPath.Text = "";
                    txtItemBagFolderPath.Text = "";
                    chkEnableFileLogging.IsChecked = true;
                    cboLogLevel.SelectedIndex = 1; // Information
                    chkAutoSave.IsChecked = false;
                    chkBackupOnSave.IsChecked = true;
                    
                    // Save defaults to registry
                    _registrySettings.SaveSettings(new Dictionary<string, string>
                    {
                        { "ItemListPath", "" },
                        { "ItemBagFolderPath", "" },
                        { "EnableFileLogging", "True" },
                        { "LogLevel", "Information" },
                        { "AutoSave", "False" },
                        { "BackupOnSave", "True" }
                    });
                    
                    _logger.Information("Settings reset to defaults and saved to registry");
                    
                    System.Windows.MessageBox.Show(
                        "Settings have been reset to defaults and saved to registry.",
                        "Settings Reset",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error resetting settings to defaults");
                    System.Windows.MessageBox.Show(
                        $"Error resetting settings: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate settings
            if (string.IsNullOrWhiteSpace(txtItemBagFolderPath.Text))
            {
                System.Windows.MessageBox.Show(
                    "Please specify a path for the ItemBag folder.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }
            
            if (!Directory.Exists(txtItemBagFolderPath.Text))
            {
                var result = System.Windows.MessageBox.Show(
                    "The specified ItemBag folder does not exist. Would you like to create it?",
                    "Folder Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(txtItemBagFolderPath.Text);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(
                            $"Error creating folder: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            
            SaveSettings();
            DialogResult = true;
            Close();
        }

        // Public properties to access settings from main window
        public string ItemListPath => txtItemListPath.Text;
        public string ItemBagFolderPath => txtItemBagFolderPath.Text;
        public bool EnableFileLogging => chkEnableFileLogging.IsChecked == true;
        public string LogLevel => ((System.Windows.Controls.ComboBoxItem)cboLogLevel.SelectedItem)?.Tag?.ToString() ?? "Information";
        public bool AutoSave => chkAutoSave.IsChecked == true;
        public bool BackupOnSave => chkBackupOnSave.IsChecked == true;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _registrySettings?.Dispose();
        }
    }
}
