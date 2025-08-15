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
using System.Xml;
using System.Collections.ObjectModel;
using ItemBagEditor.Models;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using System;

namespace ItemBagEditor.Services
{
    public class ItemBagService : IItemBagService
    {
        private readonly ILogger<ItemBagService> _logger;

        public ItemBagService(ILogger<ItemBagService> logger)
        {
            _logger = logger;
        }
        public async Task<ItemBag?> LoadItemBagAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Loading ItemBag from {FilePath}", filePath);
                
                if (!File.Exists(filePath))
                {
                    _logger.LogError("ItemBag file does not exist: {FilePath}", filePath);
                    return null;
                }

                _logger.LogDebug("Reading ItemBag file content");
                var xml = await File.ReadAllTextAsync(filePath);
                _logger.LogDebug("ItemBag file read successfully, size: {Size} characters", xml.Length);
                
                var doc = XDocument.Parse(xml);
                var itemBagElement = doc.Element("ItemBag");

                if (itemBagElement == null)
                {
                    _logger.LogError("ItemBag element not found in XML");
                    return null;
                }

                _logger.LogDebug("Parsing ItemBag XML element");
                var itemBag = ItemBag.FromXml(itemBagElement);
                _logger.LogInformation("ItemBag loaded successfully: {BagName}", itemBag?.Config?.Name ?? "Unknown");
                
                return itemBag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ItemBag from {FilePath}", filePath);
                return null;
            }
        }

        public async Task<bool> SaveItemBagAsync(string filePath, ItemBag itemBag)
        {
            try
            {
                _logger.LogInformation("Saving ItemBag to {FilePath}", filePath);
                _logger.LogDebug("ItemBag name: {BagName}", itemBag.Config?.Name ?? "Unknown");
                
                var doc = new XDocument();
                var root = itemBag.ToXml();
                doc.Add(root);

                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    NewLineChars = "\r\n"
                };

                _logger.LogDebug("Writing ItemBag to file with XML settings");
                using var writer = XmlWriter.Create(filePath, settings);
                doc.Save(writer);
                
                _logger.LogInformation("ItemBag saved successfully to {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ItemBag to {FilePath}", filePath);
                return false;
            }
        }

        public async Task<List<string>> GetItemBagFilesAsync(string folderPath)
        {
            try
            {
                _logger.LogDebug("Getting ItemBag files from folder: {FolderPath}", folderPath);
                
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogWarning("Folder does not exist: {FolderPath}", folderPath);
                    return new List<string>();
                }

                var files = Directory.GetFiles(folderPath, "*.xml")
                    .Where(f => !f.EndsWith("launcher-config.json"))
                    .ToList();

                _logger.LogDebug("Found {FileCount} ItemBag files in folder", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ItemBag files from folder: {FolderPath}", folderPath);
                return new List<string>();
            }
        }

        public ItemBag CreateNewItemBag()
        {
            _logger.LogDebug("Creating new ItemBag");
            var itemBag = new ItemBag
            {
                Config = new BagConfig
                {
                    Name = "New ItemBag",
                    ItemRate = 10000,
                    SetItemRate = 0,
                    SetItemCount = 1,
                    MasterySetItemInclude = 0,
                    MoneyDrop = 0,
                    IsPentagramForBeginnersDrop = 0,
                    PartyDropRate = 0,
                    PartyOneDropOnly = 0,
                    PartyShareType = 0,
                    BagUseEffect = -1,
                    BagUseType = 0,
                    BagUseRate = 10000
                },
                SummonBook = new SummonBook(),
                AddCoin = new AddCoin(),
                Ruud = new Ruud(),
                DropSections = new ObservableCollection<DropSection>
                {
                    new DropSection
                    {
                        UseMode = -1,
                        DisplayName = "Section 1",
                        DropAllows = new ObservableCollection<DropAllow>
                        {
                            new DropAllow
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
                            }
                        }
                    }
                }
            };
            
            _logger.LogDebug("New ItemBag created successfully: {BagName}", itemBag.Config.Name);
            return itemBag;
        }
    }
}
