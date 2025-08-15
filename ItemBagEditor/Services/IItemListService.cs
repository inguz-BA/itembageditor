/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team Application
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using ItemBagEditor.Models;

namespace ItemBagEditor.Services
{
    public interface IItemListService
    {
        Task<bool> LoadEmbeddedItemListAsync();
        Task<bool> LoadItemListAsync(string filePath);
        List<Item> GetItemsByCategory(int category);
        List<Item> SearchItems(string searchTerm);
        List<int> GetCategories();
        Item? GetItem(int category, int index);
    }
}
