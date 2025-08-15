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
    public class ItemBag
    {
        public BagConfig Config { get; set; } = new();
        public SummonBook SummonBook { get; set; } = new();
        public AddCoin AddCoin { get; set; } = new();
        public Ruud Ruud { get; set; } = new();
        public ObservableCollection<DropSection> DropSections { get; set; } = new();

        public static ItemBag FromXml(XElement element)
        {
            var itemBag = new ItemBag();
            
            var configElement = element.Element("BagConfig");
            if (configElement != null)
                itemBag.Config = BagConfig.FromXml(configElement);
            
            var summonBookElement = element.Element("SummonBook");
            if (summonBookElement != null)
                itemBag.SummonBook = SummonBook.FromXml(summonBookElement);
            
            var addCoinElement = element.Element("AddCoin");
            if (addCoinElement != null)
                itemBag.AddCoin = AddCoin.FromXml(addCoinElement);
            
            var ruudElement = element.Element("Ruud");
            if (ruudElement != null)
                itemBag.Ruud = Ruud.FromXml(ruudElement);
            
            var dropSections = element.Elements("DropSection");
            foreach (var section in dropSections)
            {
                itemBag.DropSections.Add(DropSection.FromXml(section));
            }
            
            return itemBag;
        }

        public XElement ToXml()
        {
            var element = new XElement("ItemBag");
            element.Add(Config.ToXml());
            element.Add(SummonBook.ToXml());
            element.Add(AddCoin.ToXml());
            element.Add(Ruud.ToXml());
            
            foreach (var section in DropSections)
            {
                element.Add(section.ToXml());
            }
            
            return element;
        }
    }

    public class BagConfig
    {
        public string Name { get; set; } = string.Empty;
        public int ItemRate { get; set; } = 10000;
        public int SetItemRate { get; set; } = 0;
        public int SetItemCount { get; set; } = 1;
        public int MasterySetItemInclude { get; set; } = 0;
        public int MoneyDrop { get; set; } = 0;
        public int IsPentagramForBeginnersDrop { get; set; } = 0;
        public int PartyDropRate { get; set; } = 0;
        public int PartyOneDropOnly { get; set; } = 0;
        public int PartyShareType { get; set; } = 0;
        public int BagUseEffect { get; set; } = -1;
        public int BagUseType { get; set; } = 0;
        public int BagUseRate { get; set; } = 10000;

        public static BagConfig FromXml(XElement element)
        {
            return new BagConfig
            {
                Name = element.Attribute("Name")?.Value ?? string.Empty,
                ItemRate = int.Parse(element.Attribute("ItemRate")?.Value ?? "10000"),
                SetItemRate = int.Parse(element.Attribute("SetItemRate")?.Value ?? "0"),
                SetItemCount = int.Parse(element.Attribute("SetItemCount")?.Value ?? "1"),
                MasterySetItemInclude = int.Parse(element.Attribute("MasterySetItemInclude")?.Value ?? "0"),
                MoneyDrop = int.Parse(element.Attribute("MoneyDrop")?.Value ?? "0"),
                IsPentagramForBeginnersDrop = int.Parse(element.Attribute("IsPentagramForBeginnersDrop")?.Value ?? "0"),
                PartyDropRate = int.Parse(element.Attribute("PartyDropRate")?.Value ?? "0"),
                PartyOneDropOnly = int.Parse(element.Attribute("PartyOneDropOnly")?.Value ?? "0"),
                PartyShareType = int.Parse(element.Attribute("PartyShareType")?.Value ?? "0"),
                BagUseEffect = int.Parse(element.Attribute("BagUseEffect")?.Value ?? "-1"),
                BagUseType = int.Parse(element.Attribute("BagUseType")?.Value ?? "0"),
                BagUseRate = int.Parse(element.Attribute("BagUseRate")?.Value ?? "10000")
            };
        }

        public XElement ToXml()
        {
            var element = new XElement("BagConfig");
            
            // Always include all attributes to maintain complete configuration visibility
            element.Add(new XAttribute("Name", Name));
            element.Add(new XAttribute("ItemRate", ItemRate));
            element.Add(new XAttribute("SetItemRate", SetItemRate));
            element.Add(new XAttribute("SetItemCount", SetItemCount));
            element.Add(new XAttribute("MasterySetItemInclude", MasterySetItemInclude));
            element.Add(new XAttribute("MoneyDrop", MoneyDrop));
            element.Add(new XAttribute("IsPentagramForBeginnersDrop", IsPentagramForBeginnersDrop));
            element.Add(new XAttribute("PartyDropRate", PartyDropRate));
            element.Add(new XAttribute("PartyOneDropOnly", PartyOneDropOnly));
            element.Add(new XAttribute("PartyShareType", PartyShareType));
            element.Add(new XAttribute("BagUseEffect", BagUseEffect));
            element.Add(new XAttribute("BagUseType", BagUseType));
            element.Add(new XAttribute("BagUseRate", BagUseRate));
            
            return element;
        }
    }

    public class SummonBook
    {
        public int Enable { get; set; } = 0;
        public int DropRate { get; set; } = 0;
        public int ItemCat { get; set; } = 0;
        public int ItemIndex { get; set; } = 0;

        public static SummonBook FromXml(XElement element)
        {
            return new SummonBook
            {
                Enable = int.Parse(element.Attribute("Enable")?.Value ?? "0"),
                DropRate = int.Parse(element.Attribute("DropRate")?.Value ?? "0"),
                ItemCat = int.Parse(element.Attribute("ItemCat")?.Value ?? "0"),
                ItemIndex = int.Parse(element.Attribute("ItemIndex")?.Value ?? "0")
            };
        }

        public XElement ToXml()
        {
            var element = new XElement("SummonBook");
            
            // Always include all attributes to maintain complete configuration visibility
            element.Add(new XAttribute("Enable", Enable));
            element.Add(new XAttribute("DropRate", DropRate));
            element.Add(new XAttribute("ItemCat", ItemCat));
            element.Add(new XAttribute("ItemIndex", ItemIndex));
            
            return element;
        }
    }

    public class AddCoin
    {
        public int Enable { get; set; } = 0;
        public int CoinType { get; set; } = 0;
        public int CoinValue { get; set; } = 0;
        public int PlayerMinLevel { get; set; } = 1;
        public string PlayerMaxLevel { get; set; } = "MAX";
        public int PlayerMinReset { get; set; } = 0;
        public string PlayerMaxReset { get; set; } = "MAX";

        public static AddCoin FromXml(XElement element)
        {
            return new AddCoin
            {
                Enable = int.Parse(element.Attribute("Enable")?.Value ?? "0"),
                CoinType = int.Parse(element.Attribute("CoinType")?.Value ?? "0"),
                CoinValue = int.Parse(element.Attribute("CoinValue")?.Value ?? "0"),
                PlayerMinLevel = int.Parse(element.Attribute("PlayerMinLevel")?.Value ?? "1"),
                PlayerMaxLevel = element.Attribute("PlayerMaxLevel")?.Value ?? "MAX",
                PlayerMinReset = int.Parse(element.Attribute("PlayerMinReset")?.Value ?? "0"),
                PlayerMaxReset = element.Attribute("PlayerMaxReset")?.Value ?? "MAX"
            };
        }

        public XElement ToXml()
        {
            var element = new XElement("AddCoin");
            
            // Always include all attributes to maintain complete configuration visibility
            element.Add(new XAttribute("Enable", Enable));
            element.Add(new XAttribute("CoinType", CoinType));
            element.Add(new XAttribute("CoinValue", CoinValue));
            element.Add(new XAttribute("PlayerMinLevel", PlayerMinLevel));
            element.Add(new XAttribute("PlayerMaxLevel", PlayerMaxLevel));
            element.Add(new XAttribute("PlayerMinReset", PlayerMinReset));
            element.Add(new XAttribute("PlayerMaxReset", PlayerMaxReset));
            
            return element;
        }
    }

    public class Ruud
    {
        public int GainRate { get; set; } = 0;
        public int MinValue { get; set; } = 1;
        public int MaxValue { get; set; } = 10;
        public int PlayerMinLevel { get; set; } = 1;
        public string PlayerMaxLevel { get; set; } = "MAX";
        public int PlayerMinReset { get; set; } = 0;
        public string PlayerMaxReset { get; set; } = "MAX";

        public static Ruud FromXml(XElement element)
        {
            return new Ruud
            {
                GainRate = int.Parse(element.Attribute("GainRate")?.Value ?? "0"),
                MinValue = int.Parse(element.Attribute("MinValue")?.Value ?? "1"),
                MaxValue = int.Parse(element.Attribute("MaxValue")?.Value ?? "10"),
                PlayerMinLevel = int.Parse(element.Attribute("PlayerMinLevel")?.Value ?? "1"),
                PlayerMaxLevel = element.Attribute("PlayerMaxLevel")?.Value ?? "MAX",
                PlayerMinReset = int.Parse(element.Attribute("PlayerMinReset")?.Value ?? "0"),
                PlayerMaxReset = element.Attribute("PlayerMaxReset")?.Value ?? "MAX"
            };
        }

        public XElement ToXml()
        {
            var element = new XElement("Ruud");
            
            // Always include all attributes to maintain complete configuration visibility
            element.Add(new XAttribute("GainRate", GainRate));
            element.Add(new XAttribute("MinValue", MinValue));
            element.Add(new XAttribute("MaxValue", MaxValue));
            element.Add(new XAttribute("PlayerMinLevel", PlayerMinLevel));
            element.Add(new XAttribute("PlayerMaxLevel", PlayerMaxLevel));
            element.Add(new XAttribute("PlayerMinReset", PlayerMinReset));
            element.Add(new XAttribute("PlayerMaxReset", PlayerMaxReset));
            
            return element;
        }
    }
}
