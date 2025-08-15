/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ItemBagEditor.Models;
using ItemBagEditor.Services;
using Microsoft.Extensions.Logging;

namespace ItemBagEditor
{
    public partial class AddItemDialog : Window
    {
        private readonly IItemListService _itemListService;
        private readonly ILogger<AddItemDialog> _logger;
        private string _imagesPath = "";
        private readonly string _sourceImagesPath = @"C:\Users\ennzo\OneDrive\Desktop\Tools\IBC\images\items";
        private List<Item> _allItems;
        private List<Item> _filteredItems;
        private Item? _selectedItem;

        public List<DropItemConfiguration>? Results { get; private set; }
        public DropItemConfiguration? Result => Results?.FirstOrDefault();
        public Item? SelectedItem => _selectedItem;

        public AddItemDialog(IItemListService itemListService, ILogger<AddItemDialog> logger)
        {
            InitializeComponent();
            _itemListService = itemListService;
            _logger = logger;
            _allItems = new List<Item>();
            _filteredItems = new List<Item>();
            
            // Set up images path and copy images if needed
            SetupImagesPath();
            
            InitializeControls();
            LoadCategories();
            LoadItems();
        }

        private void SetupImagesPath()
        {
            try
            {
                // Set images path to executable directory
                string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                _imagesPath = Path.Combine(exeDir, "images", "items");
                
                // Create images directory if it doesn't exist
                if (!Directory.Exists(_imagesPath))
                {
                    Directory.CreateDirectory(_imagesPath);
                }
                
                // Copy images from source directory if they exist
                if (Directory.Exists(_sourceImagesPath))
                {
                    CopyImagesFromSource();
                }
                else
                {
                    _logger.LogWarning("Source images directory not found: {SourcePath}", _sourceImagesPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up images path");
            }
        }

        private void CopyImagesFromSource()
        {
            try
            {
                _logger.LogInformation("Copying images from source directory to executable directory");
                
                // Copy all files from source to destination
                foreach (string filePath in Directory.GetFiles(_sourceImagesPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(_imagesPath, fileName);
                    
                    // Only copy if destination doesn't exist or source is newer
                    if (!File.Exists(destPath) || File.GetLastWriteTime(filePath) > File.GetLastWriteTime(destPath))
                    {
                        File.Copy(filePath, destPath, true);
                        _logger.LogDebug("Copied image: {FileName}", fileName);
                    }
                }
                
                _logger.LogInformation("Images copied successfully to: {DestPath}", _imagesPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying images from source directory");
            }
        }

        private void InitializeControls()
        {
            // Set default values for combo boxes
            cmbSkill.SelectedIndex = 0;
            cmbLuck.SelectedIndex = 0;
            cmbOption.SelectedIndex = 0;
            cmbExc.SelectedIndex = 0;
            cmbSetItem.SelectedIndex = 0;
            cmbSocketCount.SelectedIndex = 0;
            cmbElementalItem.SelectedIndex = 0;
            cmbErrtelRank.SelectedIndex = 0;
            cmbDurationPreset.SelectedIndex = 0;
            
            // Initialize checkboxes
            chkEnableErrtelRank.IsChecked = false;
            
            // Most properties are always included in ItemBag configuration files
            // Only Errtel Rank has a checkbox as it's only applicable to specific item types
        }

        private void LoadCategories()
        {
            try
            {
                _logger.LogInformation("Loading categories from ItemListService");
                var categories = _itemListService.GetCategories();
                _logger.LogInformation("Categories loaded: {Count} categories", categories.Count);
                cmbCategory.Items.Clear();
                
                foreach (var category in categories)
                {
                    cmbCategory.Items.Add($"Category {category}");
                }
                
                if (cmbCategory.Items.Count > 0)
                    cmbCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
            }
        }

        private void LoadItems()
        {
            try
            {
                _logger.LogInformation("Loading items from ItemListService");
                _allItems = _itemListService.SearchItems("");
                _logger.LogInformation("Items loaded: {Count} total items", _allItems.Count);
                _filteredItems = _allItems.ToList();
                UpdateItemList();
                UpdateItemCount();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading items");
            }
        }

        private void UpdateItemList()
        {
            lstItems.Items.Clear();
            foreach (var item in _filteredItems)
            {
                lstItems.Items.Add($"{item.Index}: {item.Name} (Level {item.ReqLevel})");
            }
        }

        private void UpdateItemCount()
        {
            txtItemCount.Text = $"{_filteredItems.Count} items";
            UpdateSelectedCount();
        }
        
        private void UpdateSelectedCount()
        {
            int selectedCount = lstItems.SelectedItems.Count;
            txtSelectedCount.Text = $" | {selectedCount} selected";
            btnAdd.IsEnabled = selectedCount > 0;
        }

        private void cmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategory.SelectedIndex >= 0 && cmbCategory.SelectedIndex < _itemListService.GetCategories().Count)
            {
                var selectedCategory = _itemListService.GetCategories()[cmbCategory.SelectedIndex];
                _filteredItems = _allItems.Where(i => i.Category == selectedCategory).ToList();
                UpdateItemList();
                UpdateItemCount();
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = txtSearch.Text;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _filteredItems = _allItems.ToList();
            }
            else
            {
                _filteredItems = _allItems.Where(i => 
                    i.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.Index.ToString().Contains(searchTerm)
                ).ToList();
            }
            UpdateItemList();
            UpdateItemCount();
        }

        private void lstItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedCount();
            
            if (lstItems.SelectedItems.Count == 1)
            {
                // Single item selected - show details
                var selectedIndex = lstItems.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < _filteredItems.Count)
                {
                    _selectedItem = _filteredItems[selectedIndex];
                    UpdateSelectedItemInfo();
                    LoadItemImage();
                }
            }
            else if (lstItems.SelectedItems.Count > 1)
            {
                // Multiple items selected - show summary
                _selectedItem = null;
                ShowMultipleItemsSummary();
                ClearItemImage();
            }
            else
            {
                // No items selected
                _selectedItem = null;
                ClearSelectedItemInfo();
                ClearItemImage();
            }
        }

