using System;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace FFTweaks.Outhard
{
[BepInPlugin("com.fierr.fftwaks", "FFTweaks-Outhard", "0.1.0")]
[BepInDependency("com.iggy.altstart")]
public class FFTweaksOuthardPlugin : BaseUnityPlugin
{
    private static readonly int[] StartChoiceIds =
    {
        -2200, -2201, -2202, -2203, -2204, -2205, -2206,
        -2207, -2208, -2209, -2210, -2211, -2212, -2213,
        -2214, -2215, -2216, -2217, -2218, -2219, -2220,
        -2221, -2222, -2223
    };

    private static readonly string[] BaseStartStatusEffects =
    {
        "Strength",
        "Dexterity",
        "Constitution",
        "Intellect",
        "Wisdom",
        "Charisma"
    };

    private void Awake()
    {
        Logger.LogInfo("FFTweaks-Outhard loaded.");
        Logger.LogInfo(string.Format("FFTweaks-Outhard init: startChoiceIds={0} assembly={1}", StartChoiceIds.Length, typeof(FFTweaksOuthardPlugin).Assembly.Location));
        var harmony = new Harmony("com.fierr.fftwaks");
        harmony.PatchAll(typeof(FFTweaksOuthardPlugin));
    }

    private static bool IsStartChoiceItem(Item item)
    {
        return item != null && StartChoiceIds.Contains(item.ItemID);
    }

    private static void ApplyBasePassives(Character character)
    {
        if (character == null || character.StatusEffectMngr == null)
        {
            return;
        }

        foreach (string effectId in BaseStartStatusEffects)
        {
            if (!character.StatusEffectMngr.HasStatusEffect(effectId))
            {
                character.StatusEffectMngr.AddStatusEffect(effectId);
            }
        }
    }

    [HarmonyPatch(typeof(CharacterSkillKnowledge), "AddItem", new Type[] { typeof(Item) })]
    private static class CharacterSkillKnowledge_AddItem
    {
        private static void Postfix(Item _item)
        {
            if (!IsStartChoiceItem(_item))
            {
                return;
            }

            Character owner = _item.OwnerCharacter;
            if (owner == null || !owner.IsLocalPlayer)
            {
                return;
            }

            ApplyBasePassives(owner);
        }
    }
}
}
