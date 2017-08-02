﻿using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SabberStoneCore.Loader
{
	public class Generate
    {
        private static readonly string Path = @"C:\Users\admin\Source\Repos\";

        private static readonly Regex Rgx = new Regex("[^a-zA-Z0-9 -]");

        private static string UpperCaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            var a = s.ToLower().ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static void CardSetFile(Dictionary<string, Card>.ValueCollection values)
        {
            //var cardSets = new[] // {CardSet.EXPERT1}; //Enum.GetValues(typeof(CardSet));
            //   // {CardSet.FP2, CardSet.TGT, CardSet.LOE, CardSet.OG, CardSet.KARA, CardSet.GANGS};
            //{ CardSet.GVG};
            //var cardSets = new[] { CardSet.UNGORO};
            var cardSets = Enum.GetValues(typeof(ECardSet));
            foreach (ECardSet cardSet in cardSets)
            {
                var className = UpperCaseFirst(cardSet.ToString()) + "CardsGen";
                var path = Path + @"SabberStone\SabberStoneCore\Loader\Generated\CardSets\";
                var classNameTest = UpperCaseFirst(cardSet.ToString()) + "CardsGenTest";
                var pathTest = Path + @"SabberStone\SabberStoneCore\Loader\Generated\CardSetsTest\";

                WriteCardSetFile(cardSet, className, path, values);
                WriteCardSetTestFile(cardSet, classNameTest, pathTest, values);
            }
        }

        private static void WriteCardSetFile(ECardSet cardSet, string className, string path, Dictionary<string, Card>.ValueCollection values)
        {
            var cardClasses = Enum.GetValues(typeof(ECardClass));
            var methods = new List<string>();

            var str = new StringBuilder();
            str.AppendLine("using System.Collections.Generic;");
            str.AppendLine("using SabberStoneCore.Enchants;");
            str.AppendLine("using SabberStoneCore.Conditions;");
            str.AppendLine("using SabberStoneCore.Enums;");
            str.AppendLine("using SabberStoneCore.Model;");
            str.AppendLine("using SabberStoneCore.Tasks;");
            str.AppendLine("using SabberStoneCore.Tasks.SimpleTasks;");
            str.AppendLine();
            str.AppendLine("namespace SabberStoneCore.Loader.Generated.CardSets");
            str.AppendLine("{");
            str.AppendLine($"\tpublic class {className}");
            str.AppendLine("\t{");

            var heroes = CreateMethode("Heroes", values, null, cardSet, ECardType.HERO, ECardClass.INVALID);
            if (heroes != null)
            {
                methods.Add("Heroes");
                str.Append(heroes);
                str.AppendLine();
            }

            var heroPowers = CreateMethode("HeroPowers", values, null, cardSet, ECardType.HERO_POWER, ECardClass.INVALID);
            if (heroPowers != null)
            {
                methods.Add("HeroPowers");
                str.Append(heroPowers);
                str.AppendLine();
            }

            foreach (ECardClass cardClass in cardClasses)
            {
                if (cardClass == ECardClass.NEUTRAL || cardClass == ECardClass.INVALID)
                    continue;

                var cardClassString = CreateMethode(UpperCaseFirst(cardClass.ToString()), values, true, cardSet, ECardType.INVALID, cardClass);
                if (cardClassString != null)
                {
                    methods.Add(UpperCaseFirst(cardClass.ToString()));
                    str.Append(cardClassString);
                    str.AppendLine();
                }
                var cardClassNonCollectString = CreateMethode(UpperCaseFirst(cardClass.ToString()) + "NonCollect", values, false, cardSet, ECardType.INVALID, cardClass);
                if (cardClassNonCollectString != null)
                {
                    methods.Add(UpperCaseFirst(cardClass.ToString()) + "NonCollect");
                    str.Append(cardClassNonCollectString);
                    str.AppendLine();
                }
            }
            var neutralClassString = CreateMethode(UpperCaseFirst(ECardClass.NEUTRAL.ToString()), values, true, cardSet,
                ECardType.INVALID, ECardClass.NEUTRAL);
            if (neutralClassString != null)
            {
                methods.Add(UpperCaseFirst(ECardClass.NEUTRAL.ToString()));
                str.Append(neutralClassString);
                str.AppendLine();
            }

            var neutralNonCollectClassString = CreateMethode(UpperCaseFirst(ECardClass.NEUTRAL.ToString()) + "NonCollect", values, false,
                cardSet, ECardType.INVALID, ECardClass.NEUTRAL);
            if (neutralNonCollectClassString != null)
            {
                methods.Add(UpperCaseFirst(ECardClass.NEUTRAL.ToString()) + "NonCollect");
                str.Append(neutralNonCollectClassString);
                str.AppendLine();
            }

            str.AppendLine("\t\tpublic static void AddAll(Dictionary<string, List<Enchantment>> cards)");
            str.AppendLine("\t\t{");
            methods.ForEach(p => str.AppendLine($"\t\t\t{p}(cards);"));
            str.AppendLine("\t\t}");
            str.AppendLine("\t}");
            str.AppendLine("}");

            var file = path + className + ".cs";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Console.WriteLine($"Writing cardset class: {file}.");
            File.WriteAllText(file, str.ToString());
        }

        private static string CreateMethode(string name,
            Dictionary<string, Card>.ValueCollection values, bool? collect, ECardSet set, ECardType type,
            ECardClass cardClass)
        {
            var valuesOrdered = values
                .Where(p => p.Set == set
                            && (collect == null || p.Collectible == collect)
                            && (type == ECardType.INVALID && p.Type != ECardType.HERO && p.Type != ECardType.HERO_POWER || p.Type == type)
                            && (cardClass == ECardClass.INVALID || p.Class == cardClass))
                            .OrderBy(p => p.Type.ToString());

            if (!valuesOrdered.Any())
            {
                return null;
            }
            var str = new StringBuilder();
            str.AppendLine($"\t\tprivate static void {name}(IDictionary<string, List<Enchantment>> cards)");
            str.AppendLine("\t\t{");
            foreach (var card in valuesOrdered)
            {
                str.Append(AddCardString(card));
                str.Append(AddCardCode(card));
            }
            str.AppendLine("\t\t}");
            return str.ToString();
        }

        private static string AddCardString(Card card, int tabs = 2)
        {
            var tab = tabs == 2 ? "\t\t" : "\t";
            var atkHpStr = "";
            if (card.Tags.ContainsKey(EGameTag.ATK) || card.Tags.ContainsKey(EGameTag.HEALTH))
            {
                var atk = card[EGameTag.ATK];
                var hp = card[EGameTag.HEALTH];
                atkHpStr = $"[ATK:{atk}/HP:{hp}] ";
            }

            var cardRace = "";
            if (card.Race != ERace.INVALID)
                cardRace = $"Race: {card.Race.ToString().ToLower()}, ";
            var cardFac = "";
            if (card.Faction != EFaction.INVALID)
                cardFac = $"Fac: {card.Faction.ToString().ToLower()}, ";
            var cardSet = "";
            if (card.Set != ECardSet.INVALID)
                cardSet = $"Set: {card.Set.ToString().ToLower()}, ";
            var cardRarity = "";
            if (card.Rarity != ERarity.INVALID)
                cardRarity = $"Rarity: {card.Rarity.ToString().ToLower()}";

            var except = new List<EGameTag>
            {
                EGameTag.COST,
                EGameTag.ATK,
                EGameTag.HEALTH,
                EGameTag.CARD_SET,
                EGameTag.CARDTYPE,
                EGameTag.RARITY,
                EGameTag.AttackVisualType,
                EGameTag.CLASS,
                EGameTag.CARDRACE,
                EGameTag.FACTION,
                EGameTag.COLLECTIBLE,
                EGameTag.DevState,
                EGameTag.ENCHANTMENT_BIRTH_VISUAL,
                EGameTag.ENCHANTMENT_IDLE_VISUAL,
                EGameTag.TRIGGER_VISUAL
            };

            var cardType = card.Type.ToString();

            var str = new StringBuilder();
            str.AppendLine($"{tab}\t// ----------------{(" " + cardType + " - " + card.Class).PadLeft(40, '-')}");
            str.AppendLine(
                $"{tab}\t// [{card.Id}] {card.Name} {(!card.Collectible ? "(*) " : "")}- COST:{card.Cost} {atkHpStr}");
            str.AppendLine($"{tab}\t// - {cardRace}{cardFac}{cardSet}{cardRarity}");
            if (card.Text != null)
            {
                str.AppendLine($"{tab}\t// --------------------------------------------------------");
                str.Append($"{tab}\t// Text: {card.Text.Replace("\n", $"\n{tab}\t//       ")}\n");
            }
            if (card.Entourage != null && card.Entourage.Count > 0)
            {
                str.AppendLine($"{tab}\t// --------------------------------------------------------");
                str.AppendLine($"{tab}\t// Entourage: {string.Join(", ", card.Entourage)}");
            }
            var wHead = true;
            foreach (var key in card.Tags.Keys)
            {
                if (except.Contains(key))
                    continue;

                if (wHead)
                {
                    str.AppendLine($"{tab}\t// --------------------------------------------------------");
                    str.AppendLine($"{tab}\t// GameTag:");
                    wHead = false;
                }
                string t = null;
                if (Tag.TypedTags.ContainsKey(key))
                {
                    t = Enum.GetName(Tag.TypedTags[key], (int) card.Tags[key]);
                }

                str.AppendLine($"{tab}\t// - {key} = {(t != null ? t : card.Tags[key].ToString())}");
            }

            if (card.PlayRequirements.Count > 0)
            {
                str.AppendLine($"{tab}\t// --------------------------------------------------------");
                str.AppendLine($"{tab}\t// PlayReq:");
            }
            foreach (var key in card.PlayRequirements.Keys)
            {
                str.AppendLine($"{tab}\t// - {key} = {card.PlayRequirements[key]}");
            }

            wHead = true;
            foreach (var key in card.RefTags.Keys)
            {
                if (wHead)
                {
                    str.AppendLine($"{tab}\t// --------------------------------------------------------");
                    str.AppendLine($"{tab}\t// RefTag:");
                    wHead = false;
                }
                string t = null;
                if (Tag.TypedTags.ContainsKey(key))
                {
                    t = Enum.GetName(Tag.TypedTags[key], (int) card.Tags[key]);
                }
                str.AppendLine($"{tab}\t// - {key} = {t ?? card.RefTags[key].ToString()}");
            }
            str.AppendLine($"{tab}\t// --------------------------------------------------------");
            return str.ToString();
        }

        internal static void EnchantmentLeftOver(Card c)
        {
            throw new NotImplementedException();
        }

        private static string AddCardCode(Card card)
        {
            var str = new StringBuilder();

            var enchantId = Cards.All
                .Where(p => p.Id.Contains(card.Id) && p.Id.Length > card.Id.Length && p.Type == ECardType.ENCHANTMENT)
                .Select(p => p.Id).FirstOrDefault();

            var activations = new List<string>();
            if (card.Type == ECardType.SPELL)
                activations.Add("EnchantmentActivation.SPELL");
            if (card.Type == ECardType.WEAPON)
                activations.Add("EnchantmentActivation.WEAPON");
            if (card[EGameTag.BATTLECRY] == 1)
                activations.Add("EnchantmentActivation.BATTLECRY");
            if (card[EGameTag.DEATHRATTLE] == 1)
                activations.Add("EnchantmentActivation.DEATHRATTLE");

            str.AppendLine($"\t\t\tcards.Add(\"{card.Id}\", new List<Enchantment> {{");
            str.AppendLine($"\t\t\t\t// TODO [{card.Id}] {card.Name} && Test: {card.Name}_{card.Id}");
            if (activations.Count > 0)
            {
                activations.ForEach(p =>
                {
                    str.AppendLine($"\t\t\t\tnew Enchantment");
                    str.AppendLine($"\t\t\t\t{{");
                    if (enchantId != null)
                        str.AppendLine($"\t\t\t\t\tInfoCardId = \"{enchantId}\",");
                    str.AppendLine($"\t\t\t\t\tActivation = {p},");
                    str.AppendLine($"\t\t\t\t\tSingleTask = null,");
                    str.AppendLine($"\t\t\t\t}},");
                });
            }
            else
            {
                str.AppendLine($"\t\t\t\tnew Enchantment");
                str.AppendLine($"\t\t\t\t{{");
                if (enchantId != null)
                    str.AppendLine($"\t\t\t\t\tInfoCardId = \"{enchantId}\",");
                str.AppendLine($"\t\t\t\t\t//Activation = null,");
                str.AppendLine($"\t\t\t\t\t//SingleTask = null,");
                str.AppendLine($"\t\t\t\t}}");
            }

            str.AppendLine($"\t\t\t}});\n");
            return str.ToString();
        }

        private static void WriteCardSetTestFile(ECardSet cardSet, string className, string path, Dictionary<string, Card>.ValueCollection values)
        {
            var cardClasses = Enum.GetValues(typeof(ECardClass));

            var str = new StringBuilder();
            str.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
            str.AppendLine("using SabberStoneCore.Enums;");
            str.AppendLine("using SabberStoneCore.Config;");
            str.AppendLine("using SabberStoneCore.Model;");
            str.AppendLine();
            str.AppendLine("namespace SabberStoneUnitTest.CardSets");
            str.AppendLine("{");

            var heroes = CreateMethodeTest("Heroes", values, null, cardSet, ECardType.HERO, ECardClass.INVALID);
            if (heroes != null)
            {
                str.Append(heroes);
                str.AppendLine();
            }

            var heroPowers = CreateMethodeTest("HeroPowers", values, null, cardSet, ECardType.HERO_POWER, ECardClass.INVALID);
            if (heroPowers != null)
            {
                str.Append(heroPowers);
                str.AppendLine();
            }

            foreach (ECardClass cardClass in cardClasses)
            {
                if (cardClass == ECardClass.NEUTRAL || cardClass == ECardClass.INVALID)
                    continue;

                var cardClassString = CreateMethodeTest(UpperCaseFirst(cardClass.ToString()), values, true, cardSet,
                    ECardType.INVALID, cardClass);
                if (cardClassString != null)
                {
                    str.Append(cardClassString);
                    str.AppendLine();
                }
            }

            var neutralCardString = CreateMethodeTest(UpperCaseFirst(ECardClass.NEUTRAL.ToString()), values, true, cardSet,
                ECardType.INVALID, ECardClass.NEUTRAL);
            if (neutralCardString != null)
            {
                str.Append(neutralCardString);
                str.AppendLine();
            }

            str.AppendLine("}");

            var file = path + className + ".cs";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Console.WriteLine($"Writing test class: {file}.");
            File.WriteAllText(file, str.ToString());
        }

        private static string CreateMethodeTest(string name,
            Dictionary<string, Card>.ValueCollection values, bool? collect, ECardSet set, ECardType type,
            ECardClass cardClass)
        {
            var valuesOrdered = values.Where(p => p.Set == set
                            && (collect == null || p.Collectible == collect)
                            && (type == ECardType.INVALID && p.Type != ECardType.HERO && p.Type != ECardType.HERO_POWER || p.Type == type)
                            && (cardClass == ECardClass.INVALID || p.Class == cardClass)).OrderBy(p => p.Type.ToString());
            if (!valuesOrdered.Any())
            {
                return null;
            }
            var str = new StringBuilder();
            str.AppendLine("\t[TestClass]");
            str.AppendLine($"\tpublic class {name}{UpperCaseFirst(set.ToString())}Test");
            str.AppendLine("\t{");
            foreach (var card in valuesOrdered)
            {
                var cardNameRx = Rgx.Replace(card.Name, "").Split(' ', '-').ToList();
                var cardName = string.Join("", cardNameRx.Select(p => UpperCaseFirst(p)).ToList());
                var heroClass1 = card.Class == ECardClass.INVALID || card.Class == ECardClass.NEUTRAL
                    ? ECardClass.MAGE
                    : card.Class;
                var heroClass2 = card.Class == ECardClass.INVALID || card.Class == ECardClass.NEUTRAL
                    ? ECardClass.MAGE
                    : card.Class;
                str.Append(AddCardString(card, 1));
                str.AppendLine("\t\t[TestMethod, Ignore]");
                str.AppendLine($"\t\tpublic void {cardName}_{card.Id}()");
                str.AppendLine("\t\t{");
                str.AppendLine($"\t\t\t// TODO {cardName}_{card.Id} test");
                str.AppendLine("\t\t\tvar game = new Game(new GameConfig");
                str.AppendLine("\t\t\t{");
                str.AppendLine("\t\t\t\tStartPlayer = 1,");
                str.AppendLine($"\t\t\t\tPlayer1HeroClass = CardClass.{heroClass1},");
                str.AppendLine($"\t\t\t\tPlayer2HeroClass = CardClass.{heroClass2},");
                str.AppendLine("\t\t\t\tFillDecks = true");
                str.AppendLine("\t\t\t});");
                str.AppendLine("\t\t\tgame.StartGame();");
                str.AppendLine("\t\t\tgame.Player1.BaseMana = 10;");
                str.AppendLine("\t\t\tgame.Player2.BaseMana = 10;");
                str.AppendLine($"\t\t\t//var testCard = game.CurrentPlayer.Draw(Cards.FromName(\"{card.Name}\"));");
                str.AppendLine("\t\t}");
                str.AppendLine();
            }
            str.AppendLine("\t}");
            return str.ToString();
        }

        public static void EnchantmentLeftOver(Dictionary<string, Card>.ValueCollection cardsValues)
        {
            var str = new StringBuilder();
            str.AppendLine($"CARD_ID|IMPL.|SET|FORMAT|TYPE|CLASS|NAME|TEXT");
            foreach (var card in cardsValues)
            {
                if (!card.Collectible || !Cards.StandardSets.Contains(card.Set) && !card.Implemented)
                    continue;

                str.AppendLine($"{card.Id}|{card.Implemented}|{card.Set}|{(Cards.StandardSets.Contains(card.Set)?"S":"W")}|{card.Type}|{card.Class}|{card.Name}|{RemoveLineEndings(card.Text)}");

                //if (!card.Collectible || card.Implemented)
                //    continue;
                //str.AppendLine($"{card.AssetId}");
            }

            var path = Path + @"SabberStone\SabberStoneCore\Loader\Generated\Statistics\";
            var file = path + "echantmentsleft.csv";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            Console.WriteLine($"Writing cardset class: {file}.");
            File.WriteAllText(file, str.ToString());
        }

        public static string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value
                .Replace("\r\n", string.Empty)
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace(lineSeparator, " ")
                .Replace(paragraphSeparator, " ")
                .Replace("[x]", string.Empty).Trim()
                ;
        }
    }
}