        private void UpdateSelectedItemInfo()
        {
            if (_selectedItem != null)
            {
                txtSelectedItemName.Text = _selectedItem.Name;
                txtSelectedItemCategory.Text = _selectedItem.Category.ToString();
                txtSelectedItemIndex.Text = _selectedItem.Index.ToString();
                btnAdd.IsEnabled = true;
                
                // Show Evolution Stone options only for Evolution Stone items
                ShowEvolutionStoneOptions();
                
                // Show Errtel Rank options only for applicable Errtel items
                ShowErrtelRankOptions();
                
                // Set intelligent defaults based on item type
                SetIntelligentDefaults();
            }
        }

        private void ClearSelectedItemInfo()
        {
            txtSelectedItemName.Text = "No item selected";
            txtSelectedItemCategory.Text = "-";
            txtSelectedItemIndex.Text = "-";
            btnAdd.IsEnabled = false;
        }

        private void LoadItemImage()
        {
            try
            {
                if (_selectedItem == null)
                {
                    ClearItemImage();
                    return;
                }

                // Check if images directory exists
                if (!Directory.Exists(_imagesPath))
                {
                    ClearItemImage();
                    return;
                }

                // Construct the image path based on category and index
                string imageFileName = $"{_selectedItem.Category}-{_selectedItem.Index}.gif";
                string imagePath = Path.Combine(_imagesPath, imageFileName);
                
                // Check if the specific image exists
                if (File.Exists(imagePath))
                {
                    // Load the specific item image
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imagePath);
                    bitmap.EndInit();
                    imgItemPreview.Source = bitmap;
                }
                else
                {
                    // Load the default "no image" placeholder
                    string noImagePath = Path.Combine(_imagesPath, "item_noimg.png");
                    if (File.Exists(noImagePath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(noImagePath);
                        bitmap.EndInit();
                        imgItemPreview.Source = bitmap;
                    }
                    else
                    {
                        // If even the no-image file doesn't exist, clear the image
                        imgItemPreview.Source = null;
                    }
                }
            }
            catch (Exception ex)
            {
                // If there's any error loading the image, just clear it
                imgItemPreview.Source = null;
                _logger.LogWarning(ex, "Error loading item image for category {Category}, index {Index}", 
                    _selectedItem?.Category, _selectedItem?.Index);
            }
        }

