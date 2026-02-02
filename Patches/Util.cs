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

        public static void PopulateDefaultClassAttributes()
        {
            var rng = Plugin.Randomiser;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] tweakFields = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];

            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName;
                if (name == SpellName.RockBlock)
                    fullTypeName = "StonewallObject";
                else if (name == SpellName.FlameLeash)
                    fullTypeName = "BurningLeashObject";
                else if (name == SpellName.SomerAssault)
                    fullTypeName = "SomAssaultObject";
                else if (name == SpellName.Suspend)
                    fullTypeName = "SustainObjectObject";
                else
                    fullTypeName = $"{name}Object";

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

        public static SpellManager mgr;

        static void Postfix(SpellManager __instance)
        {
            mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;

            spellManagerIsLoaded = true;

            DefaultSpellTable = mgr.spell_table.ToDictionary(kvp => kvp.Key, kvp => new Spell(kvp.Value));
        }


    }
}
