/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ItemBagEditor.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ItemBagEditor.Services
{
    public interface IItemBagItemService
    {
        Task<bool> AddItemToDropSectionAsync(ItemBag itemBag, int sectionIndex, int dropIndex, Item item, DropItemConfiguration config);
        Task<bool> RemoveItemFromDropSectionAsync(ItemBag itemBag, int sectionIndex, int dropIndex, int itemIndex);
        Task<List<DropItem>> GetItemsInDropSectionAsync(ItemBag itemBag, int sectionIndex, int dropIndex);
        Task<bool> UpdateItemConfigurationAsync(ItemBag itemBag, int sectionIndex, int dropIndex, int itemIndex, DropItemConfiguration config);
    }

    public class ItemBagItemService : IItemBagItemService
    {
        private readonly ILogger<ItemBagItemService> _logger;

        public ItemBagItemService(ILogger<ItemBagItemService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> AddItemToDropSectionAsync(ItemBag itemBag, int sectionIndex, int dropIndex, Item item, DropItemConfiguration config)
        {
            try
            {
                _logger.LogInformation("Adding item {ItemName} (Cat:{Category}, Index:{Index}) to ItemBag {BagName}", 
                    item.Name, item.Category, item.Index, itemBag.Config.Name);

                if (itemBag.DropSections == null || sectionIndex >= itemBag.DropSections.Count)
                {
                    _logger.LogError("Invalid section index: {SectionIndex}", sectionIndex);
                    return false;
                }

                var section = itemBag.DropSections[sectionIndex];
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    _logger.LogWarning("No DropAllow elements found in section {SectionIndex}", sectionIndex);
                    return false;
                }

                // For now, we'll add to the first DropAllow element
                var dropAllow = section.DropAllows[0];
                if (dropAllow.Drops == null || dropIndex >= dropAllow.Drops.Count)
                {
                    _logger.LogError("Invalid drop index: {DropIndex}", dropIndex);
                    return false;
                }

                var drop = dropAllow.Drops[dropIndex];
                
                var dropItem = new DropItem
                {
                    Cat = item.Category,
                    Index = item.Index,
                    ItemMinLevel = config.ItemMinLevel,
                    ItemMaxLevel = config.ItemMaxLevel,
                    Durability = config.Durability,
                    Skill = config.Skill,
                    Luck = config.Luck,
                    Option = config.Option,
                    Exc = config.Exc,
                    SetItem = config.SetItem,
                    ErrtelRank = config.ErrtelRank,
                    SocketCount = config.SocketCount,
                    OptSlotInfo = config.OptSlotInfo,
                    ElementalItem = config.ElementalItem,
                    MuunEvolutionItemCat = config.MuunEvolutionItemCat,
                    MuunEvolutionItemIndex = config.MuunEvolutionItemIndex,
                    Duration = config.Duration,
                    Rate = config.Rate,
                    Name = item.Name
                };

                drop.Items.Add(dropItem);
                
                _logger.LogInformation("Successfully added item {ItemName} to drop section", item.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to drop section");
                return false;
            }
        }

        public async Task<bool> RemoveItemFromDropSectionAsync(ItemBag itemBag, int sectionIndex, int dropIndex, int itemIndex)
        {
            try
            {
                _logger.LogInformation("Removing item at index {ItemIndex} from drop section {SectionIndex}, drop {DropIndex}", 
                    itemIndex, sectionIndex, dropIndex);

                if (itemBag.DropSections == null || sectionIndex >= itemBag.DropSections.Count)
                {
                    _logger.LogError("Invalid section index: {SectionIndex}", sectionIndex);
                    return false;
                }

                var section = itemBag.DropSections[sectionIndex];
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    _logger.LogWarning("No DropAllow elements found in section {SectionIndex}", sectionIndex);
                    return false;
                }

                var dropAllow = section.DropAllows[0];
                if (dropAllow.Drops == null || dropIndex >= dropAllow.Drops.Count)
                {
                    _logger.LogError("Invalid drop index: {DropIndex}", dropIndex);
                    return false;
                }

                var drop = dropAllow.Drops[dropIndex];
                if (itemIndex >= drop.Items.Count)
                {
                    _logger.LogError("Invalid item index: {ItemIndex}", itemIndex);
                    return false;
                }

                var removedItem = drop.Items[itemIndex];
                drop.Items.RemoveAt(itemIndex);
                
                _logger.LogInformation("Successfully removed item {ItemName} from drop section", removedItem.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from drop section");
                return false;
            }
        }

        public async Task<List<DropItem>> GetItemsInDropSectionAsync(ItemBag itemBag, int sectionIndex, int dropIndex)
        {
            try
            {
                if (itemBag.DropSections == null || sectionIndex >= itemBag.DropSections.Count)
                {
                    _logger.LogWarning("Invalid section index: {SectionIndex}", sectionIndex);
                    return new List<DropItem>();
                }

                var section = itemBag.DropSections[sectionIndex];
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    return new List<DropItem>();
                }

                var dropAllow = section.DropAllows[0];
                if (dropAllow.Drops == null || dropIndex >= dropAllow.Drops.Count)
                {
                    _logger.LogWarning("Invalid drop index: {DropIndex}", dropIndex);
                    return new List<DropItem>();
                }

                var drop = dropAllow.Drops[dropIndex];
                return drop.Items.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting items from drop section");
                return new List<DropItem>();
            }
        }

        public async Task<bool> UpdateItemConfigurationAsync(ItemBag itemBag, int sectionIndex, int dropIndex, int itemIndex, DropItemConfiguration config)
        {
            try
            {
                _logger.LogInformation("Updating item configuration at index {ItemIndex} in drop section {SectionIndex}, drop {DropIndex}", 
                    itemIndex, sectionIndex, dropIndex);

                if (itemBag.DropSections == null || sectionIndex >= itemBag.DropSections.Count)
                {
                    _logger.LogError("Invalid section index: {SectionIndex}", sectionIndex);
                    return false;
                }

                var section = itemBag.DropSections[sectionIndex];
                if (section.DropAllows == null || section.DropAllows.Count == 0)
                {
                    _logger.LogWarning("No DropAllow elements found in section {SectionIndex}", sectionIndex);
                    return false;
                }

                var dropAllow = section.DropAllows[0];
                if (dropAllow.Drops == null || dropIndex >= dropAllow.Drops.Count)
                {
                    _logger.LogError("Invalid drop index: {DropIndex}", dropIndex);
                    return false;
                }

                var drop = dropAllow.Drops[dropIndex];
                if (itemIndex >= drop.Items.Count)
                {
                    _logger.LogError("Invalid item index: {ItemIndex}", itemIndex);
                    return false;
                }

                var item = drop.Items[itemIndex];
                
                // Update the item configuration
                item.ItemMinLevel = config.ItemMinLevel;
                item.ItemMaxLevel = config.ItemMaxLevel;
                item.Durability = config.Durability;
                item.Skill = config.Skill;
                item.Luck = config.Luck;
                item.Option = config.Option;
                item.Exc = config.Exc;
                item.SetItem = config.SetItem;
                item.ErrtelRank = config.ErrtelRank;
                item.SocketCount = config.SocketCount;
                item.OptSlotInfo = config.OptSlotInfo;
                item.ElementalItem = config.ElementalItem;
                item.MuunEvolutionItemCat = config.MuunEvolutionItemCat;
                item.MuunEvolutionItemIndex = config.MuunEvolutionItemIndex;
                item.Duration = config.Duration;
                item.Rate = config.Rate;
                
                _logger.LogInformation("Successfully updated item configuration for {ItemName}", item.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item configuration");
                return false;
            }
        }
    }

    public class DropItemConfiguration
    {
        public int Cat { get; set; } = 0;
        public int Index { get; set; } = 0;
        public int ItemMinLevel { get; set; } = 0;
        public int ItemMaxLevel { get; set; } = 0;
        public int Durability { get; set; } = 0;
        public int Skill { get; set; } = 0;
        public int Luck { get; set; } = 0;
        public int Option { get; set; } = 0;
        public string Exc { get; set; } = "-1";
        public int SetItem { get; set; } = 0;
        public int ErrtelRank { get; set; } = 0;
        public int SocketCount { get; set; } = 0;
        public string OptSlotInfo { get; set; } = "";
        public int ElementalItem { get; set; } = 0;
        public string MuunEvolutionItemCat { get; set; } = "";
        public string MuunEvolutionItemIndex { get; set; } = "";
        public int Duration { get; set; } = 0;
        public int Rate { get; set; } = 0;
    }
}
