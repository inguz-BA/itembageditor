/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Serilog;

namespace ItemBagEditor.Services
{
    public class RegistrySettingsService
    {
        private readonly ILogger _logger;
        private readonly string _registryKeyPath;
        private readonly RegistryKey _registryKey;

        public RegistrySettingsService()
        {
            _logger = Log.ForContext<RegistrySettingsService>();
            _registryKeyPath = @"SOFTWARE\Inguz\ItemBagEditor";
            
            try
            {
                // Try to open the registry key, create if it doesn't exist
                _registryKey = Registry.CurrentUser.OpenSubKey(_registryKeyPath, true);
                if (_registryKey == null)
                {
                    _registryKey = Registry.CurrentUser.CreateSubKey(_registryKeyPath);
                    if (_registryKey == null)
                    {
                        throw new InvalidOperationException($"Failed to create registry key: {_registryKeyPath}");
                    }
                    _logger.Information("Created new registry key: {RegistryKey}", _registryKeyPath);
                }
                else
                {
                    _logger.Debug("Opened existing registry key: {RegistryKey}", _registryKeyPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error accessing registry key: {RegistryKey}", _registryKeyPath);
                throw;
            }
        }

        public void SetValue(string name, string value)
        {
            try
            {
                if (_registryKey != null)
                {
                    _registryKey.SetValue(name, value);
                    _logger.Debug("Set registry value: {Name} = {Value}", name, value);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error setting registry value: {Name} = {Value}", name, value);
                throw;
            }
        }

        public void SetValue(string name, bool value)
        {
            SetValue(name, value.ToString());
        }

        public void SetValue(string name, int value)
        {
            SetValue(name, value.ToString());
        }

        public string GetValue(string name, string defaultValue = "")
        {
            try
            {
                if (_registryKey != null)
                {
                    var value = _registryKey.GetValue(name, defaultValue)?.ToString() ?? defaultValue;
                    _logger.Debug("Get registry value: {Name} = {Value}", name, value);
                    return value;
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting registry value: {Name}", name);
                return defaultValue;
            }
        }

        public bool GetBoolValue(string name, bool defaultValue = false)
        {
            try
            {
                var value = GetValue(name, defaultValue.ToString());
                return bool.TryParse(value, out bool result) ? result : defaultValue;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting bool registry value: {Name}", name);
                return defaultValue;
            }
        }

        public int GetIntValue(string name, int defaultValue = 0)
        {
            try
            {
                var value = GetValue(name, defaultValue.ToString());
                return int.TryParse(value, out int result) ? result : defaultValue;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting int registry value: {Name}", name);
                return defaultValue;
            }
        }

        public Dictionary<string, string> GetAllValues()
        {
            var values = new Dictionary<string, string>();
            
            try
            {
                if (_registryKey != null)
                {
                    var valueNames = _registryKey.GetValueNames();
                    foreach (var name in valueNames)
                    {
                        var value = _registryKey.GetValue(name)?.ToString() ?? "";
                        values[name] = value;
                    }
                    _logger.Debug("Retrieved {Count} registry values", values.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting all registry values");
            }
            
            return values;
        }

        public void DeleteValue(string name)
        {
            try
            {
                if (_registryKey != null)
                {
                    _registryKey.DeleteValue(name, false);
                    _logger.Debug("Deleted registry value: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting registry value: {Name}", name);
                throw;
            }
        }

        public void ClearAllValues()
        {
            try
            {
                if (_registryKey != null)
                {
                    var valueNames = _registryKey.GetValueNames();
                    foreach (var name in valueNames)
                    {
                        _registryKey.DeleteValue(name, false);
                    }
                    _logger.Information("Cleared all registry values");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error clearing all registry values");
                throw;
            }
        }

        public Dictionary<string, string> LoadSettings()
        {
            return GetAllValues();
        }

        public void SaveSettings(Dictionary<string, string> settings)
        {
            try
            {
                foreach (var setting in settings)
                {
                    SetValue(setting.Key, setting.Value);
                }
                _logger.Information("Saved {Count} settings to registry", settings.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error saving settings to registry");
                throw;
            }
        }

        public void Dispose()
        {
            _registryKey?.Dispose();
        }
    }
}
