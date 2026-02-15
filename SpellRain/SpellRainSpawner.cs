using System.Collections.Generic;
using UnityEngine;

namespace MageKit.SpellRain
{
    public class OneTimeSpell
    {
        public SpellName spellName;
        public SpellButton button;
        public bool used = false;
    }

    public static class SpellRainSpawner
    {
        public static Dictionary<int, Dictionary<SpellButton, OneTimeSpell>> oneTimeSpells = [];
        private static GameObject crystalPrefab;
        private static bool prefabInitialized = false;

        private static GameObject GetCrystalPrefab()
        {
            // Check if cached prefab is still valid (not destroyed)
            if (crystalPrefab == null)
            {
                prefabInitialized = false;
            }

            if (!prefabInitialized)
            {
                GameObject go = GameUtility.Instantiate("Units/Crystal", Vector3.zero, Quaternion.identity, 0);
                var crystal = go.GetComponent<CrystalObject>();
                if (crystal != null)
                {
                    crystal.Init(null, 0, SpellName.Brrage, CrystalObject.CrystalState.Inert, null, false);
                    crystal.TransitionState(CrystalObject.CrystalState.Preserved);

                    GameObject prefabCopy = Object.Instantiate(go);
                    prefabCopy.SetActive(false);
                    Object.DontDestroyOnLoad(prefabCopy);

                    // Remove PhotonView to prevent network conflicts
                    RemovePhotonComponents(prefabCopy);

                    crystalPrefab = prefabCopy;
                    Object.Destroy(go); // destroy the original temporary instance
                    Plugin.Log.LogInfo("Spawned, cloned, and cached a CrystalObject prefab using GameUtility.Instantiate.");
                }
                else
                {
                    Plugin.Log.LogError("Failed to get CrystalObject from instantiated prefab!");
                }

                prefabInitialized = true;
            }
            return crystalPrefab;
        }

        private static void RemovePhotonComponents(GameObject obj)
        {
            // Remove PhotonView components to avoid network ID conflicts
            PhotonView[] views = obj.GetComponentsInChildren<PhotonView>(true);
            foreach (var view in views)
            {
                Object.DestroyImmediate(view);
            }

            if (views.Length > 0)
            {
                Plugin.Log.LogInfo($"Removed {views.Length} PhotonView component(s) from crystal.");
            }
        }

        public static GameObject SpawnPickupCrystal(Vector3 position, SpellName spell, SpellButton targetSlot = SpellButton.Secondary)
        {
            GameObject prefab = GetCrystalPrefab();
            if (prefab == null)
            {
                Plugin.Log.LogError("Could not find crystal prefab to spawn!");
                return null;
            }

            GameObject newCrystal = Object.Instantiate(prefab, position, Quaternion.identity);

            // Remove PhotonView from instantiated copy (prefab removal is deferred)
            RemovePhotonComponents(newCrystal);

            // make copied spell mesh visible
            newCrystal.SetActive(true);
            newCrystal.layer = 12;
            foreach (Transform child in newCrystal.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 12;
                string name = child.gameObject.name.ToLower();
                if (name.Contains("spell_icon_plate"))
                {
                    child.gameObject.SetActive(true);
                    // Activate parent hierarchy
                    Transform parent = child.parent;
                    while (parent != null)
                    {
                        parent.gameObject.SetActive(true);
                        parent = parent.parent;
                    }
                }
            }

            // Disable CrystalObject behavior
            CrystalObject crystalObj = newCrystal.GetComponent<CrystalObject>();
            if (crystalObj != null)
            {
                // Update visuals BEFORE disabling component
                SetupCrystalVisuals(newCrystal, spell, crystalObj);
                crystalObj.enabled = false;
            }

            SpellRainHelper pickup = newCrystal.AddComponent<SpellRainHelper>();
            pickup.spellToGive = spell;
            pickup.spellButton  = targetSlot;

            Plugin.Log.LogInfo($"Spawned pickup crystal at {position} with spell: {spell}");
            return newCrystal;
        }

        public static GameObject SpawnRandomPickupCrystal(Vector3 position, SpellButton targetSlot = SpellButton.Secondary)
        {
            var allSpells = System.Enum.GetValues(typeof(SpellName));
            SpellName randomSpell = (SpellName)allSpells.GetValue(Random.Range(0, allSpells.Length));
            return SpawnPickupCrystal(position, randomSpell, targetSlot);
        }

        public static GameObject SpawnPickupNearPlayer(int playerNumber, SpellName spell, float distance = 5f, SpellButton targetSlot = SpellButton.Secondary)
        {
            if (!PlayerManager.players.ContainsKey(playerNumber))
            {
                Plugin.Log.LogWarning($"Player {playerNumber} not found!");
                return null;
            }

            GameObject wizard = PlayerManager.players[playerNumber].wizard;
            if (wizard == null)
            {
                Plugin.Log.LogWarning($"Player {playerNumber} has no wizard!");
                return null;
            }

            Vector3 spawnPos = wizard.transform.position + wizard.transform.forward * distance;
            return SpawnPickupCrystal(spawnPos, spell, targetSlot);
        }

