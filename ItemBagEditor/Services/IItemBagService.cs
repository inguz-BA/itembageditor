/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using ItemBagEditor.Models;

namespace ItemBagEditor.Services
{
    public interface IItemBagService
    {
        Task<ItemBag?> LoadItemBagAsync(string filePath);
        Task<bool> SaveItemBagAsync(string filePath, ItemBag itemBag);
        Task<List<string>> GetItemBagFilesAsync(string folderPath);
        ItemBag CreateNewItemBag();
    }
}
