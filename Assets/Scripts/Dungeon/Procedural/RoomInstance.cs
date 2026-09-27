using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NoLightBelow.Enemies;
using NoLightBelow.Player;
using NoLightBelow.Dungeon;

namespace NoLightBelow.Dungeon.Procedural
{
    [SelectionBase]
    public class RoomInstance : MonoBehaviour
    {
        [Header("Room Identity")]
        [SerializeField] private RoomType roomType = RoomType.NormalCombat;
        [SerializeField] private RoomState currentState = RoomState.Unvisited;
        [SerializeField] private int depthLevel = 1;

        [Header("Connectors & Gates")]
        [SerializeField] private RoomConnector entranceConnector;
        [SerializeField] private List<RoomConnector> exitConnectors = new List<RoomConnector>();

        [Header("Spawn Markers")]
        [SerializeField] private List<Transform> enemySpawnPoints = new List<Transform>();
        [SerializeField] private Transform chestSpawnPoint;

        [Header("Combat & Encounter Rules")]
        [SerializeField] private int totalWaves = 2;
        [SerializeField] private int enemiesPerWave = 2;

        [Header("Unity Events for Feedback & Audio")]
        public UnityEvent OnCombatStarted;
        public UnityEvent OnRoomCleared;
        public UnityEvent<RoomInstance> OnPlayerEnteredRoom;

        private readonly List<CryptSkeletonAI> _activeEnemies = new List<CryptSkeletonAI>();
        private int _currentWave = 0;
        private bool _isEncounterActive = false;
        private RoomDataSO _sourceData;

        public RoomType Type => roomType;
        public RoomState State => currentState;
        public int DepthLevel => depthLevel;
        public RoomConnector EntranceConnector => entranceConnector;
        public IReadOnlyList<RoomConnector> ExitConnectors => exitConnectors;
        public RoomDataSO SourceData => _sourceData;

        public void Initialize(RoomDataSO data, int depth, float healthMult = 1f, float damageMult = 1f)
        {
            _sourceData = data;
            depthLevel = depth;

            if (data != null)
            {
                roomType = data.Type;
                totalWaves = data.TotalWaves;
                enemiesPerWave = data.BaseEnemiesPerWave;
            }

            currentState = RoomState.Unvisited;
            _isEncounterActive = false;
            _currentWave = 0;

            FindAndRegisterConnectors();
        }

        public void FindAndRegisterConnectors()
        {
            RoomConnector[] all = GetComponentsInChildren<RoomConnector>(true);
            exitConnectors.Clear();
            entranceConnector = null;

            foreach (var conn in all)
            {
                if (conn.IsEntrance)
                {
                    entranceConnector = conn;
                }
                else
                {
                    exitConnectors.Add(conn);
                }
            }

            // Fallback: If no explicit entrance designated, first connector is entrance
            if (entranceConnector == null && all.Length > 0)
            {
                entranceConnector = all[0];
                entranceConnector.SetAsEntrance(true);
            }
        }

