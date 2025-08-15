/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using System;

namespace ItemBagEditor.Models
{
    public class Item
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Slot { get; set; }
        public int SkillIndex { get; set; }
        public int TwoHand { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Serial { get; set; }
        public int Option { get; set; }
        public int Drop { get; set; }
        public int DropLevel { get; set; }
        public int ReqLevel { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int AttackSpeed { get; set; }
        public int Durability { get; set; }
        public int Cat { get; set; }
        public int Category { get; set; } // Category from ItemList.xml

        public static Item FromXml(XElement element)
        {
            try
            {
                var item = new Item
                {
                    Index = int.Parse(element.Attribute("Index")?.Value ?? "0"),
                    Name = element.Attribute("Name")?.Value ?? string.Empty,
                    Slot = int.Parse(element.Attribute("Slot")?.Value ?? "0"),
                    SkillIndex = int.Parse(element.Attribute("SkillIndex")?.Value ?? "0"),
                    TwoHand = int.Parse(element.Attribute("TwoHand")?.Value ?? "0"),
                    Width = int.Parse(element.Attribute("Width")?.Value ?? "0"),
                    Height = int.Parse(element.Attribute("Height")?.Value ?? "0"),
                    Serial = int.Parse(element.Attribute("Serial")?.Value ?? "0"),
                    Option = int.Parse(element.Attribute("Option")?.Value ?? "0"),
                    Drop = int.Parse(element.Attribute("Drop")?.Value ?? "0"),
                    DropLevel = int.Parse(element.Attribute("DropLevel")?.Value ?? "0"),
                    ReqLevel = int.Parse(element.Attribute("ReqLevel")?.Value ?? "0"),
                    MinDamage = int.Parse(element.Attribute("MinDamage")?.Value ?? "0"),
                    MaxDamage = int.Parse(element.Attribute("MaxDamage")?.Value ?? "0"),
                    AttackSpeed = int.Parse(element.Attribute("AttackSpeed")?.Value ?? "0"),
                    Durability = int.Parse(element.Attribute("Durability")?.Value ?? "0")
                };
                
                return item;
            }
            catch (Exception ex)
            {
                // Note: We can't use ILogger here as this is a static method
                // The calling service should handle logging
                throw new InvalidOperationException($"Failed to parse Item XML element: {element}", ex);
            }
        }

        public override string ToString()
        {
            return $"{Index}: {Name} (Level {ReqLevel})";
        }
    }
}
