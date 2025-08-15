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
using System.Windows.Media.Imaging;
using ItemBagEditor.Models;

namespace ItemBagEditor
{
    public partial class EditItemDialog : Window
    {
        private readonly DropItem _item;
        private string _imagesPath = "";
        private readonly string _sourceImagesPath = @"C:\Users\ennzo\OneDrive\Desktop\Tools\IBC\images\items";

        public EditItemDialog(DropItem item)
        {
            InitializeComponent();
            _item = item;
            
            // Set up images path and copy images if needed
            SetupImagesPath();
            
            LoadItemProperties();
            LoadItemImage();
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
            }
            catch (Exception ex)
            {
                // Log error if logger is available, otherwise just continue
                System.Diagnostics.Debug.WriteLine($"Error copying images: {ex.Message}");
            }
        }

        private void CopyImagesFromSource()
        {
            try
            {
                // Copy all files from source to destination
                foreach (string filePath in Directory.GetFiles(_sourceImagesPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destPath = Path.Combine(_imagesPath, fileName);
                    
                    // Only copy if destination doesn't exist or source is newer
                    if (!File.Exists(destPath) || File.GetLastWriteTime(filePath) > File.GetLastWriteTime(destPath))
                    {
                        File.Copy(filePath, destPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error if logger is available, otherwise just continue
                System.Diagnostics.Debug.WriteLine($"Error copying images: {ex.Message}");
            }
        }

        private void LoadItemProperties()
        {
            try
            {
                // Basic Information
                txtCategory.Text = _item.Cat.ToString();
                txtIndex.Text = _item.Index.ToString();
                txtName.Text = _item.Name ?? "Unknown";
                txtMinLevel.Text = _item.ItemMinLevel.ToString();
                txtMaxLevel.Text = _item.ItemMaxLevel.ToString();

                // Enhancement Properties
                SetComboBoxSelection(cmbSkill, _item.Skill.ToString());
                SetComboBoxSelection(cmbLuck, _item.Luck.ToString());
                SetComboBoxSelection(cmbOption, _item.Option.ToString());
                txtDurability.Text = _item.Durability.ToString();
                SetComboBoxSelection(cmbExc, _item.Exc ?? "-1");

                // Advanced Properties
                SetComboBoxSelection(cmbSetItem, _item.SetItem.ToString());
                SetComboBoxSelection(cmbSocketCount, _item.SocketCount.ToString());
                SetComboBoxSelection(cmbElementalItem, _item.ElementalItem.ToString());
                txtOptSlotInfo.Text = _item.OptSlotInfo ?? "";
                txtDuration.Text = _item.Duration.ToString();
                txtRate.Text = _item.Rate.ToString();

                // Errtel Rank Options
                SetupErrtelRankOptions();

                // Update item info display
                txtItemInfo.Text = $"Category: {_item.Cat}, Index: {_item.Index}";
                txtItemName.Text = _item.Name ?? "Unknown Item";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading item properties: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadItemImage()
        {
            try
            {
                // Check if images directory exists
                if (!Directory.Exists(_imagesPath))
                {
                    imgItemPreview.Source = null;
                    return;
                }

                // Construct the image path based on category and index
                string imageFileName = $"{_item.Cat}-{_item.Index}.gif";
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
                // Optionally log the error (you can add logging here if needed)
                System.Diagnostics.Debug.WriteLine($"Error loading item image: {ex.Message}");
            }
        }

        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                var item = comboBox.Items[i] as System.Windows.Controls.ComboBoxItem;
                if (item?.Tag?.ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SetupErrtelRankOptions()
        {
            // Check if this is an Errtel item (Category 12 or 13, Index 200-299 are typically Errtels)
            bool isErrtelItem = ((_item.Cat == 12 || _item.Cat == 13) && 
                                _item.Index >= 200 && _item.Index <= 299);
            
            if (isErrtelItem)
            {
                // Show Errtel Rank options for non-Beginner Errtels
                chkEnableErrtelRank.IsEnabled = true;
                chkEnableErrtelRank.ToolTip = "Enable to set Errtel Rank for this item. Only applies to Errtel items (Category 12/13, Index 200-299).";
                cmbErrtelRank.ToolTip = "Defines rank to drop Errtel item with, 1-5. Does not apply to Errtels of Beginner. Default is 1.";
                
                // Set the checkbox state based on current ErrtelRank value
                chkEnableErrtelRank.IsChecked = _item.ErrtelRank > 0;
                cmbErrtelRank.IsEnabled = _item.ErrtelRank > 0;
                
                // Set the combo box selection
                if (_item.ErrtelRank > 0)
                {
                    SetComboBoxSelection(cmbErrtelRank, _item.ErrtelRank.ToString());
                }
                else
                {
                    cmbErrtelRank.SelectedIndex = 0; // Default to "0 - No Errtel Rank"
                }
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
            
            // Wire up the checkbox event
            chkEnableErrtelRank.Checked += (s, e) => cmbErrtelRank.IsEnabled = true;
            chkEnableErrtelRank.Unchecked += (s, e) => cmbErrtelRank.IsEnabled = false;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate and update item properties
                if (int.TryParse(txtMinLevel.Text, out int minLevel))
                    _item.ItemMinLevel = minLevel;
                
                if (int.TryParse(txtMaxLevel.Text, out int maxLevel))
                    _item.ItemMaxLevel = maxLevel;

                if (int.TryParse(txtDurability.Text, out int durability))
                    _item.Durability = durability;

                if (int.TryParse(txtDuration.Text, out int duration))
                    _item.Duration = duration;

                if (int.TryParse(txtRate.Text, out int rate))
                    _item.Rate = rate;

                // Update combo box values
                if (cmbSkill.SelectedItem is System.Windows.Controls.ComboBoxItem skillItem && skillItem.Tag?.ToString() is string skillValue)
                    _item.Skill = int.Parse(skillValue);

                if (cmbLuck.SelectedItem is System.Windows.Controls.ComboBoxItem luckItem && luckItem.Tag?.ToString() is string luckValue)
                    _item.Luck = int.Parse(luckValue);

                if (cmbOption.SelectedItem is System.Windows.Controls.ComboBoxItem optionItem && optionItem.Tag?.ToString() is string optionValue)
                    _item.Option = int.Parse(optionValue);

                if (cmbExc.SelectedItem is System.Windows.Controls.ComboBoxItem excItem && excItem.Tag?.ToString() is string excValue)
                    _item.Exc = excValue;

                if (cmbSetItem.SelectedItem is System.Windows.Controls.ComboBoxItem setItem && setItem.Tag?.ToString() is string setItemValue)
                    _item.SetItem = int.Parse(setItemValue);

                if (cmbSocketCount.SelectedItem is System.Windows.Controls.ComboBoxItem socketItem && socketItem.Tag?.ToString() is string socketValue)
                    _item.SocketCount = int.Parse(socketValue);

                if (cmbElementalItem.SelectedItem is System.Windows.Controls.ComboBoxItem elementItem && elementItem.Tag?.ToString() is string elementValue)
                    _item.ElementalItem = int.Parse(elementValue);

                // Update ErrtelRank based on checkbox and item type
                if (chkEnableErrtelRank.IsChecked == true && chkEnableErrtelRank.IsEnabled)
                {
                    if (cmbErrtelRank.SelectedItem is System.Windows.Controls.ComboBoxItem errtelItem && errtelItem.Tag?.ToString() is string errtelValue)
                    {
                        int errtelRank = int.Parse(errtelValue);
                        if (errtelRank > 0)
                        {
                            _item.ErrtelRank = errtelRank;
                        }
                        else
                        {
                            _item.ErrtelRank = 0; // Don't include in XML
                        }
                    }
                }
                else
                {
                    _item.ErrtelRank = 0; // Don't include in XML
                }

                // Update text values
                _item.OptSlotInfo = txtOptSlotInfo.Text;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving item properties: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error saving item properties: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