        public Bounds GetCalculatedBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(transform.position, _sourceData != null ? _sourceData.ApproximateBoundsSize : new Vector3(20f, 6f, 20f));
            }

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                // Ignore small props or weapon models
                if (renderers[i].GetComponent<Collider>() != null)
                {
                    combined.Encapsulate(renderers[i].bounds);
                }
            }

            return combined;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentState != RoomState.Unvisited) return;

            var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                HandlePlayerEntered();
            }
        }

        public void HandlePlayerEntered()
        {
            OnPlayerEnteredRoom?.Invoke(this);

            if (roomType == RoomType.Spawn || roomType == RoomType.Loot || roomType == RoomType.Event)
            {
                // Safe rooms auto-clear
                currentState = RoomState.Cleared;
                OnRoomCleared?.Invoke();
                return;
            }

            StartCombatEncounter();
        }

        public void StartCombatEncounter()
        {
            if (_isEncounterActive || currentState == RoomState.Cleared) return;

            _isEncounterActive = true;
            currentState = RoomState.ActiveCombat;
            OnCombatStarted?.Invoke();

            Debug.Log($"<color=#DC143C>[Câmara {depthLevel}]</color> Portas trancadas! Combate iniciado.");

            // Slam all gates shut!
            LockAllGates();

            StartCoroutine(SpawnWaveRoutine(1.0f));
        }

        private void LockAllGates()
        {
            if (entranceConnector != null && entranceConnector.AssociatedGate != null)
            {
                entranceConnector.AssociatedGate.CloseGate();
            }

            foreach (var exit in exitConnectors)
            {
                if (exit != null && exit.AssociatedGate != null)
                {
                    exit.AssociatedGate.CloseGate();
                }
            }
        }

        private void UnlockAllGates()
        {
            if (entranceConnector != null && entranceConnector.AssociatedGate != null)
            {
                entranceConnector.AssociatedGate.OpenGate();
            }

            foreach (var exit in exitConnectors)
            {
                if (exit != null && exit.AssociatedGate != null)
                {
                    exit.AssociatedGate.OpenGate();
                }
            }
        }

        private IEnumerator SpawnWaveRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            _currentWave++;
            int countToSpawn = enemiesPerWave + (_currentWave - 1);
            Debug.Log($"<color=#DC143C>[Onda {_currentWave}/{totalWaves}]</color> Invocando {countToSpawn} Esqueletos da Cripta!");

            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos;
                if (enemySpawnPoints.Count > 0)
                {
                    spawnPos = enemySpawnPoints[i % enemySpawnPoints.Count].position + UnityEngine.Random.insideUnitSphere * 0.4f;
                    spawnPos.y = 0.1f;
                }
                else
                {
                    spawnPos = transform.position + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0.1f, UnityEngine.Random.Range(-4f, 4f));
                }

                GameObject skeleton = SkeletonModelBuilder.CreateSkeleton(spawnPos);
                var ai = skeleton.GetComponent<CryptSkeletonAI>();
                if (ai != null)
                {
                    _activeEnemies.Add(ai);
                    ai.OnSkeletonDied += HandleEnemyDefeated;
                }

                yield return new WaitForSeconds(0.35f);
            }
        }

        private void HandleEnemyDefeated(CryptSkeletonAI enemy)
        {
            _activeEnemies.Remove(enemy);

            if (_activeEnemies.Count == 0)
            {
                if (_currentWave < totalWaves)
                {
                    StartCoroutine(SpawnWaveRoutine(1.4f));
                }
                else
                {
                    ClearRoom();
                }
            }
        }

        private void ClearRoom()
        {
            _isEncounterActive = false;
            currentState = RoomState.Cleared;
            Debug.Log($"<color=#32CD32>[Câmara {depthLevel} Purificada!]</color> Portões abertos e recompensa liberada.");

            UnlockAllGates();

            // Spawn Card Reward Chest
            Vector3 chestPos = chestSpawnPoint != null ? chestSpawnPoint.position : transform.position + Vector3.up * 0.05f;
            CardRewardChest.CreateChest(chestPos, transform);

            OnRoomCleared?.Invoke();
        }

        public void ResetForPool()
        {
            currentState = RoomState.Unvisited;
            _isEncounterActive = false;
            _currentWave = 0;
            _activeEnemies.Clear();

            // Destroy any remaining chests or leftovers
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.Contains("Chest") || child.name.Contains("Skeleton"))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void SetReferences(RoomConnector entrance, List<RoomConnector> exits, List<Transform> spawns, Transform chestPoint)
        {
            entranceConnector = entrance;
            exitConnectors = exits ?? new List<RoomConnector>();
            enemySpawnPoints = spawns ?? new List<Transform>();
            chestSpawnPoint = chestPoint;
        }
    }

    public class RoomTriggerRelay : MonoBehaviour
    {
        public RoomInstance parentRoom;

        private void OnTriggerEnter(Collider other)
        {
            if (parentRoom != null)
            {
                var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    parentRoom.HandlePlayerEntered();
                }
            }
        }
    }
}