        private void ClearItemImage()
        {
            imgItemPreview.Source = null;
        }
        
        private void ShowMultipleItemsSummary()
        {
            txtSelectedItemName.Text = $"{lstItems.SelectedItems.Count} items selected";
            txtSelectedItemCategory.Text = "Multiple";
            txtSelectedItemIndex.Text = "Multiple";
        }
        
        private void ShowEvolutionStoneOptions()
        {
            if (_selectedItem != null)
            {
                // Check if this is an Evolution Stone item
                bool isEvolutionStone = (_selectedItem.Category == 16 && _selectedItem.Index == 211) || 
                                       (_selectedItem.Category == 20 && _selectedItem.Index == 510);
                
                if (isEvolutionStone)
                {
                    evolutionStonePanel.Visibility = Visibility.Visible;
                    // Reset to default selection
                    cmbEvolutionStoneVariant.SelectedIndex = 0;
                }
                else
                {
                    evolutionStonePanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                evolutionStonePanel.Visibility = Visibility.Collapsed;
            }
        }
        
        private void ShowErrtelRankOptions()
        {
            if (_selectedItem != null)
            {
                // Check if this is an Errtel item (Category 12 or 13, Index 200-299 are typically Errtels)
                bool isErrtelItem = ((_selectedItem.Category == 12 || _selectedItem.Category == 13) && 
                                    _selectedItem.Index >= 200 && _selectedItem.Index <= 299);
                
                if (isErrtelItem)
                {
                    // Show Errtel Rank options for non-Beginner Errtels
                    chkEnableErrtelRank.IsEnabled = true;
                    chkEnableErrtelRank.ToolTip = "Enable to set Errtel Rank for this item. Only applies to Errtel items (Category 12/13, Index 200-299).";
                    cmbErrtelRank.ToolTip = "Defines rank to drop Errtel item with, 1-5. Does not apply to Errtels of Beginner. Default is 1.";
                }
                                    else
                    {
                        // Disable Errtel Rank for non-Errtel items
                        chkEnableErrtelRank.IsEnabled = false;
                        chkEnableErrtelRank.IsChecked = false;
                        cmbErrtelRank.IsEnabled = false;
                        chkEnableErrtelRank.ToolTip = "ErrtelRank only applies to Errtel items (Category 12/13, Index 200-299).";
                        cmbErrtelRank.ToolTip = "ErrtelRank only applies to Errtel items (Category 12/13, Index 200-299).";
                    }
            }
            else
            {
                chkEnableErrtelRank.IsEnabled = false;
                chkEnableErrtelRank.IsChecked = false;
                cmbErrtelRank.IsEnabled = false;
                chkEnableErrtelRank.ToolTip = "Select an item first to configure Errtel Rank.";
                cmbErrtelRank.ToolTip = "Select an item first to configure Errtel Rank.";
            }
        }
        

        
        private void cmbEvolutionStoneVariant_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method handles the Evolution Stone variant selection
            // The selected values will be used when creating the DropItem
        }
        
