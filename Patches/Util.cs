using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalancePatch;
using HarmonyLib;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Patches.Util
{
    [HarmonyPatch(typeof(SpellManager), "Awake")]
    public static class Util
    {
        public static Dictionary<SpellName, Dictionary<string, float>> DefaultClassAttributes = [];
        public static Dictionary<SpellName, Spell> DefaultSpellTable = [];
        public static bool spellManagerIsLoaded = false;
        public static SpellManager mgr;

        static void Postfix(SpellManager __instance)
        {
            mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;

            Plugin.Log.LogInfo("SpellManager is loaded.");
            spellManagerIsLoaded = true;

            Plugin.Log.LogInfo("Populating default spell table.");
            DefaultSpellTable = mgr.spell_table.ToDictionary(kvp => kvp.Key, kvp => new Spell(kvp.Value));
        }


        public static string GetSpellObjectTypeName(SpellName name)
        {
            return name switch
            {
                SpellName.RockBlock => "StonewallObject",
                SpellName.FlameLeash => "BurningLeashObject",
                SpellName.SomerAssault => "SomAssaultObject",
                SpellName.Sustain => "SustainObjectObject",
                _ => $"{name}Object"
            };
        }

        public static SpellName? GetSpellNameFromTypeName(string typeName)
        {
            return typeName switch
            {
                "StonewallObject"     => SpellName.RockBlock,
                "BurningLeashObject"  => SpellName.FlameLeash,
                "SomAssaultObject"    => SpellName.SomerAssault,
                "SustainObjectObject" => SpellName.Sustain,
                _ => Enum.GetValues(typeof(SpellName))
                    .Cast<SpellName?>()
                    .FirstOrDefault(name => $"{name}Object" == typeName)
            };
        }

        public static void PopulateDefaultClassAttributes()
        {
            var rng = Plugin.Randomiser;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] tweakFields = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];

            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName = GetSpellObjectTypeName(name);

                Type spellType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(fullTypeName, false))
                    .FirstOrDefault(t => t != null);

                if (spellType == null)
                    continue;

                // Construct a dummy instance
                SpellObject instance = Activator.CreateInstance(spellType) as SpellObject;

                var values = new Dictionary<string, float>();

                foreach (var fieldName in tweakFields)
                {
                    FieldInfo field = spellType.GetField(fieldName, flags);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        float original = (float)field.GetValue(instance);

                        values[fieldName] = original;
                    }
                }

                DefaultClassAttributes[name] = values;
            }
        }
    }
}
