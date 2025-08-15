/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System;
using ItemBagEditor.Models;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ItemBagEditor.Services
{
    public class ItemListService : IItemListService
    {
        private readonly ILogger<ItemListService> _logger;
        private List<Item> _allItems = new();
        private Dictionary<int, List<Item>> _itemsByCategory = new();

        public ItemListService(ILogger<ItemListService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> LoadEmbeddedItemListAsync()
        {
            try
            {
                _logger.LogInformation("Loading ItemList from local file or embedded resources");
                
                // First try to load from local file in the same directory as the executable
                var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ItemList.xml");
                if (File.Exists(localPath))
                {
                    _logger.LogInformation("Found local ItemList.xml, loading from file: {Path}", localPath);
                    var localXml = await File.ReadAllTextAsync(localPath);
                    _logger.LogInformation("Local ItemList.xml loaded successfully, size: {Size} characters", localXml.Length);
                    return await ParseItemListXml(localXml);
                }
                
                // Fallback to embedded resource
                _logger.LogInformation("Local ItemList.xml not found, trying embedded resource");
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "ItemList.xml";
                
                // Debug: List all available resources
                var allResources = assembly.GetManifestResourceNames();
                _logger.LogInformation("Available resources: {Resources}", string.Join(", ", allResources));
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _logger.LogError("Embedded ItemList resource not found: {ResourceName}", resourceName);
                    return false;
                }

                _logger.LogDebug("Reading embedded ItemList content");
                using var reader = new StreamReader(stream);
                var embeddedXml = await reader.ReadToEndAsync();
                _logger.LogDebug("Embedded ItemList read successfully, size: {Size} characters", embeddedXml.Length);
                
                return await ParseItemListXml(embeddedXml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ItemList");
                return false;
            }
        }

        public async Task<bool> LoadItemListAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Loading ItemList from {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogError("ItemList file does not exist: {FilePath}", filePath);
                    return false;
                }

                _logger.LogDebug("Reading ItemList file content");
                var xml = await File.ReadAllTextAsync(filePath);
                _logger.LogDebug("ItemList file read successfully, size: {Size} characters", xml.Length);
                
                return await ParseItemListXml(xml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ItemList from {FilePath}", filePath);
                return false;
            }
        }

        private async Task<bool> ParseItemListXml(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var itemList = doc.Element("ItemList");

                if (itemList == null)
                {
                    _logger.LogError("ItemList element not found in XML");
                    return false;
                }

                _logger.LogDebug("Clearing existing item collections");
                _allItems.Clear();
                _itemsByCategory.Clear();

                var categories = itemList.Elements("Category");
                var totalItems = 0;

                foreach (var categoryElement in categories)
                {
                    var categoryIndex = int.Parse(categoryElement.Attribute("Index")?.Value ?? "0");
                    var categoryName = categoryElement.Attribute("Name")?.Value ?? $"Category {categoryIndex}";
                    
                    _logger.LogDebug("Processing category {CategoryIndex}: {CategoryName}", categoryIndex, categoryName);

                    var items = categoryElement.Elements("Item");
                    foreach (var itemElement in items)
                    {
                        try
                        {
                            var item = Item.FromXml(itemElement);
                            
                            // Set the category from the parent Category element
                            item.Category = categoryIndex;
                            item.Cat = item.Slot; // Keep existing logic for backward compatibility
                            
                            _allItems.Add(item);

                            if (!_itemsByCategory.ContainsKey(categoryIndex))
                                _itemsByCategory[categoryIndex] = new List<Item>();
                            
                            _itemsByCategory[categoryIndex].Add(item);
                            totalItems++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse item element in category {CategoryIndex}: {Element}", categoryIndex, itemElement.ToString());
                        }
                    }
                }

                _logger.LogInformation("ItemList loaded successfully. Total items: {TotalItems}, Categories: {CategoryCount}", 
                    totalItems, _itemsByCategory.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing ItemList XML");
                return false;
            }
        }

        public List<Item> GetItemsByCategory(int category)
        {
            _logger.LogDebug("Getting items for category {Category}", category);
            var items = _itemsByCategory.ContainsKey(category) ? _itemsByCategory[category] : new List<Item>();
            _logger.LogDebug("Found {ItemCount} items for category {Category}", items.Count, category);
            return items;
        }

        public List<Item> SearchItems(string searchTerm)
        {
            _logger.LogDebug("Searching items with term: {SearchTerm}", searchTerm);
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                _logger.LogDebug("Empty search term, returning all items");
                return _allItems;
            }

            var results = _allItems.Where(item => 
                item.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.Index.ToString().Contains(searchTerm)
            ).ToList();
            
            _logger.LogDebug("Search completed, found {ResultCount} items", results.Count);
            return results;
        }

        public List<int> GetCategories()
        {
            var categories = _itemsByCategory.Keys.OrderBy(x => x).ToList();
            _logger.LogDebug("Retrieved {CategoryCount} categories", categories.Count);
            return categories;
        }

        public Item? GetItem(int category, int index)
        {
            _logger.LogDebug("Getting item for category {Category}, index {Index}", category, index);
            
            if (!_itemsByCategory.ContainsKey(category))
            {
                _logger.LogDebug("Category {Category} not found", category);
                return null;
            }
            
            var item = _itemsByCategory[category].FirstOrDefault(x => x.Index == index);
            if (item == null)
            {
                _logger.LogDebug("Item with index {Index} not found in category {Category}", index, category);
            }
            else
            {
                _logger.LogDebug("Found item: {ItemName} in category {Category}", item.Name, category);
            }
            
            return item;
        }
    }
}
