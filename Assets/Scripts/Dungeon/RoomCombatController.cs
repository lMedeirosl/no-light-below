using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoLightBelow.Enemies;
using NoLightBelow.Player;

namespace NoLightBelow.Dungeon
{
    public class RoomCombatController : MonoBehaviour
    {
        [Header("Room Gates")]
        [SerializeField] private DungeonGate entranceGate;
        [SerializeField] private DungeonGate exitGate;

        [Header("Spawn Points")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private Vector3 chestSpawnPosition = new Vector3(0f, 0f, 22f);

        [Header("Encounter Settings")]
        [SerializeField] private int totalWaves = 2;
        [SerializeField] private int enemiesPerWave = 2;

        private int _currentWave = 0;
        private readonly List<CryptSkeletonAI> _activeEnemies = new List<CryptSkeletonAI>();
        private bool _isEncounterStarted;
        private bool _isRoomCleared;

        public bool IsRoomCleared => _isRoomCleared;

        private void OnTriggerEnter(Collider other)
        {
            if (_isEncounterStarted || _isRoomCleared) return;

            if (other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                StartCombatEncounter();
            }
        }

        public void StartCombatEncounter()
        {
            _isEncounterStarted = true;
            Debug.Log("<color=#DC143C>[Câmara Hostil]</color> O jogador adentrou a câmara de combate! As grades se fecham!");

            // Slam gates down!
            if (entranceGate != null) entranceGate.CloseGate();
            if (exitGate != null) exitGate.CloseGate();

            StartCoroutine(SpawnNextWaveRoutine(1.2f));
        }

        private IEnumerator SpawnNextWaveRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            _currentWave++;
            int countToSpawn = enemiesPerWave + (_currentWave - 1);
            Debug.Log($"<color=#DC143C>[Onda {_currentWave}/{totalWaves}]</color> Despertando {countToSpawn} Esqueletos da Cripta!");

            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos;
                if (spawnPoints.Count > 0)
                {
                    spawnPos = spawnPoints[i % spawnPoints.Count].position + Random.insideUnitSphere * 0.5f;
                    spawnPos.y = 0.1f;
                }
                else
                {
                    // Default fallback spawn positions in room
                    spawnPos = transform.position + new Vector3(Random.Range(-5f, 5f), 0.1f, Random.Range(-4f, 6f));
                }

                GameObject skeleton = SkeletonModelBuilder.CreateSkeleton(spawnPos);
                var ai = skeleton.GetComponent<CryptSkeletonAI>();
                if (ai != null)
                {
                    _activeEnemies.Add(ai);
                    ai.OnSkeletonDied += HandleEnemyDied;
                }

                yield return new WaitForSeconds(0.4f);
            }
        }

        private void HandleEnemyDied(CryptSkeletonAI enemy)
        {
            _activeEnemies.Remove(enemy);

            if (_activeEnemies.Count == 0)
            {
                if (_currentWave < totalWaves)
                {
                    StartCoroutine(SpawnNextWaveRoutine(1.5f));
                }
                else
                {
                    OnAllWavesCleared();
                }
            }
        }

        private void OnAllWavesCleared()
        {
            _isRoomCleared = true;
            Debug.Log("<color=#32CD32>[Câmara Purificada]</color> Todos os inimigos foram derrotados! Os portões subiram!");

            // Lift gates up!
            if (entranceGate != null) entranceGate.OpenGate();
            if (exitGate != null) exitGate.OpenGate();

            // Spawn Card Reward Chest
            CardRewardChest.CreateChest(chestSpawnPosition, transform);
        }

        public void ConfigureRoom(DungeonGate entrance, DungeonGate exit, List<Transform> spawns, Vector3 chestPos)
        {
            entranceGate = entrance;
            exitGate = exit;
            spawnPoints = spawns;
            chestSpawnPosition = chestPos;
        }
    }
}
