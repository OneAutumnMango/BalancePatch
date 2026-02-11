using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MageQuitModFramework.Spells;

namespace BalancePatch.Randomiser
{
    public static class RandomiserHelpers
    {
        public static int HashSeed(string seed)
        {
            int hash = 0;
            foreach (char c in seed)
            {
                hash = (hash * 31 + c) & 0x7FFFFFFF;
            }
            return hash;
        }

        public static Dictionary<Type, Dictionary<string, float>> PrecomputeSpellAttributes(string[] fieldNames, Func<string, float, float> valueTransform = null)
        {
            var result = new Dictionary<Type, Dictionary<string, float>>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName = SpellModificationSystem.GetSpellObjectTypeName(name);
                Type spellType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(fullTypeName, false))
                    .FirstOrDefault(t => t != null);

                if (spellType == null)
                    continue;

                object instance = Activator.CreateInstance(spellType);
                var values = new Dictionary<string, float>();

                foreach (var fieldName in fieldNames)
                {
                    FieldInfo field = spellType.GetField(fieldName, flags);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        float original = (float)field.GetValue(instance);
                        float transformed = valueTransform != null ? valueTransform(fieldName, original) : original;
                        values[fieldName] = transformed;
                    }
                }

                result[spellType] = values;
            }

            return result;
        }
    }
}
