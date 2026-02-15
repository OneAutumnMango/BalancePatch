using System.Collections.Generic;
using MageQuitModFramework.Utilities;
using UnityEngine;

namespace MageKit.SpellRain
{
    public class SpellRainHelper : MonoBehaviour
    {
        public SpellName spellToGive;
        public SpellButton spellButton = SpellButton.Secondary;
        public string networkId; // Unique ID for network synchronization
        private bool pickedUp = false;
        private SoundPlayer soundPlayer;

        void Start()
        {
            soundPlayer = GetComponent<SoundPlayer>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (pickedUp) return;

            Identity wizardId = other.transform.root.GetComponent<Identity>();
            if (wizardId != null)
            {
                PickupSpell(wizardId.owner);
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (pickedUp) return;

            Identity wizardId = collision.collider.transform.root.GetComponent<Identity>();
            if (wizardId != null)
            {
                PickupSpell(wizardId.owner);
            }
        }

        void PickupSpell(int pickerOwner)
        {
            pickedUp = true;

            if (!SpellRainSpawner.oneTimeSpells.ContainsKey(pickerOwner))
            {
                SpellRainSpawner.oneTimeSpells[pickerOwner] = [];
            }

            SpellRainSpawner.oneTimeSpells[pickerOwner][spellButton] = new OneTimeSpell
            {
                spellName = spellToGive,
                button    = spellButton,
                used      = false
            };

            Globals.spell_manager.AddSpellToPlayer(
                spellButton,
                spellToGive,
                pickerOwner
            );

            soundPlayer?.PlaySoundInstantiate("event:/sfx/ice/cryogenic-pick-up", 5f);

            ShowHudButton(spellButton);

            Plugin.Log.LogInfo($"Player {pickerOwner} picked up one-time spell: {spellToGive} in slot {spellButton}");

            // Notify network that this pickup was collected
            SpellRainNetworking.NetworkPickup(networkId, pickerOwner);

            Destroy(gameObject, 0.1f);
        }

        public static void HideHudButton(SpellButton spellButton)
        {
            if (SpellHudController.current == null) {
                Plugin.Log.LogWarning("SpellHudController is null, cannot update HUD");
                return;
            }

            var hud = SpellHudController.current.spellHuds[(int)spellButton];
            GameModificationHelpers.SetPrivateField(hud, "spellButton", SpellButton.None);
            hud.Hide();
        }

        public void ShowHudButton(SpellButton spellButton)
        {
            if (SpellHudController.current == null) {
                Plugin.Log.LogWarning("SpellHudController is null, cannot update HUD");
                return;
            }

            var hud = SpellHudController.current.spellHuds[(int)spellButton];
            GameModificationHelpers.SetPrivateField(hud, "spellButton", spellButton);
            SpellHudController.current.Initialize();
            hud.Show();
        }
    }
}