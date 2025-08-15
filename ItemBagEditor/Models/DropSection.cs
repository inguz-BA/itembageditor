/*
 * Copyright © 2024 Inguz. All rights reserved.
 * 
 * ItemBag Editor - Advanced ItemBag Editor for DVT-Team EMU
 * This software is proprietary and confidential.
 * Unauthorized copying, distribution, or use is strictly prohibited.
 */

using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace ItemBagEditor.Models
{
    public class DropSection
    {
        public int UseMode { get; set; } = -1;
        public string DisplayName { get; set; } = "Section 1";
        public ObservableCollection<DropAllow> DropAllows { get; set; } = new();

        public static DropSection FromXml(XElement element)
        {
            var section = new DropSection
            {
                UseMode = int.Parse(element.Attribute("UseMode")?.Value ?? "-1"),
                DisplayName = element.Attribute("DisplayName")?.Value ?? "Section 1"
            };

            var dropAllows = element.Elements("DropAllow");
            foreach (var allow in dropAllows)
            {
                section.DropAllows.Add(DropAllow.FromXml(allow));
            }

            return section;
        }

        public XElement ToXml()
        {
            var element = new XElement("DropSection");
            element.Add(new XAttribute("UseMode", UseMode));
            element.Add(new XAttribute("DisplayName", DisplayName));

            foreach (var allow in DropAllows)
            {
                element.Add(allow.ToXml());
            }

            return element;
        }
    }

    public class DropAllow
    {
        public int DW { get; set; } = 1;
        public int DK { get; set; } = 1;
        public int ELF { get; set; } = 1;
        public int MG { get; set; } = 1;
        public int DL { get; set; } = 1;
        public int SU { get; set; } = 1;
        public int RF { get; set; } = 1;
        public int GL { get; set; } = 1;
        public int RW { get; set; } = 1;
        public int SLA { get; set; } = 1;
        public int GC { get; set; } = 1;
        public int LW { get; set; } = 1;
        public int LM { get; set; } = 1;
        public int IK { get; set; } = 1;
        public int AC { get; set; } = 1;
        public int PlayerMinLevel { get; set; } = 1;
        public string PlayerMaxLevel { get; set; } = "MAX";
        public int PlayerMinReset { get; set; } = 0;
        public string PlayerMaxReset { get; set; } = "MAX";
        public int MapNumber { get; set; } = -1;
        public ObservableCollection<Drop> Drops { get; set; } = new();

        public static DropAllow FromXml(XElement element)
        {
            var allow = new DropAllow
            {
                DW = int.Parse(element.Attribute("DW")?.Value ?? "1"),
                DK = int.Parse(element.Attribute("DK")?.Value ?? "1"),
                ELF = int.Parse(element.Attribute("ELF")?.Value ?? "1"),
                MG = int.Parse(element.Attribute("MG")?.Value ?? "1"),
                DL = int.Parse(element.Attribute("DL")?.Value ?? "1"),
                SU = int.Parse(element.Attribute("SU")?.Value ?? "1"),
                RF = int.Parse(element.Attribute("RF")?.Value ?? "1"),
                GL = int.Parse(element.Attribute("GL")?.Value ?? "1"),
                RW = int.Parse(element.Attribute("RW")?.Value ?? "1"),
                SLA = int.Parse(element.Attribute("SLA")?.Value ?? "1"),
                GC = int.Parse(element.Attribute("GC")?.Value ?? "1"),
                LW = int.Parse(element.Attribute("LW")?.Value ?? "1"),
                LM = int.Parse(element.Attribute("LM")?.Value ?? "1"),
                IK = int.Parse(element.Attribute("IK")?.Value ?? "1"),
                AC = int.Parse(element.Attribute("AC")?.Value ?? "1"),
                PlayerMinLevel = int.Parse(element.Attribute("PlayerMinLevel")?.Value ?? "1"),
                PlayerMaxLevel = element.Attribute("PlayerMaxLevel")?.Value ?? "MAX",
                PlayerMinReset = int.Parse(element.Attribute("PlayerMinReset")?.Value ?? "0"),
                PlayerMaxReset = element.Attribute("PlayerMaxReset")?.Value ?? "MAX",
                MapNumber = int.Parse(element.Attribute("MapNumber")?.Value ?? "-1")
            };

            var drops = element.Elements("Drop");
            foreach (var drop in drops)
            {
                allow.Drops.Add(Drop.FromXml(drop));
            }

            return allow;
        }

        public XElement ToXml()
        {
            var element = new XElement("DropAllow");
            element.Add(new XAttribute("DW", DW));
            element.Add(new XAttribute("DK", DK));
            element.Add(new XAttribute("ELF", ELF));
            element.Add(new XAttribute("MG", MG));
            element.Add(new XAttribute("DL", DL));
            element.Add(new XAttribute("SU", SU));
            element.Add(new XAttribute("RF", RF));
            element.Add(new XAttribute("GL", GL));
            element.Add(new XAttribute("RW", RW));
            element.Add(new XAttribute("SLA", SLA));
            element.Add(new XAttribute("GC", GC));
            element.Add(new XAttribute("LW", LW));
            element.Add(new XAttribute("LM", LM));
            element.Add(new XAttribute("IK", IK));
            element.Add(new XAttribute("AC", AC));
            element.Add(new XAttribute("PlayerMinLevel", PlayerMinLevel));
            element.Add(new XAttribute("PlayerMaxLevel", PlayerMaxLevel));
            element.Add(new XAttribute("PlayerMinReset", PlayerMinReset));
            element.Add(new XAttribute("PlayerMaxReset", PlayerMaxReset));
            element.Add(new XAttribute("MapNumber", MapNumber));

            foreach (var drop in Drops)
            {
                element.Add(drop.ToXml());
            }

            return element;
        }
    }

    public class Drop
    {
        public int Rate { get; set; } = 10000;
        public int Type { get; set; } = 0;
        public int Count { get; set; } = 1;
        public ObservableCollection<DropItem> Items { get; set; } = new();

        public static Drop FromXml(XElement element)
        {
            var drop = new Drop
            {
                Rate = int.Parse(element.Attribute("Rate")?.Value ?? "10000"),
                Type = int.Parse(element.Attribute("Type")?.Value ?? "0"),
                Count = int.Parse(element.Attribute("Count")?.Value ?? "1")
            };

            var items = element.Elements("Item");
            foreach (var item in items)
            {
                drop.Items.Add(DropItem.FromXml(item));
            }

            return drop;
        }

        public XElement ToXml()
        {
            var element = new XElement("Drop");
            element.Add(new XAttribute("Rate", Rate));
            element.Add(new XAttribute("Type", Type));
            element.Add(new XAttribute("Count", Count));

            foreach (var item in Items)
            {
                element.Add(item.ToXml());
            }

            return element;
        }
    }

    public class DropItem
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
        public int KindA { get; set; } = 0;
        public int Duration { get; set; } = 0;
        public string Name { get; set; } = "";
        public int Rate { get; set; } = 0;

        public static DropItem FromXml(XElement element)
        {
            return new DropItem
            {
                Cat = int.Parse(element.Attribute("Cat")?.Value ?? "0"),
                Index = int.Parse(element.Attribute("Index")?.Value ?? "0"),
                ItemMinLevel = int.Parse(element.Attribute("ItemMinLevel")?.Value ?? "0"),
                ItemMaxLevel = int.Parse(element.Attribute("ItemMaxLevel")?.Value ?? "0"),
                Durability = int.Parse(element.Attribute("Durability")?.Value ?? "0"),
                Skill = int.Parse(element.Attribute("Skill")?.Value ?? "0"),
                Luck = int.Parse(element.Attribute("Luck")?.Value ?? "0"),
                Option = int.Parse(element.Attribute("Option")?.Value ?? "0"),
                Exc = element.Attribute("Exc")?.Value ?? "-1",
                SetItem = int.Parse(element.Attribute("SetItem")?.Value ?? "0"),
                ErrtelRank = int.Parse(element.Attribute("ErrtelRank")?.Value ?? "0"),
                SocketCount = int.Parse(element.Attribute("SocketCount")?.Value ?? "0"),
                OptSlotInfo = element.Attribute("OptSlotInfo")?.Value ?? "",
                ElementalItem = int.Parse(element.Attribute("ElementalItem")?.Value ?? "0"),
                MuunEvolutionItemCat = element.Attribute("MuunEvolutionItemCat")?.Value ?? "",
                MuunEvolutionItemIndex = element.Attribute("MuunEvolutionItemIndex")?.Value ?? "",
                KindA = int.Parse(element.Attribute("KindA")?.Value ?? "0"),
                Duration = int.Parse(element.Attribute("Duration")?.Value ?? "0"),
                Name = element.Attribute("Name")?.Value ?? "",
                Rate = int.Parse(element.Attribute("Rate")?.Value ?? "0")
            };
        }

        private bool IsErrtelItem(int category, int index)
        {
            // ErrtelRank only applies to Errtel items (Category 12/13, Index 200-299)
            return (category == 12 || category == 13) && index >= 200 && index <= 299;
        }

        public XElement ToXml()
        {
            var element = new XElement("Item");
            element.Add(new XAttribute("Cat", Cat));
            element.Add(new XAttribute("Index", Index));
            
            // Always include these mandatory properties regardless of their values
            element.Add(new XAttribute("ItemMinLevel", ItemMinLevel));
            element.Add(new XAttribute("ItemMaxLevel", ItemMaxLevel));
            element.Add(new XAttribute("Skill", Skill));
            element.Add(new XAttribute("Luck", Luck));
            element.Add(new XAttribute("Option", Option));
            element.Add(new XAttribute("Exc", Exc));
            element.Add(new XAttribute("SetItem", SetItem));
            element.Add(new XAttribute("SocketCount", SocketCount));
            element.Add(new XAttribute("ElementalItem", ElementalItem));
            
            // Only add Durability if it's not 0 (this is optional)
            if (Durability > 0)
                element.Add(new XAttribute("Durability", Durability));
            
            // Only add ErrtelRank if it's greater than 0 AND the item is actually an Errtel item
            // ErrtelRank only applies to Errtel items (Category 12/13, Index 200-299)
            if (ErrtelRank > 0 && IsErrtelItem(Cat, Index))
                element.Add(new XAttribute("ErrtelRank", ErrtelRank));
            
            // Only add OptSlotInfo if it has meaningful content
            if (!string.IsNullOrEmpty(OptSlotInfo))
                element.Add(new XAttribute("OptSlotInfo", OptSlotInfo));
            
            // Only add MuunEvolutionItemCat and MuunEvolutionItemIndex if they have meaningful values
            // These properties specify which Evolution Stone variant to create
            if (!string.IsNullOrEmpty(MuunEvolutionItemCat) && MuunEvolutionItemCat != "0")
                element.Add(new XAttribute("MuunEvolutionItemCat", MuunEvolutionItemCat));
            if (!string.IsNullOrEmpty(MuunEvolutionItemIndex) && MuunEvolutionItemIndex != "0")
                element.Add(new XAttribute("MuunEvolutionItemIndex", MuunEvolutionItemIndex));
            
            // Only add Duration if the user has set a value (not the default 0)
            if (Duration > 0)
                element.Add(new XAttribute("Duration", Duration));
            
            // Only add Rate if it's not 0
            if (Rate > 0)
                element.Add(new XAttribute("Rate", Rate));
            
            // Always add Name at the end
            element.Add(new XAttribute("Name", Name));
            
            return element;
        }
    }
}
