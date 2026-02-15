using System.Collections;
using System.Collections.Generic;
using MageQuitModFramework.Data;
using MageQuitModFramework.Spells;
using UnityEngine;

namespace MageKit.SpellRain
{
    /// <summary>
    /// Manages automatic spell rain spawning during rounds.
    /// Spawns pickups at regular intervals within configurable boundaries.
    /// </summary>
    public class SpellRainManager : MonoBehaviour
    {
        private static SpellRainManager _instance;
        private List<GameObject> _spawnedPickups = [];
        private Coroutine _spawnCoroutine;

        // Configurable parameters
        public static float SpawnInterval { get; set; } = .5f;
        public static float MinX { get; set; } = 50f;
        public static float MaxX { get; set; } = 150f;
        public static float MinZ { get; set; } = 50f;
        public static float MaxZ { get; set; } = 150f;
        public static float SpawnHeight { get; set; } = 10f;
        public static bool EnableAutoSpawn { get; set; } = true;

        public static void Initialize()
        {
            if (_instance != null)
                return;

            GameObject managerObj = new GameObject("SpellRainManager");
            _instance = managerObj.AddComponent<SpellRainManager>();
            DontDestroyOnLoad(managerObj);

            // Subscribe to round lifecycle events
            GameEventsObserver.SubscribeToRoundStart(OnRoundStart);
            GameEventsObserver.SubscribeToRoundEnd(OnRoundEnd);

            Plugin.Log.LogInfo("[SpellRainManager] Initialized and subscribed to round events");
        }

        public static void Cleanup()
        {
            if (_instance != null)
            {
                GameEventsObserver.UnsubscribeFromRoundStart(OnRoundStart);
                GameEventsObserver.UnsubscribeFromRoundEnd(OnRoundEnd);

                Destroy(_instance.gameObject);
                _instance = null;

                Plugin.Log.LogInfo("[SpellRainManager] Cleaned up");
            }
        }

        private static void OnRoundStart()
        {
            if (_instance != null && EnableAutoSpawn)
            {
                _instance.StartSpawnCoroutine();
            }
        }

        private static void OnRoundEnd()
        {
            if (_instance != null)
            {
                _instance.StopSpawnCoroutine();
                _instance.DestroyAllPickups();
            }
        }

        private void StartSpawnCoroutine()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
            }

            _spawnCoroutine = StartCoroutine(SpawnPickupsCoroutine());
            Plugin.Log.LogInfo($"[SpellRainManager] Started spawn coroutine (interval: {SpawnInterval}s)");
        }

        private void StopSpawnCoroutine()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
                Plugin.Log.LogInfo("[SpellRainManager] Stopped spawn coroutine");
            }
        }

        private IEnumerator SpawnPickupsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(SpawnInterval);

                SpawnRandomPickup();
            }
        }

        private void SpawnRandomPickup()
        {
            float x = Random.Range(MinX, MaxX);
            float z = Random.Range(MinZ, MaxZ);
            Vector3 position = new Vector3(x, SpawnHeight, z);

            GameObject pickup = SpellRainSpawner.NetworkSpawnRandomPickupCrystal(position, SpellButton.Secondary);
            if (pickup != null)
            {
                _spawnedPickups.Add(pickup);
                Plugin.Log.LogInfo($"[SpellRainManager] Spawned pickup at ({x:F1}, {SpawnHeight}, {z:F1}). Total: {_spawnedPickups.Count}");
            }
        }

        private void DestroyAllPickups()
        {
            int count = _spawnedPickups.Count;
            foreach (var pickup in _spawnedPickups)
            {
                if (pickup != null)
                {
                    Destroy(pickup);
                }
            }

            _spawnedPickups.Clear();
            Plugin.Log.LogInfo($"[SpellRainManager] Destroyed {count} pickups");
        }

        private void OnDestroy()
        {
            StopSpawnCoroutine();
        }
    }
}