        private void cmbDurationPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDurationPreset.SelectedItem is ComboBoxItem selectedPreset)
            {
                string tag = selectedPreset.Tag?.ToString() ?? "0";
                if (int.TryParse(tag, out int duration))
                {
                    txtDuration.Text = duration.ToString();
                }
            }
        }
        
        private void SetIntelligentDefaults()
        {
            if (_selectedItem == null) return;
            
            try
            {
                // Set defaults based on item category
                switch (_selectedItem.Category)
                {
                    case 0: // Swords
                    case 2: // Maces
                    case 3: // Spears
                    case 4: // Bows
                    case 5: // Staffs
                        // Weapons typically have Skill, Luck, and Option
                        cmbSkill.SelectedIndex = 1; // With Skill
                        cmbLuck.SelectedIndex = 1; // With Luck
                        cmbOption.SelectedIndex = 2; // +8 Option
                        cmbExc.SelectedIndex = 1; // Random count of random options
                        cmbSetItem.SelectedIndex = 0; // No Ancient
                        cmbSocketCount.SelectedIndex = 0; // No Sockets
                        cmbElementalItem.SelectedIndex = 0; // No Element
                        break;
                        
                    case 7: // Helmets
                    case 8: // Armor
                    case 9: // Pants
                    case 10: // Gloves
                    case 11: // Boots
                        // Armor typically has Luck and Option
                        cmbSkill.SelectedIndex = 0; // No Skill
                        cmbLuck.SelectedIndex = 1; // With Luck
                        cmbOption.SelectedIndex = 2; // +8 Option
                        cmbExc.SelectedIndex = 1; // Random count of random options
                        cmbSetItem.SelectedIndex = 0; // No Ancient
                        cmbSocketCount.SelectedIndex = 0; // No Sockets
                        cmbElementalItem.SelectedIndex = 0; // No Element
                        break;
                        
                                         case 13: // Accessories (Rings, Pendants, etc.) and Errtels
                         // Check if this is an Errtel item
                         if (_selectedItem.Index >= 200 && _selectedItem.Index <= 299)
                         {
                             // Errtel items typically have no special properties
                             cmbSkill.SelectedIndex = 0; // No Skill
                             cmbLuck.SelectedIndex = 0; // No Luck
                             cmbOption.SelectedIndex = 0; // No Option
                             cmbExc.SelectedIndex = 0; // No exc option
                             cmbSetItem.SelectedIndex = 0; // No Ancient
                             cmbSocketCount.SelectedIndex = 0; // No Sockets
                             cmbElementalItem.SelectedIndex = 0; // No Element
                             
                                                // Enable Errtel Rank for Errtel items
                             chkEnableErrtelRank.IsChecked = true;
                             chkEnableErrtelRank.IsEnabled = true;
                             cmbErrtelRank.IsEnabled = true;
                             cmbErrtelRank.SelectedIndex = 0; // Default (1)
                         }
                         else
                         {
                             // Regular accessories typically have Luck and Option
                             cmbSkill.SelectedIndex = 0; // No Skill
                             cmbLuck.SelectedIndex = 1; // With Luck
                             cmbOption.SelectedIndex = 1; // +4 Option
                             cmbExc.SelectedIndex = 1; // Random count of random options
                             cmbSetItem.SelectedIndex = 0; // No Ancient
                             cmbSocketCount.SelectedIndex = 0; // No Sockets
                             cmbElementalItem.SelectedIndex = 0; // No Element
                             cmbErrtelRank.SelectedIndex = 0; // Default (1)
                         }
                         break;
                        
                                         case 14: // Jewels
                         // Jewels typically have no special properties
                         cmbSkill.SelectedIndex = 0; // No Skill
                         cmbLuck.SelectedIndex = 0; // No Luck
                         cmbOption.SelectedIndex = 0; // No Option
                         cmbExc.SelectedIndex = 0; // No exc option
                         cmbSetItem.SelectedIndex = 0; // No Ancient
                         cmbSocketCount.SelectedIndex = 0; // No Sockets
                         cmbElementalItem.SelectedIndex = 0; // No Element
                         
                         // Jewels often use Durability, but it's always included
                         break;
                        
                    case 16: // Muun/Pet items
                        // Muun items typically have no special properties
                        cmbSkill.SelectedIndex = 0; // No Skill
                        cmbLuck.SelectedIndex = 0; // No Luck
                        cmbOption.SelectedIndex = 0; // No Option
                        cmbExc.SelectedIndex = 0; // No exc option
                        cmbSetItem.SelectedIndex = 0; // No Ancient
                        cmbSocketCount.SelectedIndex = 0; // No Sockets
                        cmbElementalItem.SelectedIndex = 0; // No Element
                        break;
                        
                    default:
                        // For other categories, use moderate defaults
                        cmbSkill.SelectedIndex = 0; // No Skill
                        cmbLuck.SelectedIndex = 1; // With Luck
                        cmbOption.SelectedIndex = 1; // +4 Option
                        cmbExc.SelectedIndex = 0; // No exc option
                        cmbSetItem.SelectedIndex = 0; // No Ancient
                        cmbSocketCount.SelectedIndex = 0; // No Sockets
                        cmbElementalItem.SelectedIndex = 0; // No Element
                        break;
                }
                
                // Special handling for Evolution Stone items
                if ((_selectedItem.Category == 16 && _selectedItem.Index == 211) || 
                    (_selectedItem.Category == 20 && _selectedItem.Index == 510))
                {
                    // Evolution Stone items should have default variant selected
                    if (cmbEvolutionStoneVariant.Items.Count > 1)
                        cmbEvolutionStoneVariant.SelectedIndex = 1; // Select first variant instead of "None"
                }
                
                _logger.LogInformation("Set intelligent defaults for item {Name} (Cat:{Category}, Index:{Index})", 
                    _selectedItem.Name, _selectedItem.Category, _selectedItem.Index);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting intelligent defaults for item {Name}", _selectedItem.Name);
            }
        }

                private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (lstItems.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one item first.", "No Items Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Results = new List<DropItemConfiguration>();
                
                foreach (var selectedItem in lstItems.SelectedItems)
                {
                    var itemIndex = lstItems.Items.IndexOf(selectedItem);
                    if (itemIndex >= 0 && itemIndex < _filteredItems.Count)
                    {
                        var item = _filteredItems[itemIndex];
                        var config = CreateItemConfiguration(item);
                        Results.Add(config);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item configurations");
                MessageBox.Show($"Error creating item configurations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private DropItemConfiguration CreateItemConfiguration(Item item)
        {
            // Get Evolution Stone values if an Evolution Stone item is selected
            string muunEvolutionItemCat = "";
            string muunEvolutionItemIndex = "";
            
            if (IsEvolutionStoneItem(item) && 
                cmbEvolutionStoneVariant.SelectedItem is ComboBoxItem selectedVariant)
            {
                string variantTag = selectedVariant.Tag?.ToString() ?? "";
                if (!string.IsNullOrEmpty(variantTag) && variantTag.Contains(";"))
                {
                    string[] parts = variantTag.Split(';');
                    if (parts.Length == 2)
                    {
                        muunEvolutionItemCat = parts[0];
                        muunEvolutionItemIndex = parts[1];
                    }
                }
            }
            
            // Most properties are always included - no checkboxes required
            int durability = int.Parse(txtDurability.Text);
            string optSlotInfo = txtOptSlotInfo.Text;
            int elementalItem = int.Parse((cmbElementalItem.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0");
            int duration = int.Parse(txtDuration.Text);
            
            // Determine ErrtelRank based on checkbox and item type
            int errtelRank = 0; // Default to 0 (not included)
            if (chkEnableErrtelRank.IsChecked == true)
            {
                // Check if this is an Errtel item (Category 12, Index 200-299)
                if (item.Category == 12 && item.Index >= 200 && item.Index <= 299)
                {
                    // This is an Errtel item and checkbox is checked
                    errtelRank = int.Parse((cmbErrtelRank.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0");
                }
                else if (item.Category == 13 && item.Index >= 200 && item.Index <= 299)
                {
                    // This is a Category 13 Errtel item and checkbox is checked
                    errtelRank = int.Parse((cmbErrtelRank.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0");
                }
                // If it's not an Errtel item, errtelRank remains 0 (not included)
            }
            
            return new DropItemConfiguration
            {
                Cat = item.Category,
                Index = item.Index,
                ItemMinLevel = int.Parse(txtItemMinLevel.Text),
                ItemMaxLevel = int.Parse(txtItemMaxLevel.Text),
                Durability = durability,
                Skill = int.Parse((cmbSkill.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0"),
                Luck = int.Parse((cmbLuck.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0"),
                Option = int.Parse((cmbOption.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0"),
                Exc = (cmbExc.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "-1",
                SetItem = int.Parse((cmbSetItem.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0"),
                ErrtelRank = errtelRank,
                SocketCount = int.Parse((cmbSocketCount.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "0"),
                OptSlotInfo = optSlotInfo,
                ElementalItem = elementalItem,
                MuunEvolutionItemCat = muunEvolutionItemCat,
                MuunEvolutionItemIndex = muunEvolutionItemIndex,
                Duration = duration,
                Rate = int.Parse(txtRate.Text)
            };
        }
        
        private bool IsEvolutionStoneItem(Item item)
        {
            return (item.Category == 16 && item.Index == 211) || 
                   (item.Category == 20 && item.Index == 510);
        }

        private void btnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            lstItems.SelectedItems.Clear();
        }
        
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