        public static GameObject SpawnRandomPickupNearPlayer(int playerNumber, float distance = 5f, SpellButton targetSlot = SpellButton.Secondary)
        {
            var allSpells = System.Enum.GetValues(typeof(SpellName));
            SpellName randomSpell = (SpellName)allSpells.GetValue(Random.Range(0, allSpells.Length));
            return SpawnPickupNearPlayer(playerNumber, randomSpell, distance, targetSlot);
        }

        public static List<GameObject> SpawnPickupCircle(Vector3 center, int count, float radius, SpellButton targetSlot = SpellButton.Secondary)
        {
            List<GameObject> spawned = [];

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
                Vector3 spawnPos = center + offset;

                GameObject crystal = SpawnRandomPickupCrystal(spawnPos, targetSlot);
                if (crystal != null)
                {
                    spawned.Add(crystal);
                }
            }

            return spawned;
        }

        /// <summary>
        /// Network-safe spawn near player. Only master client spawns, all clients see it.
        /// </summary>
        public static GameObject NetworkSpawnPickupNearPlayer(int playerNumber, SpellName spell, float distance = 5f, SpellButton targetSlot = SpellButton.Secondary)
        {
            if (!PlayerManager.players.ContainsKey(playerNumber))
            {
                Plugin.Log.LogWarning($"Player {playerNumber} not found!");
                return null;
            }

            GameObject wizard = PlayerManager.players[playerNumber].wizard;
            if (wizard == null)
            {
                Plugin.Log.LogWarning($"Player {playerNumber} has no wizard!");
                return null;
            }

            Vector3 spawnPos = wizard.transform.position + wizard.transform.forward * distance;
            return SpellRainNetworking.NetworkSpawnPickup(spawnPos, spell, targetSlot);
        }

        /// <summary>
        /// Network-safe random spawn near player. Only master client spawns, all clients see it.
        /// </summary>
        public static GameObject NetworkSpawnRandomPickupNearPlayer(int playerNumber, float distance = 5f, SpellButton targetSlot = SpellButton.Secondary)
        {
            var allSpells = System.Enum.GetValues(typeof(SpellName));
            SpellName randomSpell = (SpellName)allSpells.GetValue(Random.Range(0, allSpells.Length));
            return NetworkSpawnPickupNearPlayer(playerNumber, randomSpell, distance, targetSlot);
        }

        /// <summary>
        /// Network-safe spawn at arbitrary position. Only master client spawns, all clients see it.
        /// </summary>
        public static GameObject NetworkSpawnPickup(Vector3 position, SpellName spell, SpellButton targetSlot = SpellButton.Secondary)
        {
            return SpellRainNetworking.NetworkSpawnPickup(position, spell, targetSlot);
        }

        /// <summary>
        /// Network-safe random spawn at arbitrary position. Only master client spawns, all clients see it.
        /// </summary>
        public static GameObject NetworkSpawnRandomPickupCrystal(Vector3 position, SpellButton targetSlot = SpellButton.Secondary)
        {
            var allSpells = System.Enum.GetValues(typeof(SpellName));
            SpellName randomSpell = (SpellName)allSpells.GetValue(Random.Range(0, allSpells.Length));
            return NetworkSpawnPickup(position, randomSpell, targetSlot);
        }

        private static void SetupCrystalVisuals(GameObject crystal, SpellName spell, CrystalObject co)
        {
            try
            {
                if (Globals.spell_manager.spell_table.ContainsKey(spell))
                {
                    Spell spellData = Globals.spell_manager.spell_table[spell];
                    Sprite icon = spellData.icon;

                    if (co != null && co.preservedSpellRenderer != null)
                    {
                        Material mat = co.preservedSpellRenderer.materials[0];
                        if (icon != null)
                        {
                            mat.mainTexture = icon.texture;
                            mat.SetTexture(MaterialHashes.EmissionMap, icon.texture);
                            mat.SetTexture(MaterialHashes.OcclusionMap, icon.texture);
                            mat.SetTexture(MaterialHashes.HeightMap, icon.texture);
                            mat.SetColor(MaterialHashes.EmissionColor, Globals.iconEmissionColors[(int)spellData.element]);
                        }

                        // Ensure the spell icon plate is visible
                        if (co.preservedSpellRenderer.gameObject != null)
                        {
                            co.preservedSpellRenderer.gameObject.SetActive(true);
                        }
                    }
                    else {
                        Plugin.Log.LogWarning($"CrystalObject or preservedSpellRenderer not found on crystal prefab for spell: {spell}");
                    }
                    // // Optionally instantiate preserveTokenizePrefab if available
                    // if (crystal.GetComponent<CrystalObject>() is CrystalObject co && co.preserveTokenizePrefab != null)
                    // {
                    //     Object.Instantiate(co.preserveTokenizePrefab, crystal.transform.position, Globals.sideways);
                    // }
                    // // Optionally disable collisions
                    // if (crystal.GetComponent<Rigidbody>() is Rigidbody rb)
                    //     rb.detectCollisions = false;
                }
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning($"Could not set up crystal visuals: {e.Message}");
            }
        }
    }
}