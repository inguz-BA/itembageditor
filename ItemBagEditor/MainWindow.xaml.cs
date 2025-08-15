/*
 * Copyright © 2025 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using ItemBagEditor.Models;
using ItemBagEditor.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Linq;
using Serilog;

namespace ItemBagEditor
{
    public partial class MainWindow : Window
    {
        private readonly IItemListService _itemListService;
        private readonly IItemBagService _itemBagService;
        private readonly IItemBagItemService _itemBagItemService;
        private readonly ILogger<MainWindow> _logger;
        
        private ItemBag? _currentItemBag;
        private string? _currentItemBagPath;
        private string? _itemListPath;
        private string? _itemBagFolderPath;

        public MainWindow(IItemListService itemListService, IItemBagService itemBagService, IItemBagItemService itemBagItemService, ILogger<MainWindow> logger)
        {
            try
            {
                // Store logger first for immediate use
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _logger.LogInformation("MainWindow constructor started");
                
                // Validate dependencies
                _itemListService = itemListService ?? throw new ArgumentNullException(nameof(itemListService));
                _itemBagService = itemBagService ?? throw new ArgumentNullException(nameof(itemBagService));
                _itemBagItemService = itemBagItemService ?? throw new ArgumentNullException(nameof(itemBagItemService));
                
                _logger.LogInformation("MainWindow dependencies validated");
                
            InitializeComponent();
                _logger.LogInformation("MainWindow InitializeComponent completed");
                
                // Add SelectionChanged event handler to main tabDropSections for auto-saving
                if (tabDropSections != null)
                {
                    tabDropSections.SelectionChanged += (s, e) =>
                    {
                        try
                        {
                            if (_currentItemBag != null)
                            {
                                _logger.LogDebug("Main tab changed, auto-saving configuration");
                                SaveCurrentConfiguration();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error auto-saving configuration on main tab change");
                        }
                    };
                    _logger.LogDebug("Added SelectionChanged event handler to main tabDropSections");
                }
                else
                {
                    _logger.LogWarning("tabDropSections is null, cannot add SelectionChanged event handler");
                }
                
                _logger.LogInformation("MainWindow dependencies injected successfully");
            
            InitializeUI();
                _logger.LogInformation("MainWindow initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fatal error in MainWindow constructor");
                throw;
            }
        }

        private void InitializeUI()
        {
            try
            {
                _logger.LogDebug("Initializing UI components");
                
                // UI controls are now created dynamically in tabs, no need to validate them here
                
                _logger.LogDebug("UI initialization completed");
            
                UpdateStatus("Ready to edit ItemBags");
                
                // Automatically load the embedded ItemList and then auto-load ItemBags
                _ = LoadEmbeddedItemListAndItemBagsAsync();
                
                _logger.LogInformation("UI initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing UI");
                throw;
            }
        }

        private async Task LoadEmbeddedItemListAsync()
        {
            try
            {
                _logger.LogInformation("Loading embedded ItemList automatically");
                UpdateStatus("Loading embedded ItemList...");
                
                var success = await _itemListService.LoadEmbeddedItemListAsync();
                if (success)
                {
                    _logger.LogInformation("Embedded ItemList loaded successfully");
                    var categoryCount = _itemListService.GetCategories().Count;
                    _logger.LogInformation("Found {CategoryCount} categories in embedded ItemList", categoryCount);
                    UpdateStatus($"Embedded ItemList loaded successfully. Found {categoryCount} categories.");
                    
                    if (txtFileInfo != null)
                    {
                        txtFileInfo.Text = "ItemList: Embedded (Built-in)";
                    }
                    else
                    {
                        _logger.LogWarning("txtFileInfo is null, cannot update file info");
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to load embedded ItemList, user can still load external file");
                    UpdateStatus("Embedded ItemList not available. Please load external ItemList.xml file.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading embedded ItemList");
                UpdateStatus("Error loading embedded ItemList. Please load external ItemList.xml file.");
            }
        }

        private async Task LoadEmbeddedItemListAndItemBagsAsync()
        {
            try
            {
                // First load the embedded ItemList
                await LoadEmbeddedItemListAsync();
                
                // Then auto-load ItemBags from settings
                await LoadSettingsAndAutoLoadItemBagsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during startup loading sequence");
            }
        }

        private async Task LoadSettingsAndAutoLoadItemBagsAsync()
        {
            try
            {
                _logger.LogInformation("Loading settings and auto-loading ItemBags");
                
                // Use registry settings instead of file-based settings
                var registrySettings = new RegistrySettingsService();
                var settings = registrySettings.LoadSettings();
                
                // Load ItemBag folder path
                if (settings.ContainsKey("ItemBagFolderPath") && !string.IsNullOrEmpty(settings["ItemBagFolderPath"]))
                {
                    var folderPath = settings["ItemBagFolderPath"];
                    if (Directory.Exists(folderPath))
                    {
                        _logger.LogInformation("Auto-loading ItemBags from configured folder: {FolderPath}", folderPath);
                        _itemBagFolderPath = folderPath;
                        await RefreshItemBagList();
                        
                        // Use Dispatcher.Invoke to update UI from background thread
                        Dispatcher.Invoke(() =>
                        {
                            UpdateStatus($"Auto-loaded ItemBags from: {Path.GetFileName(folderPath)}");
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Configured ItemBag folder does not exist: {FolderPath}", folderPath);
                    }
                }
                else
                {
                    _logger.LogInformation("No ItemBag folder configured for auto-loading");
                }
                
                // Dispose the registry settings service
                registrySettings.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-loading of ItemBags");
                
                // Use Dispatcher.Invoke to update UI from background thread
                Dispatcher.Invoke(() =>
                {
                    UpdateStatus("Error auto-loading ItemBags");
                });
            }
        }

        private async void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Opening settings dialog");
                
                var settingsDialog = new SettingsDialog();
                settingsDialog.Owner = this;
                
                if (settingsDialog.ShowDialog() == true)
                {
                    _logger.LogInformation("Settings saved, updating application configuration");
                    
                    // Update file paths from settings
                    _itemListPath = settingsDialog.ItemListPath;
                    if (!string.IsNullOrEmpty(_itemListPath) && File.Exists(_itemListPath))
                    {
                        var success = await _itemListService.LoadItemListAsync(_itemListPath);
                        if (success)
                        {
                            var categoryCount = _itemListService.GetCategories().Count;
                            UpdateStatus($"ItemList loaded from settings. Found {categoryCount} categories.");
                        }
                    }
                    
                    _itemBagFolderPath = settingsDialog.ItemBagFolderPath;
                    if (!string.IsNullOrEmpty(_itemBagFolderPath))
                    {
                        await RefreshItemBagList();
                    }
                    
                    // Update status
                    UpdateStatus("Settings updated successfully");
                    
                    if (txtFileInfo != null)
                    {
                        txtFileInfo.Text = $"ItemList: {Path.GetFileName(_itemListPath)}";
                    }
                }
                else
                {
                    _logger.LogInformation("Settings dialog cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening settings dialog");
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnOpenItemList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Opening ItemList file dialog");
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select ItemList.xml",
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                FileName = "ItemList.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _itemListPath = openFileDialog.FileName;
                    _logger.LogInformation("ItemList file selected: {FilePath}", _itemListPath);
                UpdateStatus($"Loading ItemList from {_itemListPath}...");

                var success = await _itemListService.LoadItemListAsync(_itemListPath);
                if (success)
                {
                    _logger.LogInformation("ItemList loaded successfully");
                    var categoryCount = _itemListService.GetCategories().Count;
                    _logger.LogInformation("Found {CategoryCount} categories", categoryCount);
                    UpdateStatus($"ItemList loaded successfully. Found {categoryCount} categories.");
                    
                    if (txtFileInfo != null)
                    {
                txtFileInfo.Text = $"ItemList: {Path.GetFileName(_itemListPath)}";
                }
                else
                {
                            _logger.LogWarning("txtFileInfo is null, cannot update file info");
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to load ItemList from {FilePath}", _itemListPath);
                    MessageBox.Show("Failed to load ItemList.xml", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Failed to load ItemList.xml");
                }
                }
                else
                {
                    _logger.LogInformation("ItemList file dialog cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening ItemList file");
                MessageBox.Show($"Error opening ItemList: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void btnOpenItemBag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Opening ItemBag file dialog");
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select ItemBag XML",
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                    var filePath = openFileDialog.FileName;
                    _logger.LogInformation("ItemBag file selected: {FilePath}", filePath);
                    await LoadItemBag(filePath);
                }
                else
                {
                    _logger.LogInformation("ItemBag file dialog cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening ItemBag file");
                MessageBox.Show($"Error opening ItemBag: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSaveItemBag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Save ItemBag button clicked");
                
            if (_currentItemBag == null)
            {
                    _logger.LogWarning("No ItemBag loaded to save");
                MessageBox.Show("No ItemBag loaded to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_currentItemBagPath))
            {
                    _logger.LogInformation("No save path specified, opening save dialog");
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save ItemBag XML",
                    Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                        FileName = $"{_currentItemBag.Config?.Name ?? "Unknown"}.xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _currentItemBagPath = saveFileDialog.FileName;
                        _logger.LogInformation("Save path selected: {FilePath}", _currentItemBagPath);
                }
                else
                {
                        _logger.LogInformation("Save dialog cancelled");
                    return;
                }
            }

                _logger.LogInformation("Saving ItemBag to {FilePath}", _currentItemBagPath);
            UpdateStatus("Saving ItemBag...");
            var success = await _itemBagService.SaveItemBagAsync(_currentItemBagPath, _currentItemBag);
            
            if (success)
            {
                    _logger.LogInformation("ItemBag saved successfully");
                UpdateStatus($"ItemBag saved successfully to {_currentItemBagPath}");
                    await RefreshItemBagList();
            }
            else
            {
                    _logger.LogError("Failed to save ItemBag to {FilePath}", _currentItemBagPath);
                MessageBox.Show("Failed to save ItemBag", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Failed to save ItemBag");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ItemBag");
                MessageBox.Show($"Error saving ItemBag: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void btnRefreshList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Refresh ItemBag list button clicked");
            if (!string.IsNullOrEmpty(_itemBagFolderPath))
            {
                await RefreshItemBagList();
            }
                else
                {
                    _logger.LogWarning("No ItemBag folder path set for refresh");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing ItemBag list");
                MessageBox.Show($"Error refreshing list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private async Task RefreshItemBagList()
        {
            try
            {
                _logger.LogInformation("Refreshing ItemBag list from folder: {FolderPath}", _itemBagFolderPath);
                
                if (string.IsNullOrEmpty(_itemBagFolderPath))
                {
                    _logger.LogWarning("No ItemBag folder path set");
                    return;
                }

                var files = await _itemBagService.GetItemBagFilesAsync(_itemBagFolderPath);
                _logger.LogDebug("Found {FileCount} ItemBag files", files.Count);

                if (lstItemBags != null)
                {
                    var fileItems = files.Select(f => new {
                        FileName = Path.GetFileName(f),
                        FullPath = f
                    }).ToList();

                    // Use Dispatcher.Invoke to update UI from background thread
                    Dispatcher.Invoke(() =>
                    {
                        lstItemBags.ItemsSource = fileItems;
                        lstItemBags.DisplayMemberPath = "FileName";
                        lstItemBags.SelectedValuePath = "FullPath";
                    });

                    _logger.LogDebug("ItemBag list refreshed, found {FileCount} files", files.Count);
                }
                else
                {
                    _logger.LogWarning("lstItemBags is null, cannot update list");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing ItemBag list");
                MessageBox.Show($"Error refreshing ItemBag list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            try
            {
                if (txtStatus != null)
                {
                    txtStatus.Text = message;
                    _logger.LogDebug("Status updated: {Message}", message);
            }
            else
            {
                    _logger.LogWarning("txtStatus is null, cannot update status");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update status");
            }
        }

        private UIElement CreateDropSectionContent(DropSection section)
        {
            try
            {
                if (section == null)
                {
                    _logger.LogWarning("DropSection is null, cannot create content");
                    return new TextBlock { Text = "Error: Section is null", Style = FindResource("ModernTextBlock") as Style };
                }
                
                _logger.LogDebug("Creating drop section content for: {SectionName}", section.DisplayName ?? "Unknown");
                
                // Create main tab control for better organization
                var tabControl = new TabControl();
                
                // Add SelectionChanged event to auto-save when switching tabs
                tabControl.SelectionChanged += (s, e) =>
                {
                    try
                    {
                        if (_currentItemBag != null)
                        {
                            _logger.LogDebug("Tab changed, auto-saving configuration");
                            // Trigger a save of the current configuration
                            SaveCurrentConfiguration();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-saving configuration on tab change");
                    }
                };
                
                // Tab 1: Bag Configuration
                _logger.LogDebug("Creating Bag Configuration tab");
                var bagConfigTab = new TabItem { Header = "Bag Configuration" };
                var bagConfigContent = CreateBagConfigurationTab();
                bagConfigTab.Content = bagConfigContent;
                tabControl.Items.Add(bagConfigTab);
                _logger.LogDebug("Added Bag Configuration tab");
                
                // Tab 2: Summon Book
                _logger.LogDebug("Creating Summon Book tab");
                var summonBookTab = new TabItem { Header = "Summon Book" };
                var summonBookContent = CreateSummonBookTab();
                summonBookTab.Content = summonBookContent;
                tabControl.Items.Add(summonBookTab);
                _logger.LogDebug("Added Summon Book tab");
                
                // Tab 3: Add Coin
                _logger.LogDebug("Creating Add Coin tab");
                var addCoinTab = new TabItem { Header = "Add Coin" };
                var addCoinContent = CreateAddCoinTab();
                addCoinTab.Content = addCoinContent;
                tabControl.Items.Add(addCoinTab);
                _logger.LogDebug("Added Add Coin tab");
                
                // Tab 4: Ruud
                _logger.LogDebug("Creating Ruud tab");
                var ruudTab = new TabItem { Header = "Ruud" };
                var ruudContent = CreateRuudTab();
                ruudTab.Content = ruudContent;
                tabControl.Items.Add(ruudTab);
                _logger.LogDebug("Added Ruud tab");
                
                // Tab 5: Basic Settings
                _logger.LogDebug("Creating Basic Settings tab");
                var basicTab = new TabItem { Header = "Basic Settings" };
                var basicContent = CreateBasicSettingsTab(section);
                basicTab.Content = basicContent;
                tabControl.Items.Add(basicTab);
                _logger.LogDebug("Added Basic Settings tab");
                
                // Tab 6: Class Restrictions
                _logger.LogDebug("Creating Class Restrictions tab");
                var classTab = new TabItem { Header = "Class Restrictions" };
                var classContent = CreateClassRestrictionsTab(section);
                classTab.Content = classContent;
                tabControl.Items.Add(classTab);
                _logger.LogDebug("Added Class Restrictions tab");
                
                // Tab 7: Player Requirements
                _logger.LogDebug("Creating Player Requirements tab");
                var playerTab = new TabItem { Header = "Player Requirements" };
                var playerContent = CreatePlayerRequirementsTab(section);
                playerTab.Content = playerContent;
                tabControl.Items.Add(playerTab);
                _logger.LogDebug("Added Player Requirements tab");
                
                // Tab 8: Drop Configuration
                _logger.LogDebug("Creating Drop Configuration tab");
                var dropTab = new TabItem { Header = "Drop Configuration" };
                // Get the first DropAllow from the section, or create a default one if none exists
                var dropAllow = section.DropAllows?.FirstOrDefault() ?? new DropAllow
                {
                    DW = 1, DK = 1, ELF = 1, MG = 1, DL = 1,
                    SU = 1, RF = 1, GL = 1, RW = 1, SLA = 1,
                    GC = 1, LW = 1, LM = 1, IK = 1, AC = 1,
                    PlayerMinLevel = 1,
                    PlayerMaxLevel = "MAX",
                    PlayerMinReset = 0,
                    PlayerMaxReset = "MAX",
                    MapNumber = -1,
                    Drops = new ObservableCollection<Drop>
                    {
                        new Drop
                        {
                            Rate = 10000,
                            Type = 0,
                            Count = 1,
                            Items = new ObservableCollection<DropItem>()
                        }
                    }
                };
                var dropContent = CreateDropConfigurationTab(dropAllow);
                dropTab.Content = dropContent;
                tabControl.Items.Add(dropTab);
                _logger.LogDebug("Added Drop Configuration tab");
                
                _logger.LogDebug("Successfully created all tabs for section: {SectionName}", section.DisplayName ?? "Unknown");
                return tabControl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating drop section content for: {SectionName}", section.DisplayName ?? "Unknown");
                throw;
            }
        }

        private UIElement CreateBasicSettingsTab(DropSection section)
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                // Section Configuration Group
                var sectionExpander = new Expander
                {
                    Header = "Section Configuration",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var sectionGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var sectionGrid = new Grid();
                sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Row 1
                var useModeLabel = new Label { Content = "Use Mode:", Style = FindResource("ModernLabel") as Style };
                var useModeCombo = new ComboBox
                {
                    Style = FindResource("ModernComboBox") as Style,
                    ItemContainerStyle = FindResource("ModernComboBoxItem") as Style,
                    Width = 150
                };
                
                // Add UseMode options according to specification
                useModeCombo.Items.Add(new ComboBoxItem { Content = "Default (-1) - Party and non-party drop", Tag = "-1" });
                useModeCombo.Items.Add(new ComboBoxItem { Content = "Not in party (0) - Used while not in party", Tag = "0" });
                useModeCombo.Items.Add(new ComboBoxItem { Content = "Party only (1) - Used for party only", Tag = "1" });
                
                // Set selected value based on section
                var currentUseMode = section.UseMode.ToString();
                for (int i = 0; i < useModeCombo.Items.Count; i++)
                {
                    var item = useModeCombo.Items[i] as ComboBoxItem;
                    if (item?.Tag?.ToString() == currentUseMode)
                    {
                        useModeCombo.SelectedIndex = i;
                        break;
                    }
                }
                
                useModeCombo.SelectionChanged += (s, e) => UpdateSectionUseMode(section, useModeCombo);
                
                var displayNameLabel = new Label { Content = "Display Name:", Style = FindResource("ModernLabel") as Style };
                var displayNameBox = new TextBox
                {
                    Text = section.DisplayName ?? "Section 1",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 150
                };
                displayNameBox.TextChanged += (s, e) => UpdateSectionDisplayName(section, displayNameBox.Text);
                
                // Set positions
                Grid.SetColumn(useModeLabel, 0);
                Grid.SetColumn(useModeCombo, 1);
                Grid.SetColumn(displayNameLabel, 2);
                Grid.SetColumn(displayNameBox, 3);
                Grid.SetRow(useModeLabel, 0);
                Grid.SetRow(useModeCombo, 0);
                Grid.SetRow(displayNameLabel, 0);
                Grid.SetRow(displayNameBox, 0);
                
                sectionGrid.Children.Add(useModeLabel);
                sectionGrid.Children.Add(useModeCombo);
                sectionGrid.Children.Add(displayNameLabel);
                sectionGrid.Children.Add(displayNameBox);
                
                sectionGroup.Content = sectionGrid;
                sectionExpander.Content = sectionGroup;
                stackPanel.Children.Add(sectionExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating basic settings tab");
                return new TextBlock { Text = "Error creating basic settings", Style = FindResource("ModernTextBlock") as Style };
            }
        }

        private UIElement CreateClassRestrictionsTab(DropSection section)
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    stackPanel.Children.Add(new TextBlock { Text = "No drop allows configured", Style = FindResource("ModernTextBlock") as Style });
                    return stackPanel;
                }
                
                var allow = section.DropAllows[0]; // Use first drop allow for now
                
                // Quick Presets section removed as requested
                
                // Individual Class Controls
                var classExpander = new Expander
                {
                    Header = "Class Restrictions",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var classGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style
                };
                
                var classGrid = new Grid();
                classGrid.Margin = new Thickness(5);
                
                // Add enough columns for 5 checkboxes per row with proper spacing
                for (int i = 0; i < 5; i++)
                {
                    classGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
                }
                
                // Add rows for the 3 rows of checkboxes
                for (int i = 0; i < 3; i++)
                {
                    classGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
                
                int row = 0;
                AddClassCheckBox(classGrid, 0, row, "DW", allow.DW == 1);
                AddClassCheckBox(classGrid, 1, row, "DK", allow.DK == 1);
                AddClassCheckBox(classGrid, 2, row, "ELF", allow.ELF == 1);
                AddClassCheckBox(classGrid, 3, row, "MG", allow.MG == 1);
                AddClassCheckBox(classGrid, 4, row, "DL", allow.DL == 1);
                
                row++;
                AddClassCheckBox(classGrid, 0, row, "SUM", allow.SU == 1);
                AddClassCheckBox(classGrid, 1, row, "RF", allow.RF == 1);
                AddClassCheckBox(classGrid, 2, row, "GL", allow.GL == 1);
                AddClassCheckBox(classGrid, 3, row, "RW", allow.RW == 1);
                AddClassCheckBox(classGrid, 4, row, "SLA", allow.SLA == 1);
                
                row++;
                AddClassCheckBox(classGrid, 0, row, "GC", allow.GC == 1);
                AddClassCheckBox(classGrid, 1, row, "LW", allow.LW == 1);
                AddClassCheckBox(classGrid, 2, row, "LM", allow.LM == 1);
                AddClassCheckBox(classGrid, 3, row, "IK", allow.IK == 1);
                AddClassCheckBox(classGrid, 4, row, "ALC", allow.AC == 1);
                
                classGroup.Content = classGrid;
                classExpander.Content = classGroup;
                stackPanel.Children.Add(classExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating class restrictions tab");
                return new TextBlock { Text = "Error creating class restrictions", Style = FindResource("ModernTextBlock") as Style };
            }
        }

        private UIElement CreatePlayerRequirementsTab(DropSection section)
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    stackPanel.Children.Add(new TextBlock { Text = "No drop allows configured", Style = FindResource("ModernTextBlock") as Style });
                    return stackPanel;
                }
                
                var allow = section.DropAllows[0]; // Use first drop allow for now
                
                // Level Requirements
                var levelExpander = new Expander
                {
                    Header = "Level Requirements",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var levelGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var levelGrid = new Grid();
                levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                levelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                var minLevelLabel = new Label { Content = "Min Level:", Style = FindResource("ModernLabel") as Style };
                var minLevelBox = new TextBox
                {
                    Text = allow.PlayerMinLevel.ToString(),
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                minLevelBox.TextChanged += (s, e) => UpdatePlayerRequirement("MinLevel", minLevelBox.Text);
                
                var maxLevelLabel = new Label { Content = "Max Level:", Style = FindResource("ModernLabel") as Style };
                var maxLevelBox = new TextBox
                {
                    Text = allow.PlayerMaxLevel ?? "MAX",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                maxLevelBox.TextChanged += (s, e) => UpdatePlayerRequirement("MaxLevel", maxLevelBox.Text);
                
                Grid.SetColumn(minLevelLabel, 0);
                Grid.SetColumn(minLevelBox, 1);
                Grid.SetColumn(maxLevelLabel, 2);
                Grid.SetColumn(maxLevelBox, 3);
                
                levelGrid.Children.Add(minLevelLabel);
                levelGrid.Children.Add(minLevelBox);
                levelGrid.Children.Add(maxLevelLabel);
                levelGrid.Children.Add(maxLevelBox);
                
                levelGroup.Content = levelGrid;
                levelExpander.Content = levelGroup;
                stackPanel.Children.Add(levelExpander);
                
                // Reset Requirements
                var resetExpander = new Expander
                {
                    Header = "Reset Requirements",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var resetGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var resetGrid = new Grid();
                resetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                resetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                resetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                resetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                var minResetLabel = new Label { Content = "Min Reset:", Style = FindResource("ModernLabel") as Style };
                var minResetBox = new TextBox
                {
                    Text = allow.PlayerMinReset.ToString(),
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                minResetBox.TextChanged += (s, e) => UpdatePlayerRequirement("MinReset", minResetBox.Text);
                
                var maxResetLabel = new Label { Content = "Max Reset:", Style = FindResource("ModernLabel") as Style };
                var maxResetBox = new TextBox
                {
                    Text = allow.PlayerMaxReset ?? "MAX",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                maxResetBox.TextChanged += (s, e) => UpdatePlayerRequirement("MaxReset", maxResetBox.Text);
                
                Grid.SetColumn(minResetLabel, 0);
                Grid.SetColumn(minResetBox, 1);
                Grid.SetColumn(maxResetLabel, 2);
                Grid.SetColumn(maxResetBox, 3);
                
                resetGrid.Children.Add(minResetLabel);
                resetGrid.Children.Add(minResetBox);
                resetGrid.Children.Add(maxResetLabel);
                resetGrid.Children.Add(maxResetBox);
                
                resetGroup.Content = resetGrid;
                resetExpander.Content = resetGroup;
                stackPanel.Children.Add(resetExpander);
                
                // Map Restrictions
                var mapExpander = new Expander
                {
                    Header = "Map Restrictions",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var mapGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style
                };
                
                var mapGrid = new Grid();
                mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                mapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                
                var mapLabel = new Label { Content = "Map Number:", Style = FindResource("ModernLabel") as Style };
                var mapBox = new TextBox
                {
                    Text = allow.MapNumber.ToString(),
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                mapBox.TextChanged += (s, e) => UpdatePlayerRequirement("MapNumber", mapBox.Text);
                
                Grid.SetColumn(mapLabel, 0);
                Grid.SetColumn(mapBox, 1);
                
                mapGrid.Children.Add(mapLabel);
                mapGrid.Children.Add(mapBox);
                
                mapGroup.Content = mapGrid;
                mapExpander.Content = mapGroup;
                stackPanel.Children.Add(mapExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating player requirements tab");
                return new TextBlock { Text = "Error creating player requirements", Style = FindResource("ModernTextBlock") as Style };
            }
        }


                

                

                

                

                

                

                

                



        private UIElement CreateBagConfigurationTab()
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                var bagConfigExpander = new Expander
                {
                    Header = "Bag Configuration",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var bagConfigGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var bagConfigGrid = new Grid();
                bagConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                bagConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                bagConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                bagConfigGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                bagConfigGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Row 1
                var bagNameLabel = new Label { Content = "Name:", Style = FindResource("ModernLabel") as Style };
                var bagNameBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.Name ?? "",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 200,
                    ToolTip = "Name of an Item or Monster. This is the display name for the ItemBag configuration."
                };
                bagNameBox.TextChanged += (s, e) => UpdateBagConfig("Name", bagNameBox.Text);
                
                var itemRateLabel = new Label { Content = "Item Rate:", Style = FindResource("ModernLabel") as Style };
                var itemRateBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.ItemRate.ToString() ?? "10000",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Chance to get an item from the bag, n/10000. If value is lower than 10000 then Zen drop is possible. Example: 8000 = 80% chance."
                };
                itemRateBox.TextChanged += (s, e) => UpdateBagConfig("ItemRate", itemRateBox.Text);
                
                // Row 2
                var setItemRateLabel = new Label { Content = "Set Item Rate:", Style = FindResource("ModernLabel") as Style };
                var setItemRateBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.SetItemRate.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Drop rate of random set item selected from entire pool of available ancients, n/10000. Rate is drawn individually for every drop attempt. Can be 0."
                };
                setItemRateBox.TextChanged += (s, e) => UpdateBagConfig("SetItemRate", setItemRateBox.Text);
                
                var moneyDropLabel = new Label { Content = "Money Drop:", Style = FindResource("ModernLabel") as Style };
                var moneyDropBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.MoneyDrop.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Money (Zen) amount to drop from bag. Must be greater than 0 if ItemRate is lower than 10000 and Ruud GainRate is 0."
                };
                moneyDropBox.TextChanged += (s, e) => UpdateBagConfig("MoneyDrop", moneyDropBox.Text);
                
                // Row 3
                var partyDropRateLabel = new Label { Content = "Party Drop Rate:", Style = FindResource("ModernLabel") as Style };
                var partyDropRateBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.PartyDropRate.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Chance to apply party drop, n/10000. If chance rate draw succeeds, bag will be executed individually for every party member within 9 tiles from party master."
                };
                partyDropRateBox.TextChanged += (s, e) => UpdateBagConfig("PartyDropRate", partyDropRateBox.Text);
                
                var bagUseRateLabel = new Label { Content = "Bag Use Rate:", Style = FindResource("ModernLabel") as Style };
                var bagUseRateBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.BagUseRate.ToString() ?? "10000",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Defines probability to use Bag, n/10000. If value is lower than 10000 (100%) then a draw is performed between item bag and another drop system in hierarchy."
                };
                bagUseRateBox.TextChanged += (s, e) => UpdateBagConfig("BagUseRate", bagUseRateBox.Text);
                
                // Row 4
                var setItemCountLabel = new Label { Content = "Set Item Count:", Style = FindResource("ModernLabel") as Style };
                var setItemCountBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.SetItemCount.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Number of set items to drop if SetItemRate is set to value greater than 0."
                };
                setItemCountBox.TextChanged += (s, e) => UpdateBagConfig("SetItemCount", setItemCountBox.Text);
                
                var masterySetItemIncludeLabel = new Label { Content = "Mastery Set Include:", Style = FindResource("ModernLabel") as Style };
                var masterySetItemIncludeBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.MasterySetItemInclude.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "If set to 1, the set item drop based on SetItemRate will include mastery set items, otherwise set to 0."
                };
                masterySetItemIncludeBox.TextChanged += (s, e) => UpdateBagConfig("MasterySetItemInclude", masterySetItemIncludeBox.Text);
                
                // Row 5
                var isPentagramForBeginnersDropLabel = new Label { Content = "Pentagram Beginners:", Style = FindResource("ModernLabel") as Style };
                var isPentagramForBeginnersDropBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.IsPentagramForBeginnersDrop.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Defines whether to drop Pentagram for Beginners with pre-defined Errtels for Beginners. 0 = No, 1 = Yes"
                };
                isPentagramForBeginnersDropBox.TextChanged += (s, e) => UpdateBagConfig("IsPentagramForBeginnersDrop", isPentagramForBeginnersDropBox.Text);
                
                var bagUseEffectLabel = new Label { Content = "Bag Use Effect:", Style = FindResource("ModernLabel") as Style };
                var bagUseEffectBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.BagUseEffect.ToString() ?? "-1",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Bag use effect: -1 = no effect, 0 = Firecracker effect type"
                };
                bagUseEffectBox.TextChanged += (s, e) => UpdateBagConfig("BagUseEffect", bagUseEffectBox.Text);
                
                // Row 6
                var partyOneDropOnlyLabel = new Label { Content = "Party One Drop Only:", Style = FindResource("ModernLabel") as Style };
                var partyOneDropOnlyBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.PartyOneDropOnly.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "If set to 1, only one party member will receive the drop. 0 = All party members can receive drops."
                };
                partyOneDropOnlyBox.TextChanged += (s, e) => UpdateBagConfig("PartyOneDropOnly", partyOneDropOnlyBox.Text);
                
                var partyShareTypeLabel = new Label { Content = "Party Share Type:", Style = FindResource("ModernLabel") as Style };
                var partyShareTypeBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.PartyShareType.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Party sharing type: 0 = Normal, other values may have special sharing behavior."
                };
                partyShareTypeBox.TextChanged += (s, e) => UpdateBagConfig("PartyShareType", partyShareTypeBox.Text);
                
                var bagUseTypeLabel = new Label { Content = "Bag Use Type:", Style = FindResource("ModernLabel") as Style };
                var bagUseTypeBox = new TextBox
                {
                    Text = _currentItemBag?.Config?.BagUseType.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100,
                    ToolTip = "Bag use type: 0 = Normal, 1 = Moss Merchant, 2 = Event/GC type."
                };
                bagUseTypeBox.TextChanged += (s, e) => UpdateBagConfig("BagUseType", bagUseTypeBox.Text);
                
                // Set positions
                Grid.SetColumn(bagNameLabel, 0);
                Grid.SetColumn(bagNameBox, 1);
                Grid.SetColumn(itemRateLabel, 2);
                Grid.SetColumn(itemRateBox, 3);
                Grid.SetRow(bagNameLabel, 0);
                Grid.SetRow(bagNameBox, 0);
                Grid.SetRow(itemRateLabel, 0);
                Grid.SetRow(itemRateBox, 0);
                
                Grid.SetColumn(setItemRateLabel, 0);
                Grid.SetColumn(setItemRateBox, 1);
                Grid.SetColumn(moneyDropLabel, 2);
                Grid.SetColumn(moneyDropBox, 3);
                Grid.SetRow(setItemRateLabel, 1);
                Grid.SetRow(setItemRateBox, 1);
                Grid.SetRow(moneyDropLabel, 1);
                Grid.SetRow(moneyDropBox, 1);
                
                Grid.SetColumn(partyDropRateLabel, 0);
                Grid.SetColumn(partyDropRateBox, 1);
                Grid.SetColumn(bagUseRateLabel, 2);
                Grid.SetColumn(bagUseRateBox, 3);
                Grid.SetRow(partyDropRateLabel, 2);
                Grid.SetRow(partyDropRateBox, 2);
                Grid.SetRow(bagUseRateLabel, 2);
                Grid.SetRow(bagUseRateBox, 2);
                
                Grid.SetColumn(setItemCountLabel, 0);
                Grid.SetColumn(setItemCountBox, 1);
                Grid.SetColumn(masterySetItemIncludeLabel, 2);
                Grid.SetColumn(masterySetItemIncludeBox, 3);
                Grid.SetRow(setItemCountLabel, 3);
                Grid.SetRow(setItemCountBox, 3);
                Grid.SetRow(masterySetItemIncludeLabel, 3);
                Grid.SetRow(masterySetItemIncludeBox, 3);
                
                Grid.SetColumn(isPentagramForBeginnersDropLabel, 0);
                Grid.SetColumn(isPentagramForBeginnersDropBox, 1);
                Grid.SetColumn(bagUseEffectLabel, 2);
                Grid.SetColumn(bagUseEffectBox, 3);
                Grid.SetRow(isPentagramForBeginnersDropLabel, 4);
                Grid.SetRow(isPentagramForBeginnersDropBox, 4);
                Grid.SetRow(bagUseEffectLabel, 4);
                Grid.SetRow(bagUseEffectBox, 4);
                
                // Row 6 - Position the remaining fields properly
                Grid.SetColumn(partyOneDropOnlyLabel, 0);
                Grid.SetColumn(partyOneDropOnlyBox, 1);
                Grid.SetColumn(partyShareTypeLabel, 2);
                Grid.SetColumn(partyShareTypeBox, 3);
                Grid.SetRow(partyOneDropOnlyLabel, 5);
                Grid.SetRow(partyOneDropOnlyBox, 5);
                Grid.SetRow(partyShareTypeLabel, 5);
                Grid.SetRow(partyShareTypeBox, 5);
                
                // Row 7 - Add BagUseType in a new row to avoid overlay
                Grid.SetColumn(bagUseTypeLabel, 0);
                Grid.SetColumn(bagUseTypeBox, 1);
                Grid.SetRow(bagUseTypeLabel, 6);
                Grid.SetRow(bagUseTypeBox, 6);
                
                // Add all elements
                bagConfigGrid.Children.Add(bagNameLabel);
                bagConfigGrid.Children.Add(bagNameBox);
                bagConfigGrid.Children.Add(itemRateLabel);
                bagConfigGrid.Children.Add(itemRateBox);
                bagConfigGrid.Children.Add(setItemRateLabel);
                bagConfigGrid.Children.Add(setItemRateBox);
                bagConfigGrid.Children.Add(moneyDropLabel);
                bagConfigGrid.Children.Add(moneyDropBox);
                bagConfigGrid.Children.Add(partyDropRateLabel);
                bagConfigGrid.Children.Add(partyDropRateBox);
                bagConfigGrid.Children.Add(bagUseRateLabel);
                bagConfigGrid.Children.Add(bagUseRateBox);
                bagConfigGrid.Children.Add(setItemCountLabel);
                bagConfigGrid.Children.Add(setItemCountBox);
                bagConfigGrid.Children.Add(masterySetItemIncludeLabel);
                bagConfigGrid.Children.Add(masterySetItemIncludeBox);
                bagConfigGrid.Children.Add(isPentagramForBeginnersDropLabel);
                bagConfigGrid.Children.Add(isPentagramForBeginnersDropBox);
                bagConfigGrid.Children.Add(bagUseEffectLabel);
                bagConfigGrid.Children.Add(bagUseEffectBox);
                bagConfigGrid.Children.Add(partyOneDropOnlyLabel);
                bagConfigGrid.Children.Add(partyOneDropOnlyBox);
                bagConfigGrid.Children.Add(partyShareTypeLabel);
                bagConfigGrid.Children.Add(partyShareTypeBox);
                bagConfigGrid.Children.Add(bagUseTypeLabel);
                bagConfigGrid.Children.Add(bagUseTypeBox);
                
                bagConfigGroup.Content = bagConfigGrid;
                bagConfigExpander.Content = bagConfigGroup;
                stackPanel.Children.Add(bagConfigExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Bag Configuration tab");
                return new TextBlock { Text = "Error creating Bag Configuration", Style = FindResource("ModernTextBlock") as Style };
            }
        }

        private UIElement CreateSummonBookTab()
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                var summonBookExpander = new Expander
                {
                    Header = "Summon Book Configuration",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var summonBookGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var summonBookGrid = new Grid();
                summonBookGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                summonBookGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                summonBookGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                summonBookGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                summonBookGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                summonBookGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Row 1
                var enableLabel = new Label { Content = "Enable:", Style = FindResource("ModernLabel") as Style };
                var enableBox = new TextBox
                {
                    Text = _currentItemBag?.SummonBook?.Enable.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                enableBox.TextChanged += (s, e) => UpdateSummonBook("Enable", enableBox.Text);
                
                var dropRateLabel = new Label { Content = "Drop Rate:", Style = FindResource("ModernLabel") as Style };
                var dropRateBox = new TextBox
                {
                    Text = _currentItemBag?.SummonBook?.DropRate.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                dropRateBox.TextChanged += (s, e) => UpdateSummonBook("DropRate", dropRateBox.Text);
                
                // Row 2
                var itemCatLabel = new Label { Content = "Item Cat:", Style = FindResource("ModernLabel") as Style };
                var itemCatBox = new TextBox
                {
                    Text = _currentItemBag?.SummonBook?.ItemCat.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                itemCatBox.TextChanged += (s, e) => UpdateSummonBook("ItemCat", itemCatBox.Text);
                
                var itemIndexLabel = new Label { Content = "Item Index:", Style = FindResource("ModernLabel") as Style };
                var itemIndexBox = new TextBox
                {
                    Text = _currentItemBag?.SummonBook?.ItemIndex.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                itemIndexBox.TextChanged += (s, e) => UpdateSummonBook("ItemIndex", itemIndexBox.Text);
                
                // Set positions
                Grid.SetColumn(enableLabel, 0);
                Grid.SetColumn(enableBox, 1);
                Grid.SetColumn(dropRateLabel, 2);
                Grid.SetColumn(dropRateBox, 3);
                Grid.SetRow(enableLabel, 0);
                Grid.SetRow(enableBox, 0);
                Grid.SetRow(dropRateLabel, 0);
                Grid.SetRow(dropRateBox, 0);
                
                Grid.SetColumn(itemCatLabel, 0);
                Grid.SetColumn(itemCatBox, 1);
                Grid.SetColumn(itemIndexLabel, 2);
                Grid.SetColumn(itemIndexBox, 3);
                Grid.SetRow(itemCatLabel, 1);
                Grid.SetRow(itemCatBox, 1);
                Grid.SetRow(itemIndexLabel, 1);
                Grid.SetRow(itemIndexBox, 1);
                
                // Add all elements
                summonBookGrid.Children.Add(enableLabel);
                summonBookGrid.Children.Add(enableBox);
                summonBookGrid.Children.Add(dropRateLabel);
                summonBookGrid.Children.Add(dropRateBox);
                summonBookGrid.Children.Add(itemCatLabel);
                summonBookGrid.Children.Add(itemCatBox);
                summonBookGrid.Children.Add(itemIndexLabel);
                summonBookGrid.Children.Add(itemIndexBox);
                
                summonBookGroup.Content = summonBookGrid;
                summonBookExpander.Content = summonBookGroup;
                stackPanel.Children.Add(summonBookExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Summon Book tab");
                return new TextBlock { Text = "Error creating Summon Book", Style = FindResource("ModernTextBlock") as Style };
            }
        }

        private UIElement CreateAddCoinTab()
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                var addCoinExpander = new Expander
                {
                    Header = "Add Coin Configuration",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var addCoinGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var addCoinGrid = new Grid();
                addCoinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                addCoinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                addCoinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                addCoinGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                addCoinGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                addCoinGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Row 1
                var enableLabel = new Label { Content = "Enable:", Style = FindResource("ModernLabel") as Style };
                var enableBox = new TextBox
                {
                    Text = _currentItemBag?.AddCoin?.Enable.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                enableBox.TextChanged += (s, e) => UpdateAddCoin("Enable", enableBox.Text);
                
                var coinTypeLabel = new Label { Content = "Coin Type:", Style = FindResource("ModernLabel") as Style };
                var coinTypeBox = new TextBox
                {
                    Text = _currentItemBag?.AddCoin?.CoinType.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                coinTypeBox.TextChanged += (s, e) => UpdateAddCoin("CoinType", coinTypeBox.Text);
                
                // Row 2
                var coinValueLabel = new Label { Content = "Coin Value:", Style = FindResource("ModernLabel") as Style };
                var coinValueBox = new TextBox
                {
                    Text = _currentItemBag?.AddCoin?.CoinValue.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                coinValueBox.TextChanged += (s, e) => UpdateAddCoin("CoinValue", coinValueBox.Text);
                
                // Set positions
                Grid.SetColumn(enableLabel, 0);
                Grid.SetColumn(enableBox, 1);
                Grid.SetColumn(coinTypeLabel, 2);
                Grid.SetColumn(coinTypeBox, 3);
                Grid.SetRow(enableLabel, 0);
                Grid.SetRow(enableBox, 0);
                Grid.SetRow(coinTypeLabel, 0);
                Grid.SetRow(coinTypeBox, 0);
                
                Grid.SetColumn(coinValueLabel, 0);
                Grid.SetColumn(coinValueBox, 1);
                Grid.SetRow(coinValueLabel, 1);
                Grid.SetRow(coinValueBox, 1);
                
                // Add all elements
                addCoinGrid.Children.Add(enableLabel);
                addCoinGrid.Children.Add(enableBox);
                addCoinGrid.Children.Add(coinTypeLabel);
                addCoinGrid.Children.Add(coinTypeBox);
                addCoinGrid.Children.Add(coinValueLabel);
                addCoinGrid.Children.Add(coinValueBox);
                
                addCoinGroup.Content = addCoinGrid;
                addCoinExpander.Content = addCoinGroup;
                stackPanel.Children.Add(addCoinExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Add Coin tab");
                return new TextBlock { Text = "Error creating Add Coin", Style = FindResource("ModernTextBlock") as Style };
            }
        }

        private UIElement CreateRuudTab()
        {
            try
            {
                var stackPanel = new StackPanel { Margin = new Thickness(10) };
                
                var ruudExpander = new Expander
                {
                    Header = "Ruud Configuration",
                    IsExpanded = true,
                    Style = FindResource("ModernExpander") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var ruudGroup = new GroupBox
                {
                    Header = "Configuration Settings",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                
                var ruudGrid = new Grid();
                ruudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                ruudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                ruudGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                // Row 1
                var gainRateLabel = new Label { Content = "Gain Rate:", Style = FindResource("ModernLabel") as Style };
                var gainRateBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.GainRate.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                gainRateBox.TextChanged += (s, e) => UpdateRuud("GainRate", gainRateBox.Text);
                
                var minValueLabel = new Label { Content = "Min Value:", Style = FindResource("ModernLabel") as Style };
                var minValueBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.MinValue.ToString() ?? "1",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                minValueBox.TextChanged += (s, e) => UpdateRuud("MinValue", minValueBox.Text);
                
                // Row 2
                var maxValueLabel = new Label { Content = "Max Value:", Style = FindResource("ModernLabel") as Style };
                var maxValueBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.MaxValue.ToString() ?? "10",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                maxValueBox.TextChanged += (s, e) => UpdateRuud("MaxValue", maxValueBox.Text);
                
                var playerMinLevelLabel = new Label { Content = "Player Min Level:", Style = FindResource("ModernLabel") as Style };
                var playerMinLevelBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.PlayerMinLevel.ToString() ?? "1",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                playerMinLevelBox.TextChanged += (s, e) => UpdateRuud("PlayerMinLevel", playerMinLevelBox.Text);
                
                // Row 3
                var playerMaxLevelLabel = new Label { Content = "Player Max Level:", Style = FindResource("ModernLabel") as Style };
                var playerMaxLevelBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.PlayerMaxLevel ?? "MAX",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                playerMaxLevelBox.TextChanged += (s, e) => UpdateRuud("PlayerMaxLevel", playerMaxLevelBox.Text);
                
                var playerMinResetLabel = new Label { Content = "Player Min Reset:", Style = FindResource("ModernLabel") as Style };
                var playerMinResetBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.PlayerMinReset.ToString() ?? "0",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                playerMinResetBox.TextChanged += (s, e) => UpdateRuud("PlayerMinReset", playerMinResetBox.Text);
                
                // Row 4
                var playerMaxResetLabel = new Label { Content = "Player Max Reset:", Style = FindResource("ModernLabel") as Style };
                var playerMaxResetBox = new TextBox
                {
                    Text = _currentItemBag?.Ruud?.PlayerMaxReset ?? "MAX",
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 100
                };
                playerMaxResetBox.TextChanged += (s, e) => UpdateRuud("PlayerMaxReset", playerMaxResetBox.Text);
                
                // Set positions - 2 columns per row with proper spacing
                Grid.SetColumn(gainRateLabel, 0);
                Grid.SetColumn(gainRateBox, 1);
                Grid.SetRow(gainRateLabel, 0);
                Grid.SetRow(gainRateBox, 0);
                
                Grid.SetColumn(minValueLabel, 0);
                Grid.SetColumn(minValueBox, 1);
                Grid.SetRow(minValueLabel, 1);
                Grid.SetRow(minValueBox, 1);
                
                Grid.SetColumn(maxValueLabel, 0);
                Grid.SetColumn(maxValueBox, 1);
                Grid.SetRow(maxValueLabel, 2);
                Grid.SetRow(maxValueBox, 2);
                
                Grid.SetColumn(playerMinLevelLabel, 0);
                Grid.SetColumn(playerMinLevelBox, 1);
                Grid.SetRow(playerMinLevelLabel, 3);
                Grid.SetRow(playerMinLevelBox, 3);
                
                Grid.SetColumn(playerMaxLevelLabel, 0);
                Grid.SetColumn(playerMaxLevelBox, 1);
                Grid.SetRow(playerMaxLevelLabel, 4);
                Grid.SetRow(playerMaxLevelBox, 4);
                
                Grid.SetColumn(playerMinResetLabel, 0);
                Grid.SetColumn(playerMinResetBox, 1);
                Grid.SetRow(playerMinResetLabel, 5);
                Grid.SetRow(playerMinResetBox, 5);
                
                Grid.SetColumn(playerMaxResetLabel, 0);
                Grid.SetColumn(playerMaxResetBox, 1);
                Grid.SetRow(playerMaxResetLabel, 6);
                Grid.SetRow(playerMaxResetBox, 6);
                
                // Add all elements
                ruudGrid.Children.Add(gainRateLabel);
                ruudGrid.Children.Add(gainRateBox);
                ruudGrid.Children.Add(minValueLabel);
                ruudGrid.Children.Add(minValueBox);
                ruudGrid.Children.Add(maxValueLabel);
                ruudGrid.Children.Add(maxValueBox);
                ruudGrid.Children.Add(playerMinLevelLabel);
                ruudGrid.Children.Add(playerMinLevelBox);
                ruudGrid.Children.Add(playerMaxLevelLabel);
                ruudGrid.Children.Add(playerMaxLevelBox);
                ruudGrid.Children.Add(playerMinResetLabel);
                ruudGrid.Children.Add(playerMinResetBox);
                ruudGrid.Children.Add(playerMaxResetLabel);
                ruudGrid.Children.Add(playerMaxResetBox);
                
                ruudGroup.Content = ruudGrid;
                ruudExpander.Content = ruudGroup;
                stackPanel.Children.Add(ruudExpander);
                
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Ruud tab");
                return new TextBlock { Text = "Error creating Ruud", Style = FindResource("ModernTextBox") as Style };
            }
        }

        private void AddClassCheckBox(Grid grid, int column, int row, string className, bool isChecked)
        {
            try
            {
                var checkBox = new CheckBox
                {
                    Content = className,
                    IsChecked = isChecked,
                    Margin = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MinWidth = 60,
                    FontWeight = FontWeights.Bold,
                    Foreground = FindResource("TextBrush") as System.Windows.Media.Brush,
                    Background = FindResource("SecondaryBrush") as System.Windows.Media.Brush
                };
                
                checkBox.Checked += (s, e) => UpdateClassRestriction(className, true);
                checkBox.Unchecked += (s, e) => UpdateClassRestriction(className, false);
                
                Grid.SetColumn(checkBox, column);
                Grid.SetRow(checkBox, row);
                grid.Children.Add(checkBox);
                
                _logger.LogDebug("Added class checkbox {ClassName} at position ({Column}, {Row})", className, column, row);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add class checkbox for {ClassName}", className);
            }
        }



        private void UpdateClassRestriction(string className, bool enabled)
        {
            try
            {
                if (_currentItemBag?.DropSections == null || tabDropSections?.SelectedIndex < 0)
                    return;
                
                var section = _currentItemBag.DropSections[tabDropSections.SelectedIndex];
                if (section?.DropAllows == null || section.DropAllows.Count == 0)
                    return;
                
                var dropAllows = section.DropAllows!;
                var allow = dropAllows[0];
                if (allow == null)
                    return;
                
                var value = enabled ? 1 : 0;
                
                switch (className)
                {
                    case "DW": allow!.DW = value; break;
                    case "DK": allow!.DK = value; break;
                    case "ELF": allow!.ELF = value; break;
                    case "MG": allow!.MG = value; break;
                    case "DL": allow!.DL = value; break;
                    case "SU": allow!.SU = value; break;
                    case "RF": allow!.RF = value; break;
                    case "GL": allow!.GL = value; break;
                    case "RW": allow!.RW = value; break;
                    case "SLA": allow!.SLA = value; break;
                    case "GC": allow!.GC = value; break;
                    case "LW": allow!.LW = value; break;
                    case "LM": allow!.LM = value; break;
                    case "IK": allow!.IK = value; break;
                    case "AC": allow!.AC = value; break;
                }
                
                _logger.LogDebug("Updated class restriction {ClassName} to {Enabled}", className, enabled);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update class restriction {ClassName}", className);
            }
        }

        private void UpdatePlayerRequirement(string requirement, string value)
        {
            try
            {
                if (_currentItemBag?.DropSections == null || tabDropSections?.SelectedIndex < 0)
                    return;
                
                var section = _currentItemBag.DropSections[tabDropSections.SelectedIndex];
                if (section?.DropAllows == null || section.DropAllows.Count == 0)
                    return;
                
                var dropAllows = section.DropAllows!;
                var allow = dropAllows[0];
                if (allow == null)
                    return;
                
                switch (requirement)
                {
                    case "MinLevel":
                        if (int.TryParse(value, out int minLevel))
                            allow!.PlayerMinLevel = minLevel;
                        break;
                    case "MaxLevel":
                        allow!.PlayerMaxLevel = value;
                        break;
                    case "MinReset":
                        if (int.TryParse(value, out int minReset))
                            allow!.PlayerMinReset = minReset;
                        break;
                    case "MaxReset":
                        allow!.PlayerMaxReset = value;
                        break;
                    case "MapNumber":
                        if (int.TryParse(value, out int mapNumber))
                            allow!.MapNumber = mapNumber;
                        break;
                }
                
                _logger.LogDebug("Updated player requirement {Requirement} to {Value}", requirement, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update player requirement {Requirement}", requirement);
            }
        }

        private void UpdateDropConfiguration(string setting, string value)
        {
            try
            {
                if (_currentItemBag?.DropSections == null || _currentItemBag.DropSections.Count == 0)
                {
                    _logger.LogWarning("No drop sections available to update");
                    return;
                }

                var section = _currentItemBag.DropSections[0]; // Use first section for now
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    _logger.LogWarning("No drop allows available to update");
                    return;
                }

                var allow = section.DropAllows[0]; // Use first drop allow for now
                if (allow.Drops == null || allow.Drops.Count == 0)
                {
                    _logger.LogWarning("No drops available to update");
                    return;
                }

                var drop = allow.Drops[0]; // Use first drop for now

                switch (setting)
                {
                    case "Rate":
                        if (int.TryParse(value, out int rate))
                            drop.Rate = rate;
                        break;
                    case "Count":
                        if (int.TryParse(value, out int count))
                            drop.Count = count;
                        break;
                    case "Type":
                        if (int.TryParse(value, out int type))
                            drop.Type = type;
                        break;
                }
                
                _logger.LogDebug("Updated drop configuration setting {Setting} to {Value}", setting, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update drop configuration setting {Setting}", setting);
            }
        }

        private void UpdateBagConfig(string setting, string value)
        {
            try
            {
                if (_currentItemBag == null)
                {
                    _logger.LogWarning("No ItemBag loaded to update BagConfig");
                    return;
                }

                switch (setting)
                {
                    case "Name":
                        _currentItemBag.Config.Name = value;
                        break;
                    case "ItemRate":
                        if (int.TryParse(value, out int itemRate))
                            _currentItemBag.Config.ItemRate = itemRate;
                        break;
                    case "SetItemRate":
                        if (int.TryParse(value, out int setItemRate))
                            _currentItemBag.Config.SetItemRate = setItemRate;
                        break;
                    case "MoneyDrop":
                        if (int.TryParse(value, out int moneyDrop))
                            _currentItemBag.Config.MoneyDrop = moneyDrop;
                        break;
                    case "PartyDropRate":
                        if (int.TryParse(value, out int partyDropRate))
                            _currentItemBag.Config.PartyDropRate = partyDropRate;
                        break;
                    case "BagUseRate":
                        if (int.TryParse(value, out int bagUseRate))
                            _currentItemBag.Config.BagUseRate = bagUseRate;
                        break;
                    case "SetItemCount":
                        if (int.TryParse(value, out int setItemCount))
                            _currentItemBag.Config.SetItemCount = setItemCount;
                        break;
                    case "MasterySetItemInclude":
                        if (int.TryParse(value, out int masterySetItemInclude))
                            _currentItemBag.Config.MasterySetItemInclude = masterySetItemInclude;
                        break;
                    case "IsPentagramForBeginnersDrop":
                        if (int.TryParse(value, out int isPentagramForBeginnersDrop))
                            _currentItemBag.Config.IsPentagramForBeginnersDrop = isPentagramForBeginnersDrop;
                        break;
                    case "BagUseEffect":
                        if (int.TryParse(value, out int bagUseEffect))
                            _currentItemBag.Config.BagUseEffect = bagUseEffect;
                        break;
                    case "PartyOneDropOnly":
                        if (int.TryParse(value, out int partyOneDropOnly))
                            _currentItemBag.Config.PartyOneDropOnly = partyOneDropOnly;
                        break;
                    case "PartyShareType":
                        if (int.TryParse(value, out int partyShareType))
                            _currentItemBag.Config.PartyShareType = partyShareType;
                        break;
                    case "BagUseType":
                        if (int.TryParse(value, out int bagUseType))
                            _currentItemBag.Config.BagUseType = bagUseType;
                        break;
                }
                
                _logger.LogDebug("Updated BagConfig setting {Setting} to {Value}", setting, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update BagConfig setting {Setting}", setting);
            }
        }

        private void UpdateSummonBook(string setting, string value)
        {
            try
            {
                if (_currentItemBag?.SummonBook == null)
                {
                    _logger.LogWarning("No SummonBook loaded to update");
                    return;
                }

                switch (setting)
                {
                    case "Enable":
                        if (int.TryParse(value, out int enable))
                            _currentItemBag.SummonBook.Enable = enable;
                        break;
                    case "DropRate":
                        if (int.TryParse(value, out int dropRate))
                            _currentItemBag.SummonBook.DropRate = dropRate;
                        break;
                    case "ItemCat":
                        if (int.TryParse(value, out int itemCat))
                            _currentItemBag.SummonBook.ItemCat = itemCat;
                        break;
                    case "ItemIndex":
                        if (int.TryParse(value, out int itemIndex))
                            _currentItemBag.SummonBook.ItemIndex = itemIndex;
                        break;
                }
                
                _logger.LogDebug("Updated SummonBook setting {Setting} to {Value}", setting, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update SummonBook setting {Setting}", setting);
            }
        }

        private void UpdateAddCoin(string setting, string value)
        {
            try
            {
                if (_currentItemBag?.AddCoin == null)
                {
                    _logger.LogWarning("No AddCoin loaded to update");
                    return;
                }

                switch (setting)
                {
                    case "Enable":
                        if (int.TryParse(value, out int enable))
                            _currentItemBag.AddCoin.Enable = enable;
                        break;
                    case "CoinType":
                        if (int.TryParse(value, out int coinType))
                            _currentItemBag.AddCoin.CoinType = coinType;
                        break;
                    case "CoinValue":
                        if (int.TryParse(value, out int coinValue))
                            _currentItemBag.AddCoin.CoinValue = coinValue;
                        break;
                }
                
                _logger.LogDebug("Updated AddCoin setting {Setting} to {Value}", setting, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update AddCoin setting {Setting}", setting);
            }
        }

        private void UpdateRuud(string setting, string value)
        {
            try
            {
                if (_currentItemBag?.Ruud == null)
                {
                    _logger.LogWarning("No Ruud loaded to update");
                    return;
                }

                switch (setting)
                {
                    case "GainRate":
                        if (int.TryParse(value, out int gainRate))
                            _currentItemBag.Ruud.GainRate = gainRate;
                        break;
                    case "MinValue":
                        if (int.TryParse(value, out int minValue))
                            _currentItemBag.Ruud.MinValue = minValue;
                        break;
                    case "MaxValue":
                        if (int.TryParse(value, out int maxValue))
                            _currentItemBag.Ruud.MaxValue = maxValue;
                        break;
                }
                
                _logger.LogDebug("Updated Ruud setting {Setting} to {Value}", setting, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update Ruud setting {Setting}", setting);
            }
        }

        private void UpdateSectionUseMode(DropSection section, ComboBox comboBox)
        {
            try
            {
                if (section == null || comboBox.SelectedItem == null)
                {
                    _logger.LogWarning("Section or comboBox selection is null");
                    return;
                }

                var selectedItem = comboBox.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag != null && int.TryParse(selectedItem.Tag.ToString(), out int useMode))
                {
                    section.UseMode = useMode;
                    _logger.LogDebug("Updated section UseMode to {UseMode}", useMode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update section UseMode");
            }
        }

        private void UpdateSectionDisplayName(DropSection section, string displayName)
        {
            try
            {
                if (section == null)
                {
                    _logger.LogWarning("Section is null");
                    return;
                }

                section.DisplayName = displayName;
                _logger.LogDebug("Updated section DisplayName to {DisplayName}", displayName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update section DisplayName");
            }
        }

        private void LoadAdditionalBagConfig()
        {
            try
            {
                _logger.LogDebug("Loading additional BagConfig properties");
                // This method will be implemented to load additional configuration properties
                // like SetItemCount, MasterySetItemInclude, IsPentagramForBeginnersDrop, etc.
                // For now, it's a placeholder for future implementation
                _logger.LogDebug("Additional BagConfig properties loaded");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load additional BagConfig properties");
            }
        }

        private void LoadDropSectionsToUI()
        {
            try
            {
                _logger.LogDebug("LoadDropSectionsToUI: Starting to load drop sections to UI");
                
                if (_currentItemBag == null)
                {
                    _logger.LogWarning("No ItemBag to load drop sections from");
                    return;
                }

                if (tabDropSections == null)
                {
                    _logger.LogWarning("tabDropSections is null, cannot load drop sections");
                    return;
                }

                // Store current tab selection to preserve it
                int currentTabIndex = tabDropSections.SelectedIndex;
                string currentTabHeader = "";
                if (currentTabIndex >= 0 && currentTabIndex < tabDropSections.Items.Count)
                {
                    var currentTab = tabDropSections.Items[currentTabIndex] as TabItem;
                    currentTabHeader = currentTab?.Header?.ToString() ?? "";
                    _logger.LogDebug("LoadDropSectionsToUI: Storing current tab selection - Index: {TabIndex}, Header: {Header}", 
                        currentTabIndex, currentTabHeader);
                }
                else
                {
                    _logger.LogDebug("LoadDropSectionsToUI: No current tab selection to preserve");
                }

                tabDropSections.Items.Clear();
                _logger.LogDebug("LoadDropSectionsToUI: Cleared existing drop section tabs");
                
                if (_currentItemBag.DropSections != null)
                {
                    foreach (var section in _currentItemBag.DropSections)
                    {
                        if (section != null)
                        {
                            try
                            {
                                _logger.LogDebug("Creating tab for section: {SectionName}", section.DisplayName ?? "Unknown");
                                var tabContent = CreateDropSectionContent(section);
                                if (tabContent != null)
                                {
                                    var tabItem = new TabItem
                                    {
                                        Header = section.DisplayName ?? "Unknown Section",
                                        Content = tabContent
                                    };
                                    
                                    tabDropSections.Items.Add(tabItem);
                                    _logger.LogDebug("Successfully added drop section tab: {SectionName}", section.DisplayName ?? "Unknown");
                                }
                                else
                                {
                                    _logger.LogWarning("CreateDropSectionContent returned null for section: {SectionName}", section.DisplayName ?? "Unknown");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to create tab for drop section: {SectionName}", section.DisplayName ?? "Unknown");
                                // Create a fallback tab with error message
                                var errorTab = new TabItem
                                {
                                    Header = section.DisplayName ?? "Error",
                                    Content = new TextBlock { Text = $"Error creating content: {ex.Message}", Style = FindResource("ModernTextBlock") as Style }
                                };
                                tabDropSections.Items.Add(errorTab);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Null drop section encountered, skipping");
                        }
                    }
                    
                    // Try to restore the previous tab selection
                    if (tabDropSections.Items.Count > 0)
                    {
                        _logger.LogDebug("LoadDropSectionsToUI: Attempting to restore tab selection. Previous index: {PreviousIndex}, Previous header: {PreviousHeader}", 
                            currentTabIndex, currentTabHeader);
                        
                        if (currentTabIndex >= 0 && currentTabIndex < tabDropSections.Items.Count)
                        {
                            // Try to select the same index if it's still valid
                            tabDropSections.SelectedIndex = currentTabIndex;
                            _logger.LogDebug("LoadDropSectionsToUI: Restored previous tab selection to index: {TabIndex}", currentTabIndex);
                        }
                        else if (!string.IsNullOrEmpty(currentTabHeader))
                        {
                            // Try to find tab with the same header
                            for (int i = 0; i < tabDropSections.Items.Count; i++)
                            {
                                var tab = tabDropSections.Items[i] as TabItem;
                                if (tab?.Header?.ToString() == currentTabHeader)
                                {
                                    tabDropSections.SelectedIndex = i;
                                    _logger.LogDebug("LoadDropSectionsToUI: Restored tab selection by header: {Header} at index: {TabIndex}", currentTabHeader, i);
                                    break;
                                }
                            }
                        }
                        
                        // If no previous selection was restored, select the first tab
                        if (tabDropSections.SelectedIndex < 0)
                        {
                            tabDropSections.SelectedIndex = 0;
                            _logger.LogDebug("LoadDropSectionsToUI: No previous selection to restore, selected first tab (index 0)");
                        }
                        
                        _logger.LogDebug("LoadDropSectionsToUI: Final tab selection: {FinalIndex}", tabDropSections.SelectedIndex);
                    }
                    
                    _logger.LogInformation("Drop sections loaded to UI successfully. Total sections: {SectionCount}", _currentItemBag.DropSections.Count);
                }
                else
                {
                    _logger.LogWarning("ItemBag.DropSections is null, no sections to load");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading drop sections to UI");
                throw;
            }
        }

        private async Task LoadItemBag(string filePath)
        {
            try
            {
                _logger.LogInformation("Loading ItemBag from {FilePath}", filePath);
                UpdateStatus($"Loading ItemBag from {filePath}...");
                
                var itemBag = await _itemBagService.LoadItemBagAsync(filePath);
                if (itemBag != null)
                {
                    _logger.LogInformation("ItemBag loaded successfully, updating UI");
                    _currentItemBag = itemBag;
                    _currentItemBagPath = filePath;
                    
                    LoadItemBagToUI();
                    UpdateStatus($"ItemBag loaded successfully: {itemBag.Config?.Name ?? "Unknown"}");
                    
                    if (txtFileInfo != null)
                    {
                        txtFileInfo.Text = $"ItemBag: {Path.GetFileName(filePath)}";
                    }
                    else
                    {
                        _logger.LogWarning("txtFileInfo is null, cannot update file info");
                    }
                    
                    _logger.LogInformation("ItemBag UI updated successfully");
                }
                else
                {
                    _logger.LogError("Failed to load ItemBag from {FilePath}", filePath);
                    MessageBox.Show("Failed to load ItemBag", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateStatus("Failed to load ItemBag");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ItemBag from {FilePath}", filePath);
                MessageBox.Show($"Error loading ItemBag: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Error loading ItemBag");
            }
        }

        private void LoadItemBagToUI()
        {
            try
            {
                _logger.LogDebug("Loading ItemBag to UI");
                
                if (_currentItemBag == null)
                {
                    _logger.LogWarning("No ItemBag to load to UI");
                    return;
                }

                _logger.LogDebug("Loading basic configuration to UI");
                // Basic configuration is now loaded dynamically in tabs when they are created
                // No need to manually set text values for removed controls
                
                _logger.LogDebug("Loading drop sections to UI");
                // Load drop sections
                LoadDropSectionsToUI();
                
                _logger.LogInformation("ItemBag loaded to UI successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ItemBag to UI");
                throw;
            }
        }

        private void lstItemBags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (lstItemBags.SelectedValue is string filePath)
                {
                    _logger.LogDebug("ItemBag list selection changed to: {FilePath}", filePath);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        _ = LoadItemBag(filePath);
                    }
                    else
                    {
                        _logger.LogWarning("Selected filePath is null or empty");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ItemBag list selection change");
            }
        }

        private void AddItemToCurrentDropSection(Item item)
        {
            try
            {
                if (_currentItemBag?.DropSections?.Count > 0 && tabDropSections?.SelectedIndex >= 0)
                {
                    var section = _currentItemBag.DropSections[tabDropSections.SelectedIndex];
                    if (section?.DropAllows?.Count > 0)
                    {
                        var allow = section.DropAllows[0];
                        if (allow?.Drops?.Count > 0)
                        {
                            var drop = allow.Drops[0];
                            
                            var dropItem = new DropItem
                            {
                                Cat = item.Cat,
                                Index = item.Index,
                                ItemMinLevel = item.ReqLevel,
                                ItemMaxLevel = item.ReqLevel,
                                Name = item.Name,
                                Skill = 0,
                                Luck = 0,
                                Option = 0,
                                Exc = "-1",
                                SetItem = 0,
                                SocketCount = 0,
                                ElementalItem = 0
                            };
                            
                            drop.Items.Add(dropItem);
                            LoadDropSectionsToUI();
                            _logger.LogDebug("Added item {ItemName} to drop section", item.Name ?? "Unknown");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding item to current drop section");
            }
        }



        private void AddItemToDrop(Drop drop, ListBox itemsListBox)
        {
            try
            {
                if (drop == null)
                {
                    _logger.LogWarning("Drop is null");
                    return;
                }

                // Create a new dialog for adding items
                var addItemDialog = new AddItemDialog(_itemListService, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AddItemDialog>());
                if (addItemDialog.ShowDialog() == true && addItemDialog.Results != null && addItemDialog.Results.Count > 0)
                {
                    // Add multiple items
                    foreach (var config in addItemDialog.Results)
                    {
                        // Find the corresponding item for this config
                        var selectedItem = _itemListService.SearchItems("").FirstOrDefault(i => 
                            i.Category == config.Cat && i.Index == config.Index);
                        
                        if (selectedItem != null)
                        {
                            // Create a new DropItem with the configured values
                            var newDropItem = new DropItem
                            {
                                Cat = selectedItem.Category,
                                Index = selectedItem.Index,
                                Name = selectedItem.Name,
                                ItemMinLevel = config?.ItemMinLevel ?? 0,
                                ItemMaxLevel = config?.ItemMaxLevel ?? 0,
                                Durability = config?.Durability ?? 0,
                                Skill = config?.Skill ?? 0,
                                Luck = config?.Luck ?? 0,
                                Option = config?.Option ?? 0,
                                Exc = config?.Exc ?? "-1",
                                SetItem = config?.SetItem ?? 0,
                                ErrtelRank = config?.ErrtelRank ?? 0,
                                SocketCount = config?.SocketCount ?? 0,
                                OptSlotInfo = config?.OptSlotInfo ?? "",
                                ElementalItem = config?.ElementalItem ?? 0,
                                MuunEvolutionItemCat = config?.MuunEvolutionItemCat ?? "",
                                MuunEvolutionItemIndex = config?.MuunEvolutionItemIndex ?? "",
                                Duration = config?.Duration ?? 0,
                                Rate = config?.Rate ?? 0
                            };

                            // Add to the drop
                            if (drop.Items == null)
                                drop.Items = new ObservableCollection<DropItem>();
                            
                            drop.Items.Add(newDropItem);
                            _logger.LogInformation("Added item {Cat}-{Index}: {Name} to drop", newDropItem.Cat, newDropItem.Index, newDropItem.Name);
                            
                            // Live update the ListBox without recreating the entire UI
                            UpdateListBoxItems(itemsListBox, drop);
                            
                            // Update the GroupBox header to show the new item count
                            if (itemsListBox.Parent is GroupBox itemsGroup)
                            {
                                int totalCount = drop.Items?.Count ?? 0;
                                itemsGroup.Header = $"Items ({totalCount} items)";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to drop");
            }
        }

        private void RemoveSelectedItems(Drop drop, ListBox itemsListBox)
        {
            try
            {
                if (drop?.Items == null || itemsListBox.SelectedItems.Count == 0)
                {
                    _logger.LogWarning("No items selected or drop is null");
                    return;
                }

                var result = MessageBox.Show("Are you sure you want to remove these items?", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    foreach (var selectedItem in itemsListBox.SelectedItems)
                    {
                        if (selectedItem is ListBoxItem listBoxItem && listBoxItem.Tag is DropItem item)
                        {
                            drop.Items.Remove(item);
                            _logger.LogInformation("Removed item {Cat}-{Index}: {Name} from drop", item.Cat, item.Index, item.Name);
                        }
                    }
                    
                    // Clear the selection to avoid issues with removed items
                    itemsListBox.SelectedItems.Clear();
                    
                    // Live update the ListBox without recreating the entire UI
                    UpdateListBoxItems(itemsListBox, drop);
                    
                    // Update the GroupBox header to show the new item count
                    if (itemsListBox.Parent is GroupBox itemsGroup)
                    {
                        int totalCount = drop.Items?.Count ?? 0;
                        itemsGroup.Header = $"Items ({totalCount} items)";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing selected items");
            }
        }

        private void RemoveItem(DropItem item, Drop drop, ListBox itemsListBox)
        {
            try
            {
                if (drop?.Items == null || item == null)
                {
                    _logger.LogWarning("Item or drop is null");
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to remove {item.Cat}-{item.Index}: {item.Name ?? "Unknown"}?", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    drop.Items.Remove(item);
                    _logger.LogInformation("Removed item {Cat}-{Index}: {Name} from drop", item.Cat, item.Index, item.Name);
                    
                    // Live update the ListBox without recreating the entire UI
                    UpdateListBoxItems(itemsListBox, drop);
                    
                    // Update the GroupBox header to show the new item count
                    if (itemsListBox.Parent is GroupBox itemsGroup)
                    {
                        int totalCount = drop.Items?.Count ?? 0;
                        itemsGroup.Header = $"Items ({totalCount} items)";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item");
            }
        }

        private void EditItem(DropItem item, Drop drop, ListBox itemsListBox)
        {
            try
            {
                if (item == null)
                {
                    _logger.LogWarning("Item is null");
                    return;
                }

                // Create and show the item editing dialog
                var editItemDialog = new EditItemDialog(item);
                if (editItemDialog.ShowDialog() == true)
                {
                    _logger.LogInformation("Updated item {Cat}-{Index}: {Name}", item.Cat, item.Index, item.Name);
                    
                    // Live update the ListBox without recreating the entire UI
                    UpdateListBoxItems(itemsListBox, drop);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing item");
            }
        }

        private void EditSelectedItems(Drop drop, ListBox itemsListBox)
        {
            try
            {
                if (drop?.Items == null || itemsListBox.SelectedItems.Count == 0)
                {
                    _logger.LogWarning("No items selected or drop is null");
                    return;
                }

                if (itemsListBox.SelectedItems.Count == 1)
                {
                    // Single item selected, edit it directly
                    if (itemsListBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is DropItem item)
                    {
                        EditItem(item, drop, itemsListBox);
                    }
                }
                else
                {
                    // Multiple items selected, show a message
                    var result = MessageBox.Show($"You have {itemsListBox.SelectedItems.Count} items selected. " +
                        "Currently, only single item editing is supported. Would you like to edit the first selected item?", 
                        "Multiple Items Selected", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        if (itemsListBox.SelectedItems[0] is ListBoxItem firstItem && firstItem.Tag is DropItem item)
                        {
                            EditItem(item, drop, itemsListBox);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing selected items");
            }
        }

        private void RefreshCurrentDropSection()
        {
            try
            {
                if (_currentItemBag == null || tabDropSections == null)
                {
                    _logger.LogWarning("Cannot refresh drop section - ItemBag or tabDropSections is null");
                    return;
                }

                int currentTabIndex = tabDropSections.SelectedIndex;
                _logger.LogDebug("RefreshCurrentDropSection: Current tab index before refresh: {TabIndex}", currentTabIndex);
                
                if (currentTabIndex < 0 || currentTabIndex >= _currentItemBag.DropSections.Count)
                {
                    _logger.LogWarning("Current tab index is invalid: {TabIndex}, total sections: {TotalSections}", 
                        currentTabIndex, _currentItemBag.DropSections?.Count ?? 0);
                    return;
                }

                var currentSection = _currentItemBag.DropSections[currentTabIndex];
                if (currentSection == null)
                {
                    _logger.LogWarning("Current section is null");
                    return;
                }

                _logger.LogDebug("Refreshing current drop section: {SectionName}", currentSection.DisplayName ?? "Unknown");

                // Store current tab selection
                string currentTabHeader = "";
                if (currentTabIndex >= 0 && currentTabIndex < tabDropSections.Items.Count)
                {
                    var currentTab = tabDropSections.Items[currentTabIndex] as TabItem;
                    currentTabHeader = currentTab?.Header?.ToString() ?? "";
                    _logger.LogDebug("Current tab header: {Header}", currentTabHeader);
                }

                // Recreate the current tab content
                var newTabContent = CreateDropSectionContent(currentSection);
                if (newTabContent != null)
                {
                    var currentTab = tabDropSections.Items[currentTabIndex] as TabItem;
                    if (currentTab != null)
                    {
                        currentTab.Content = newTabContent;
                        _logger.LogDebug("Successfully refreshed current drop section tab. Tab index after refresh: {TabIndex}", tabDropSections.SelectedIndex);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to create new tab content for current section");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing current drop section");
            }
        }

        private void UpdateItemsListOnly(Drop drop)
        {
            try
            {
                if (_currentItemBag == null || tabDropSections == null)
                {
                    _logger.LogWarning("Cannot update items list - ItemBag or tabDropSections is null");
                    return;
                }

                int currentTabIndex = tabDropSections.SelectedIndex;
                _logger.LogDebug("UpdateItemsListOnly: Current tab index: {TabIndex}", currentTabIndex);
                
                if (currentTabIndex < 0 || currentTabIndex >= tabDropSections.Items.Count)
                {
                    _logger.LogWarning("UpdateItemsListOnly: Current tab index is invalid: {TabIndex}", currentTabIndex);
                    return;
                }

                var currentTab = tabDropSections.Items[currentTabIndex] as TabItem;
                if (currentTab == null)
                {
                    _logger.LogWarning("UpdateItemsListOnly: Current tab is null");
                    return;
                }

                _logger.LogDebug("UpdateItemsListOnly: Current tab header: {Header}", currentTab.Header?.ToString() ?? "Unknown");

                // The currentTab.Content is a TabControl, we need to find the selected tab within it
                if (currentTab.Content is TabControl innerTabControl)
                {
                    _logger.LogDebug("UpdateItemsListOnly: Found inner TabControl with {TabCount} tabs", innerTabControl.Items.Count);
                    
                    // Find the "Drop Configuration" tab which should contain our ListBox
                    TabItem? dropConfigTab = null;
                    foreach (var tab in innerTabControl.Items)
                    {
                        if (tab is TabItem tabItem && tabItem.Header?.ToString()?.Contains("Drop Configuration") == true)
                        {
                            dropConfigTab = tabItem;
                            _logger.LogDebug("UpdateItemsListOnly: Found Drop Configuration tab");
                            break;
                        }
                    }

                    if (dropConfigTab != null)
                    {
                        // Now search for the ListBox within the Drop Configuration tab content
                        var listBox = FindListBoxInContent(dropConfigTab.Content);
                        if (listBox != null)
                        {
                            _logger.LogDebug("UpdateItemsListOnly: Found ListBox in Drop Configuration tab, updating items. Current item count: {CurrentCount}, Drop items count: {DropCount}", 
                                listBox.Items.Count, drop.Items?.Count ?? 0);
                            UpdateListBoxItems(listBox, drop);
                            _logger.LogDebug("UpdateItemsListOnly: ListBox updated successfully. New item count: {NewCount}", listBox.Items.Count);
                        }
                        else
                        {
                            _logger.LogWarning("UpdateItemsListOnly: Could not find ListBox in Drop Configuration tab content");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("UpdateItemsListOnly: Could not find Drop Configuration tab in inner TabControl");
                    }
                }
                else
                {
                    _logger.LogWarning("UpdateItemsListOnly: Current tab content is not a TabControl, it's: {ContentType}", 
                        currentTab.Content?.GetType()?.Name ?? "null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating items list only");
            }
        }

        private ListBox? FindListBoxInContent(object content)
        {
            try
            {
                _logger.LogDebug("FindListBoxInContent: Searching for ListBox in content type: {ContentType}", content?.GetType().Name ?? "null");
                
                if (content is StackPanel stackPanel)
                {
                    _logger.LogDebug("FindListBoxInContent: Content is StackPanel with {ChildCount} children", stackPanel.Children.Count);
                    foreach (var child in stackPanel.Children)
                    {
                        _logger.LogDebug("FindListBoxInContent: Checking child type: {ChildType}", child?.GetType().Name ?? "null");
                        if (child is GroupBox groupBox)
                        {
                            _logger.LogDebug("FindListBoxInContent: Found GroupBox with header: {Header}", groupBox.Header?.ToString() ?? "null");
                            if (groupBox.Header?.ToString()?.Contains("Items") == true)
                            {
                                if (groupBox.Content is ListBox listBox)
                                {
                                    _logger.LogDebug("FindListBoxInContent: Found ListBox in Items GroupBox");
                                    return listBox;
                                }
                                else
                                {
                                    _logger.LogDebug("FindListBoxInContent: GroupBox content is not ListBox, it's: {ContentType}", 
                                        groupBox.Content?.GetType().Name ?? "null");
                                }
                            }
                        }
                    }
                }
                else if (content is Grid grid)
                {
                    _logger.LogDebug("FindListBoxInContent: Content is Grid, searching through children");
                    return FindListBoxInGrid(grid);
                }
                
                _logger.LogDebug("FindListBoxInContent: No ListBox found in primary search");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding ListBox in content");
                return null;
            }
        }

        private ListBox? FindListBoxAlternative(object content)
        {
            try
            {
                _logger.LogDebug("FindListBoxAlternative: Trying alternative search method");
                
                // Try to find ListBox recursively in any container
                if (content is FrameworkElement element)
                {
                    return FindListBoxRecursive(element);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alternative ListBox search");
                return null;
            }
        }

        private ListBox? FindListBoxInGrid(Grid grid)
        {
            try
            {
                foreach (var child in grid.Children)
                {
                    if (child is ListBox listBox)
                    {
                        _logger.LogDebug("FindListBoxInGrid: Found ListBox directly in Grid");
                        return listBox;
                    }
                    else if (child is FrameworkElement frameworkElement)
                    {
                        var foundListBox = FindListBoxRecursive(frameworkElement);
                        if (foundListBox != null)
                            return foundListBox;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Grid for ListBox");
                return null;
            }
        }

        private ListBox? FindListBoxRecursive(FrameworkElement element)
        {
            try
            {
                if (element is ListBox listBox)
                {
                    _logger.LogDebug("FindListBoxRecursive: Found ListBox: {Name}", element.Name ?? "unnamed");
                    return listBox;
                }

                if (element is Panel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is FrameworkElement childElement)
                        {
                            var found = FindListBoxRecursive(childElement);
                            if (found != null)
                                return found;
                        }
                    }
                }
                else if (element is ContentControl contentControl && contentControl.Content is FrameworkElement contentElement)
                {
                    return FindListBoxRecursive(contentElement);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recursive ListBox search");
                return null;
            }
        }

        private void UpdateListBoxItems(ListBox listBox, Drop drop)
        {
            try
            {
                _logger.LogDebug("UpdateListBoxItems: Starting update. ListBox has {CurrentCount} items, Drop has {DropCount} items", 
                    listBox.Items.Count, drop.Items?.Count ?? 0);
                
                int originalCount = listBox.Items.Count;
                listBox.Items.Clear();
                _logger.LogDebug("UpdateListBoxItems: Cleared ListBox items");
                
                if (drop.Items != null && drop.Items.Count > 0)
                {
                    _logger.LogDebug("UpdateListBoxItems: Processing {ItemCount} items from drop", drop.Items.Count);
                    foreach (var item in drop.Items)
                    {
                        if (item != null)
                        {
                            var itemText = $"{item.Cat}-{item.Index}: {item.Name ?? "Unknown"}";
                            
                            // Basic properties
                            if (item.ItemMinLevel > 0 || item.ItemMaxLevel > 0)
                                itemText += $" (Level: {item.ItemMinLevel}-{item.ItemMaxLevel})";
                            
                            // Enhancement properties
                            if (item.Skill != 0 || item.Luck != 0 || item.Option != 0)
                                itemText += $" [S:{item.Skill} L:{item.Luck} O:{item.Option}]";
                            
                            // Excellent options
                            if (item.Exc != "-1")
                                itemText += $" [Exc:{item.Exc}]";
                            
                            // Socket and elemental
                            if (item.SocketCount > 0)
                                itemText += $" [Socket:{item.SocketCount}]";
                            if (item.ElementalItem > 0)
                                itemText += $" [Element:{item.ElementalItem}]";
                            
                            // Additional properties
                            if (item.SetItem > 0)
                                itemText += $" [Set:{item.SetItem}]";
                            if (item.ErrtelRank > 0)
                                itemText += $" [Errtel:{item.ErrtelRank}]";
                            if (!string.IsNullOrEmpty(item.OptSlotInfo) && item.OptSlotInfo != "0")
                                itemText += $" [OptSlots:{item.OptSlotInfo}]";
                            if (!string.IsNullOrEmpty(item.MuunEvolutionItemCat) && item.MuunEvolutionItemCat != "0")
                                itemText += $" [Muun:{item.MuunEvolutionItemCat}-{item.MuunEvolutionItemIndex}]";
                            if (item.Duration > 0)
                                itemText += $" [Duration:{item.Duration}s]";
                            if (item.Rate > 0)
                                itemText += $" [Rate:{item.Rate}]";
                            
                            var listItem = new ListBoxItem
                            {
                                Content = itemText,
                                Tag = item
                            };
                            
                            // Add context menu for editing
                            var contextMenu = new ContextMenu();
                            var editMenuItem = new MenuItem { Header = "Edit Item" };
                            editMenuItem.Click += (s, e) => EditItem(item, drop, listBox);
                            var removeMenuItem = new MenuItem { Header = "Remove Item" };
                            removeMenuItem.Click += (s, e) => RemoveItem(item, drop, listBox);
                            
                            contextMenu.Items.Add(editMenuItem);
                            contextMenu.Items.Add(removeMenuItem);
                            listItem.ContextMenu = contextMenu;
                            
                            listBox.Items.Add(listItem);
                            _logger.LogDebug("UpdateListBoxItems: Added item: {ItemText}", itemText);
                        }
                    }
                }
                else
                {
                    // Show a message when there are no items
                    listBox.Items.Add("No items added yet. Use the 'Add Item' button to add items to this section.");
                    _logger.LogDebug("UpdateListBoxItems: Added 'no items' message");
                }
                
                _logger.LogDebug("UpdateListBoxItems: Update complete. Original count: {OriginalCount}, New count: {NewCount}", 
                    originalCount, listBox.Items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ListBox items");
            }
        }

        private void AddNewDropSection(DropAllow? dropAllow)
        {
            try
            {
                if (dropAllow == null)
                {
                    _logger.LogWarning("DropAllow is null, cannot add new drop section");
                    return;
                }

                // Create a new Drop section with default values
                var newDrop = new Drop
                {
                    Rate = 1000, // Default rate
                    Type = 0,     // Default type
                    Count = 1,    // Default count
                    Items = new ObservableCollection<DropItem>()
                };

                // Add the new drop to the DropAllow
                if (dropAllow.Drops == null)
                {
                    dropAllow.Drops = new ObservableCollection<Drop>();
                }
                
                dropAllow.Drops.Add(newDrop);
                _logger.LogInformation("Added new drop section with rate {Rate}, type {Type}, count {Count}", 
                    newDrop.Rate, newDrop.Type, newDrop.Count);

                // Refresh the UI to show the new drop section
                // We need to refresh the entire drop sections UI, not just the current tab
                LoadDropSectionsToUI();
                
                MessageBox.Show($"New drop section added successfully!\nRate: {newDrop.Rate}\nType: {newDrop.Type}\nCount: {newDrop.Count}", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new drop section");
                MessageBox.Show($"Error adding new drop section: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private UIElement CreateDropConfigurationTab(DropAllow dropAllow)
        {
            try
            {
                var mainStackPanel = new StackPanel();
                
                if (dropAllow?.Drops != null && dropAllow.Drops.Count > 0)
                {
                    // Create a section for each Drop
                    for (int dropIndex = 0; dropIndex < dropAllow.Drops.Count; dropIndex++)
                    {
                        var drop = dropAllow.Drops[dropIndex];
                        var dropSection = CreateSingleDropSection(drop, dropIndex, dropAllow);
                        mainStackPanel.Children.Add(dropSection);
                        
                        // Add separator between drop sections (except for the last one)
                        if (dropIndex < dropAllow.Drops.Count - 1)
                        {
                            var separator = new Separator
                            {
                                Margin = new Thickness(0, 20, 0, 20),
                                Style = FindResource("ModernSeparator") as Style
                            };
                            mainStackPanel.Children.Add(separator);
                        }
                    }
                }
                else
                {
                    // No drops yet, show message
                    var noDropsMessage = new TextBlock
                    {
                        Text = "No drop sections configured yet. Use the 'Add Drop Section' button to create your first drop section.",
                        Style = FindResource("ModernTextBlock") as Style,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    mainStackPanel.Children.Add(noDropsMessage);
                }

                // Add the "Add Drop Section" button at the bottom
                var addDropSectionButton = new Button
                {
                    Content = "Add Drop Section",
                    Width = 150,
                    Height = 30,
                    Margin = new Thickness(0, 20, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Style = FindResource("ModernButton") as Style,
                    ToolTip = "Add a new drop section with different rate, type, and count"
                };
                addDropSectionButton.Click += (s, e) => AddNewDropSection(dropAllow);
                mainStackPanel.Children.Add(addDropSectionButton);

                return mainStackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating drop configuration tab");
                return new TextBlock { Text = "Error creating drop configuration tab", Foreground = System.Windows.Media.Brushes.Red };
            }
        }

        private UIElement CreateSingleDropSection(Drop drop, int dropIndex, DropAllow dropAllow)
        {
            try
            {
                var stackPanel = new StackPanel();
                
                // Drop Section Header
                var dropHeaderGroup = new GroupBox
                {
                    Header = $"Drop Section {dropIndex + 1} (Rate: {drop.Rate}, Type: {drop.Type}, Count: {drop.Count})",
                    Style = FindResource("ModernGroupBox") as Style,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var dropHeaderGrid = new Grid();
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Rate Label
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); // Rate Box
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Type Label
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); // Type Box
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Count Label
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) }); // Count Box
                dropHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Remove Button
                dropHeaderGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Rate
                var rateLabel = new Label { Content = "Rate:", Style = FindResource("ModernLabel") as Style };
                var rateBox = new TextBox
                {
                    Text = drop.Rate.ToString(),
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                rateBox.TextChanged += (s, e) => UpdateDropRate(drop, rateBox.Text);

                // Type
                var typeLabel = new Label { Content = "Type:", Style = FindResource("ModernLabel") as Style };
                var typeBox = new TextBox
                {
                    Text = drop.Type.ToString(),
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                typeBox.TextChanged += (s, e) => UpdateDropType(drop, typeBox.Text);

                // Count
                var countLabel = new Label { Content = "Count:", Style = FindResource("ModernLabel") as Style };
                var countBox = new TextBox
                {
                    Text = drop.Count.ToString(),
                    Style = FindResource("ModernTextBox") as Style,
                    Margin = new Thickness(4),
                    Width = 80
                };
                countBox.TextChanged += (s, e) => UpdateDropCount(drop, countBox.Text);

                // Remove Drop Section button
                var removeDropButton = new Button
                {
                    Content = "Remove Section",
                    Width = 160,
                    Height = 30,
                    Style = FindResource("ModernButton") as Style,
                    Margin = new Thickness(4),
                    ToolTip = "Remove this drop section"
                };
                removeDropButton.Click += (s, e) => RemoveDropSection(drop, dropAllow);

                // Set positions
                Grid.SetColumn(rateLabel, 0);
                Grid.SetColumn(rateBox, 1);
                Grid.SetColumn(typeLabel, 2);
                Grid.SetColumn(typeBox, 3);
                Grid.SetColumn(countLabel, 4);
                Grid.SetColumn(countBox, 5);
                Grid.SetColumn(removeDropButton, 6);

                dropHeaderGrid.Children.Add(rateLabel);
                dropHeaderGrid.Children.Add(rateBox);
                dropHeaderGrid.Children.Add(typeLabel);
                dropHeaderGrid.Children.Add(typeBox);
                dropHeaderGrid.Children.Add(countLabel);
                dropHeaderGrid.Children.Add(countBox);
                dropHeaderGrid.Children.Add(removeDropButton);

                dropHeaderGroup.Content = dropHeaderGrid;
                stackPanel.Children.Add(dropHeaderGroup);

                // Items List
                var itemsGroup = new GroupBox
                {
                    Header = $"Items ({drop.Items?.Count ?? 0} items)",
                    Style = FindResource("ModernGroupBox") as Style
                };

                var itemsListBox = new ListBox
                {
                    Style = FindResource("ModernListBox") as Style,
                    MaxHeight = 300,
                    SelectionMode = SelectionMode.Extended
                };

                // Update header when selection changes
                itemsListBox.SelectionChanged += (s, e) =>
                {
                    int selectedCount = itemsListBox.SelectedItems.Count;
                    int totalCount = drop.Items?.Count ?? 0;
                    if (selectedCount > 0)
                    {
                        itemsGroup.Header = $"Items ({totalCount} items) - {selectedCount} selected";
                    }
                    else
                    {
                        itemsGroup.Header = $"Items ({totalCount} items)";
                    }
                };

                // Add multi-selection context menu
                var multiContextMenu = new ContextMenu();
                var multiEditMenuItem = new MenuItem { Header = "Edit Selected Items" };
                multiEditMenuItem.Click += (s, e) => EditSelectedItems(drop, itemsListBox);
                var multiRemoveMenuItem = new MenuItem { Header = "Remove Selected Items" };
                multiRemoveMenuItem.Click += (s, e) => RemoveSelectedItems(drop, itemsListBox);

                multiContextMenu.Items.Add(multiEditMenuItem);
                multiContextMenu.Items.Add(multiRemoveMenuItem);
                itemsListBox.ContextMenu = multiContextMenu;

                // Add keyboard shortcuts
                itemsListBox.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Delete)
                    {
                        if (itemsListBox.SelectedItems.Count > 0)
                        {
                            RemoveSelectedItems(drop, itemsListBox);
                            e.Handled = true;
                        }
                    }
                    else if (e.Key == System.Windows.Input.Key.A && 
                             (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
                    {
                        itemsListBox.SelectAll();
                        e.Handled = true;
                    }
                };

                // Add double-click event handler to open item editor
                itemsListBox.MouseDoubleClick += (s, e) =>
                {
                    if (itemsListBox.SelectedItem is ListBoxItem selectedListItem && selectedListItem.Tag is DropItem selectedItem)
                    {
                        EditItem(selectedItem, drop, itemsListBox);
                        e.Handled = true;
                    }
                };

                // Populate items list
                if (drop.Items != null && drop.Items.Count > 0)
                {
                    foreach (var item in drop.Items)
                    {
                        if (item != null)
                        {
                            var itemText = $"{item.Cat}-{item.Index}: {item.Name ?? "Unknown"}";
                            
                            // Basic properties
                            if (item.ItemMinLevel > 0 || item.ItemMaxLevel > 0)
                                itemText += $" (Level: {item.ItemMinLevel}-{item.ItemMaxLevel})";
                            
                            // Enhancement properties
                            if (item.Skill != 0 || item.Luck != 0 || item.Option != 0)
                                itemText += $" [S:{item.Skill} L:{item.Luck} O:{item.Option}]";
                            
                            // Excellent options
                            if (item.Exc != "-1")
                                itemText += $" [Exc:{item.Exc}]";
                            
                            // Socket and elemental
                            if (item.SocketCount > 0)
                                itemText += $" [Socket:{item.SocketCount}]";
                            if (item.ElementalItem > 0)
                                itemText += $" [Element:{item.ElementalItem}]";
                            
                            // Additional properties
                            if (item.SetItem > 0)
                                itemText += $" [Errtel:{item.ErrtelRank}]";
                            if (item.ErrtelRank > 0)
                                itemText += $" [Errtel:{item.ErrtelRank}]";
                            if (!string.IsNullOrEmpty(item.OptSlotInfo) && item.OptSlotInfo != "0")
                                itemText += $" [OptSlots:{item.OptSlotInfo}]";
                            if (!string.IsNullOrEmpty(item.MuunEvolutionItemCat) && item.MuunEvolutionItemCat != "0")
                                itemText += $" [Muun:{item.MuunEvolutionItemCat}-{item.MuunEvolutionItemIndex}]";
                            if (item.Duration > 0)
                                itemText += $" [Duration:{item.Duration}s]";
                            if (item.Rate > 0)
                                itemText += $" [Rate:{item.Rate}]";
                            
                            var listItem = new ListBoxItem
                            {
                                Content = itemText,
                                Tag = item
                            };
                            
                            // Add context menu for editing
                            var contextMenu = new ContextMenu();
                            var editMenuItem = new MenuItem { Header = "Edit Item" };
                            editMenuItem.Click += (s, e) => EditItem(item, drop, itemsListBox);
                            var removeMenuItem = new MenuItem { Header = "Remove Item" };
                            removeMenuItem.Click += (s, e) => RemoveItem(item, drop, itemsListBox);
                            
                            contextMenu.Items.Add(editMenuItem);
                            contextMenu.Items.Add(removeMenuItem);
                            listItem.ContextMenu = contextMenu;
                            
                            itemsListBox.Items.Add(listItem);
                        }
                    }
                }
                else
                {
                    itemsListBox.Items.Add("No items added yet. Use the 'Add Item' button to add items to this section.");
                }

                itemsGroup.Content = itemsListBox;
                stackPanel.Children.Add(itemsGroup);

                // Add buttons for item management
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var addItemButton = new Button
                {
                    Content = "Add Item",
                    Width = 120,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = FindResource("ModernButton") as Style,
                    ToolTip = "Add a new item to this drop section"
                };
                addItemButton.Click += (s, e) => AddItemToDrop(drop, itemsListBox);

                var removeItemButton = new Button
                {
                    Content = "Remove Selected Items",
                    Width = 180,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = FindResource("ModernButton") as Style,
                    ToolTip = "Remove selected items from this drop section"
                };
                removeItemButton.Click += (s, e) => RemoveSelectedItems(drop, itemsListBox);

                buttonPanel.Children.Add(addItemButton);
                buttonPanel.Children.Add(removeItemButton);
                stackPanel.Children.Add(buttonPanel);

                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating single drop section");
                return new TextBlock { Text = "Error creating drop section", Foreground = System.Windows.Media.Brushes.Red };
            }
        }

        private void UpdateDropRate(Drop drop, string rateText)
        {
            try
            {
                if (int.TryParse(rateText, out int rate))
                {
                    drop.Rate = rate;
                    _logger.LogDebug("Updated drop rate to: {Rate}", rate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating drop rate");
            }
        }

        private void UpdateDropType(Drop drop, string typeText)
        {
            try
            {
                if (int.TryParse(typeText, out int type))
                {
                    drop.Type = type;
                    _logger.LogDebug("Updated drop type to: {Type}", type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating drop type");
            }
        }

        private void UpdateDropCount(Drop drop, string countText)
        {
            try
            {
                if (int.TryParse(countText, out int count))
                {
                    drop.Count = count;
                    _logger.LogDebug("Updated drop count to: {Count}", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating drop count");
            }
        }

        private void RemoveDropSection(Drop drop, DropAllow dropAllow)
        {
            try
            {
                if (dropAllow?.Drops == null)
                {
                    _logger.LogWarning("DropAllow or Drops is null");
                    return;
                }

                var result = MessageBox.Show("Are you sure you want to remove this drop section? This will also remove all items within it.", 
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    dropAllow.Drops.Remove(drop);
                    _logger.LogInformation("Removed drop section with rate {Rate}, type {Type}, count {Count}", 
                        drop.Rate, drop.Type, drop.Count);
                    
                    // Refresh the UI to show the updated drop sections
                    // We need to refresh the entire drop sections UI, not just the current tab
                    LoadDropSectionsToUI();
                    
                    MessageBox.Show("Drop section removed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing drop section");
                MessageBox.Show($"Error removing drop section: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves the current configuration to the ItemBag object
        /// This method is called automatically when switching between tabs
        /// </summary>
        private void SaveCurrentConfiguration()
        {
            try
            {
                if (_currentItemBag == null)
                {
                    _logger.LogDebug("No current ItemBag to save");
                    return;
                }

                _logger.LogDebug("Auto-saving current configuration");
                
                // The configuration is already being updated in real-time through the TextChanged events
                // This method serves as a safety net to ensure all changes are properly saved
                // and can be used for any additional validation or processing needed
                
                // Update the status to indicate changes have been saved
                UpdateStatus("Configuration auto-saved");
                
                _logger.LogDebug("Configuration auto-save completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-save of configuration");
                UpdateStatus("Error during auto-save");
            }
        }
    }
}
