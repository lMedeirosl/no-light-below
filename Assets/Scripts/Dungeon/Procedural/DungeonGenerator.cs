using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using NoLightBelow.Environment;
using NoLightBelow.Player;

namespace NoLightBelow.Dungeon.Procedural
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfigSO config;
        [SerializeField] private NavMeshSurface navMeshSurface;
        [SerializeField] private LayerMask roomCollisionLayers = ~0;

        [Header("Starting Settings")]
        [SerializeField] private Transform dungeonParent;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private int initialRoomsToGenerate = 4;

        // Runtime Tracking
        private readonly List<RoomInstance> _activeRooms = new List<RoomInstance>();
        private readonly Queue<RoomConnector> _pendingExits = new Queue<RoomConnector>();
        private readonly Dictionary<string, Queue<GameObject>> _roomPool = new Dictionary<string, Queue<GameObject>>();

        private int _currentDepth = 0;
        private RoomInstance _currentActiveRoom;
        private bool _isGeneratingBatch = false;

        public static DungeonGenerator Instance { get; private set; }
        public IReadOnlyList<RoomInstance> ActiveRooms => _activeRooms;
        public int CurrentDepth => _currentDepth;

        private void Awake()
        {
            Instance = this;
            if (dungeonParent == null) dungeonParent = transform;
            if (navMeshSurface == null) navMeshSurface = GetComponent<NavMeshSurface>();
        }

        private void Start()
        {
            if (generateOnStart)
            {
                StartCoroutine(GenerateInitialDungeonRoutine());
            }
        }

        public IEnumerator GenerateInitialDungeonRoutine()
        {
            yield return null; // Wait for initial frame

            // 1. Generate or Register Spawn Room
            RoomInstance spawnRoom = CreateSpawnRoom(Vector3.zero);
            _activeRooms.Add(spawnRoom);
            _currentActiveRoom = spawnRoom;

            // Register exits from spawn room
            foreach (var exit in spawnRoom.ExitConnectors)
            {
                _pendingExits.Enqueue(exit);
            }

            // 2. Generate initial batch of rooms ahead
            for (int i = 0; i < initialRoomsToGenerate && _pendingExits.Count > 0; i++)
            {
                GenerateNextRoom();
            }

            // 3. Bake NavMesh for the generated layout
            RebuildNavMesh();
        }

        public bool GenerateNextRoom()
        {
            if (_pendingExits.Count == 0)
            {
                Debug.LogWarning("<color=#FFA500>[DungeonGenerator]</color> Nenhum conector de saída pendente para expandir!");
                return false;
            }

            RoomConnector exitConnector = _pendingExits.Dequeue();
            if (exitConnector == null || exitConnector.IsConnected) return false;

            _currentDepth++;
            RoomType preferredType = DetermineRoomTypeForDepth(_currentDepth);
            RoomDataSO selectedRoomData = config != null ? config.GetRandomWeightedRoom(_currentDepth, preferredType) : null;

            // Attempt to instantiate and place without overlapping
            int maxPlacementAttempts = 3;
            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                GameObject newRoomObj = GetOrCreateRoomObject(selectedRoomData);
                RoomInstance newRoom = newRoomObj.GetComponent<RoomInstance>();

                if (newRoom == null)
                {
                    newRoom = newRoomObj.AddComponent<RoomInstance>();
                }

                newRoom.Initialize(selectedRoomData, _currentDepth);

                // Align Entrance of new room to Exit of current room
                RoomConnector entrance = newRoom.EntranceConnector;
                if (entrance == null)
                {
                    Debug.LogError($"[DungeonGenerator] A sala {newRoomObj.name} não possui um RoomConnector de entrada!");
                    RecycleRoom(newRoom);
                    return false;
                }

                // Math: Align new room so it extends in the direction exitConnector is pointing
                Quaternion targetRotation = Quaternion.LookRotation(exitConnector.WorldForward, Vector3.up);

                // Math: Position so entrance connector matches exit connector position exactly
                Vector3 targetPosition = exitConnector.WorldPosition - (targetRotation * entrance.transform.localPosition);

                newRoomObj.transform.rotation = targetRotation;
                newRoomObj.transform.position = targetPosition;
                Physics.SyncTransforms();

                // Validate Overlap collision with other rooms
                if (IsRoomPlacementValid(newRoom, targetPosition, targetRotation))
                {
                    // Success! Connect doors
                    exitConnector.Connect(entrance);
                    entrance.Connect(exitConnector);

                    // Add room exits to pending queue
                    foreach (var exit in newRoom.ExitConnectors)
                    {
                        if (exit != null && !exit.IsConnected)
                        {
                            _pendingExits.Enqueue(exit);
                        }
                    }

                    _activeRooms.Add(newRoom);
                    newRoom.OnPlayerEnteredRoom.AddListener(HandlePlayerEnteredRoom);

                    return true;
                }

                // Overlap detected: recycle and retry
                RecycleRoom(newRoom);
            }

            Debug.LogWarning($"<color=#FFA500>[DungeonGenerator]</color> Não foi possível posicionar uma sala conectada em {exitConnector.WorldPosition} sem colisão.");
            return false;
        }

        private bool IsRoomPlacementValid(RoomInstance room, Vector3 position, Quaternion rotation)
        {
            Vector3 boundsSize = room.SourceData != null ? room.SourceData.ApproximateBoundsSize : new Vector3(24f, 5f, 26f);

            // Procedural room extends from Z=0 to Z=boundsSize.z in local coordinates
            Vector3 localCenter = new Vector3(0f, boundsSize.y * 0.5f, boundsSize.z * 0.5f);
            Vector3 center = position + (rotation * localCenter);

            // Bounds extents slightly reduced (42% instead of 50%) so adjacent doorway walls never cause false overlap
            Vector3 halfExtents = new Vector3(boundsSize.x * 0.42f, boundsSize.y * 0.42f, boundsSize.z * 0.42f);

            Physics.SyncTransforms();
            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, roomCollisionLayers, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                // Ignore own components and triggers
                if (hit.transform.IsChildOf(room.transform)) continue;
                if (hit.isTrigger) continue;

                // Check if hit belongs to another active room
                RoomInstance hitRoom = hit.GetComponentInParent<RoomInstance>();
                if (hitRoom != null && hitRoom != room)
                {
                    return false; // Overlap with another room detected!
                }
            }

            return true;
        }

        private void HandlePlayerEnteredRoom(RoomInstance enteredRoom)
        {
            _currentActiveRoom = enteredRoom;
            int roomIndex = _activeRooms.IndexOf(enteredRoom);

            // Stream new rooms ahead if near the end of the generated path
            int roomsAhead = _activeRooms.Count - 1 - roomIndex;
            int maxAhead = config != null ? config.MaxActiveRoomsAhead : 4;

            if (roomsAhead < maxAhead && !_isGeneratingBatch)
            {
                StartCoroutine(StreamRoomsAheadRoutine(maxAhead - roomsAhead));
            }

            // Cull / recycle old rooms behind the player (Object Pooling)
            int maxBehind = config != null ? config.MaxRoomsBehindToKeep : 2;
            if (roomIndex > maxBehind)
            {
                CullOldRoomsBehind(roomIndex - maxBehind);
            }
        }

        private IEnumerator StreamRoomsAheadRoutine(int countToSpawn)
        {
            _isGeneratingBatch = true;

            for (int i = 0; i < countToSpawn && _pendingExits.Count > 0; i++)
            {
                GenerateNextRoom();
                yield return null;
            }

            RebuildNavMesh();
            _isGeneratingBatch = false;
        }

        private void CullOldRoomsBehind(int upToIndex)
        {
            for (int i = 0; i < upToIndex && _activeRooms.Count > 1; i++)
            {
                RoomInstance oldRoom = _activeRooms[0];
                if (oldRoom != null && oldRoom.Type != RoomType.Spawn)
                {
                    _activeRooms.RemoveAt(0);
                    RecycleRoom(oldRoom);
                    Debug.Log($"<color=#708090>[Object Pool]</color> Sala '{oldRoom.name}' recolhida ao pool para economizar memória.");
                }
            }
        }

        // ================= Object Pooling =================

        private GameObject GetOrCreateRoomObject(RoomDataSO data)
        {
            string key = data != null ? data.RoomId : "DefaultProcedural";

            if (_roomPool.TryGetValue(key, out var queue) && queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            // Instantiate from prefab if provided, or build procedurally
            if (data != null && data.RoomPrefab != null)
            {
                GameObject instance = Instantiate(data.RoomPrefab, dungeonParent);
                instance.name = $"{data.RoomId}_{System.Guid.NewGuid().ToString().Substring(0, 4)}";
                return instance;
            }

            // Procedural construction fallback
            return BuildProceduralRoomGameObject(data, key);
        }

        private void RecycleRoom(RoomInstance room)
        {
            if (room == null) return;

            string key = room.SourceData != null ? room.SourceData.RoomId : "DefaultProcedural";
            room.ResetForPool();
            room.gameObject.SetActive(false);

            if (!_roomPool.TryGetValue(key, out var queue))
            {
                queue = new Queue<GameObject>();
                _roomPool[key] = queue;
            }

            queue.Enqueue(room.gameObject);
        }

        // ================= NavMesh Rebuilding =================

        public void RebuildNavMesh()
        {
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>() ?? FindAnyObjectByType<NavMeshSurface>();
            }

            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }
        }

        // ================= Room Type Rules =================

        private RoomType DetermineRoomTypeForDepth(int depth)
        {
            if (depth == 1) return RoomType.NormalCombat;

            // Every 5th room is an Elite or Event room
            if (depth % 5 == 0)
            {
                return Random.value < 0.6f ? RoomType.Elite : RoomType.Loot;
            }

            // Check config elite chance curve
            if (config != null && Random.value < config.GetEliteChance(depth))
            {
                return RoomType.Elite;
            }

            return RoomType.NormalCombat;
        }

        // ================= Procedural Geometry Builder Fallback =================

        private RoomInstance CreateSpawnRoom(Vector3 position)
        {
            GameObject spawnObj = new GameObject("Room_Spawn_Sanctuary");
            spawnObj.transform.SetParent(dungeonParent, false);
            spawnObj.transform.position = position;

            var roomInstance = spawnObj.AddComponent<RoomInstance>();
            roomInstance.Initialize(null, 0);

            Material floorMat = DungeonMaterialFactory.CreateDungeonFloorMaterial();
            Material wallMat = DungeonMaterialFactory.CreateStoneWallMaterial();
            Material ironMat = DungeonMaterialFactory.CreateRustyIronMaterial();
            Material flameMat = DungeonMaterialFactory.CreateTorchFlameMaterial();

            float width = 18f;
            float length = 18f;
            float height = 5f;

            // Floor & Ceiling
            CreateBox(spawnObj.transform, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(width, 1f, length), floorMat);
            CreateBox(spawnObj.transform, "Ceiling", new Vector3(0f, height + 0.5f, 0f), new Vector3(width, 1f, length), wallMat);

            // Walls (South, West, East)
            CreateBox(spawnObj.transform, "Wall_South", new Vector3(0f, height * 0.5f, -length * 0.5f), new Vector3(width, height, 1f), wallMat);
            CreateBox(spawnObj.transform, "Wall_West", new Vector3(-width * 0.5f, height * 0.5f, 0f), new Vector3(1f, height, length), wallMat);
            CreateBox(spawnObj.transform, "Wall_East", new Vector3(width * 0.5f, height * 0.5f, 0f), new Vector3(1f, height, length), wallMat);

            // North Wall with Doorway
            float doorWidth = 4.5f;
            float sideWidth = (width - doorWidth) * 0.5f;
            float sideOffset = (doorWidth + sideWidth) * 0.5f;
            CreateBox(spawnObj.transform, "Wall_North_Left", new Vector3(-sideOffset, height * 0.5f, length * 0.5f), new Vector3(sideWidth, height, 1f), wallMat);
            CreateBox(spawnObj.transform, "Wall_North_Right", new Vector3(sideOffset, height * 0.5f, length * 0.5f), new Vector3(sideWidth, height, 1f), wallMat);
            CreateBox(spawnObj.transform, "Wall_North_Lintel", new Vector3(0f, height - 0.4f, length * 0.5f), new Vector3(doorWidth, 0.8f, 1.4f), wallMat);

            // North Portcullis Gate
            var gate = CreatePortcullisGate(spawnObj.transform, new Vector3(0f, 0f, length * 0.5f), doorWidth, height, ironMat);

            // Exit Connector
            GameObject exitConnObj = new GameObject("Connector_Exit_North");
            exitConnObj.transform.SetParent(spawnObj.transform, false);
            exitConnObj.transform.localPosition = new Vector3(0f, 0f, length * 0.5f);
            exitConnObj.transform.localRotation = Quaternion.identity; // points North
            var exitConn = exitConnObj.AddComponent<RoomConnector>();
            exitConn.SetAsEntrance(false);
            exitConn.SetAssociatedGate(gate);

            // Stone Altar in Sanctuary
            CreateBox(spawnObj.transform, "Stone_Altar", new Vector3(0f, 0.45f, -2.5f), new Vector3(2.6f, 0.9f, 1.5f), wallMat);

            // Torches
            CreateTorch(spawnObj.transform, new Vector3(-sideWidth * 0.5f, 2.8f, length * 0.5f - 0.5f), Quaternion.identity, ironMat, flameMat);
            CreateTorch(spawnObj.transform, new Vector3(sideWidth * 0.5f, 2.8f, length * 0.5f - 0.5f), Quaternion.identity, ironMat, flameMat);
            CreateTorch(spawnObj.transform, new Vector3(-width * 0.5f + 0.5f, 2.8f, -2f), Quaternion.Euler(0f, 90f, 0f), ironMat, flameMat);
            CreateTorch(spawnObj.transform, new Vector3(width * 0.5f - 0.5f, 2.8f, -2f), Quaternion.Euler(0f, -90f, 0f), ironMat, flameMat);

            // Training Dummy
            CreateTrainingDummy(spawnObj.transform, new Vector3(0f, 0.1f, 3.2f));

            roomInstance.SetReferences(null, new List<RoomConnector> { exitConn }, new List<Transform>(), null);
            return roomInstance;
        }

        private GameObject BuildProceduralRoomGameObject(RoomDataSO data, string key)
        {
            GameObject roomObj = new GameObject(key);
            roomObj.transform.SetParent(dungeonParent, false);

            var rb = roomObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var roomInstance = roomObj.AddComponent<RoomInstance>();

            Material floorMat = DungeonMaterialFactory.CreateDungeonFloorMaterial();
            Material wallMat = DungeonMaterialFactory.CreateStoneWallMaterial();
            Material ironMat = DungeonMaterialFactory.CreateRustyIronMaterial();
            Material flameMat = DungeonMaterialFactory.CreateTorchFlameMaterial();

            float width = 24f;
            float length = 26f;
            float height = 5f;
            float doorWidth = 4.5f;
            float sideWidth = (width - doorWidth) * 0.5f;
            float sideOffset = (doorWidth + sideWidth) * 0.5f;

            // Floor & Ceiling
            CreateBox(roomObj.transform, "Floor", new Vector3(0f, -0.5f, length * 0.5f), new Vector3(width, 1f, length), floorMat);
            CreateBox(roomObj.transform, "Ceiling", new Vector3(0f, height + 0.5f, length * 0.5f), new Vector3(width, 1f, length), wallMat);

            // East & West Walls
            CreateBox(roomObj.transform, "Wall_West", new Vector3(-width * 0.5f, height * 0.5f, length * 0.5f), new Vector3(1f, height, length), wallMat);
            CreateBox(roomObj.transform, "Wall_East", new Vector3(width * 0.5f, height * 0.5f, length * 0.5f), new Vector3(1f, height, length), wallMat);

            // South Wall (Entrance Doorway)
            CreateBox(roomObj.transform, "Wall_South_Left", new Vector3(-sideOffset, height * 0.5f, 0f), new Vector3(sideWidth, height, 1f), wallMat);
            CreateBox(roomObj.transform, "Wall_South_Right", new Vector3(sideOffset, height * 0.5f, 0f), new Vector3(sideWidth, height, 1f), wallMat);
            CreateBox(roomObj.transform, "Wall_South_Lintel", new Vector3(0f, height - 0.4f, 0f), new Vector3(doorWidth, 0.8f, 1.4f), wallMat);

            // North Wall (Exit Doorway)
            CreateBox(roomObj.transform, "Wall_North_Left", new Vector3(-sideOffset, height * 0.5f, length), new Vector3(sideWidth, height, 1f), wallMat);
            CreateBox(roomObj.transform, "Wall_North_Right", new Vector3(sideOffset, height * 0.5f, length), new Vector3(sideWidth, height, 1f), wallMat);
            CreateBox(roomObj.transform, "Wall_North_Lintel", new Vector3(0f, height - 0.4f, length), new Vector3(doorWidth, 0.8f, 1.4f), wallMat);

            // Physical Gates
            var entranceGate = CreatePortcullisGate(roomObj.transform, new Vector3(0f, 0f, 0f), doorWidth, height, ironMat);
            var exitGate = CreatePortcullisGate(roomObj.transform, new Vector3(0f, 0f, length), doorWidth, height, ironMat);

            // Connectors
            GameObject entConnObj = new GameObject("Connector_Entrance_South");
            entConnObj.transform.SetParent(roomObj.transform, false);
            entConnObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            entConnObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // faces out of room (South)
            var entConn = entConnObj.AddComponent<RoomConnector>();
            entConn.SetAsEntrance(true);
            entConn.SetAssociatedGate(entranceGate);

            GameObject exitConnObj = new GameObject("Connector_Exit_North");
            exitConnObj.transform.SetParent(roomObj.transform, false);
            exitConnObj.transform.localPosition = new Vector3(0f, 0f, length);
            exitConnObj.transform.localRotation = Quaternion.identity; // faces out of room (North)
            var exitConn = exitConnObj.AddComponent<RoomConnector>();
            exitConn.SetAsEntrance(false);
            exitConn.SetAssociatedGate(exitGate);

            // 4 Stone Pillars with Torches
            CreatePillar(roomObj.transform, new Vector3(6f, height * 0.5f, 7f), height, wallMat);
            CreateTorch(roomObj.transform, new Vector3(5.3f, 2.6f, 7f), Quaternion.Euler(0f, -90f, 0f), ironMat, flameMat);

            CreatePillar(roomObj.transform, new Vector3(-6f, height * 0.5f, 7f), height, wallMat);
            CreateTorch(roomObj.transform, new Vector3(-5.3f, 2.6f, 7f), Quaternion.Euler(0f, 90f, 0f), ironMat, flameMat);

            CreatePillar(roomObj.transform, new Vector3(6f, height * 0.5f, length - 7f), height, wallMat);
            CreateTorch(roomObj.transform, new Vector3(5.3f, 2.6f, length - 7f), Quaternion.Euler(0f, -90f, 0f), ironMat, flameMat);

            CreatePillar(roomObj.transform, new Vector3(-6f, height * 0.5f, length - 7f), height, wallMat);
            CreateTorch(roomObj.transform, new Vector3(-5.3f, 2.6f, length - 7f), Quaternion.Euler(0f, 90f, 0f), ironMat, flameMat);

            // Trigger for player entrance
            GameObject triggerObj = new GameObject("Trigger_PlayerEnter");
            triggerObj.transform.SetParent(roomObj.transform, false);
            triggerObj.transform.localPosition = new Vector3(0f, 1.5f, 3.5f);
            var boxCol = triggerObj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(doorWidth * 1.5f, 3.5f, 4f);
            var relay = triggerObj.AddComponent<RoomTriggerRelay>();
            relay.parentRoom = roomInstance;

            // Spawns
            var spawns = new List<Transform>();
            spawns.Add(CreateMarker(roomObj.transform, new Vector3(-5f, 0.1f, length * 0.5f), "Spawn_1"));
            spawns.Add(CreateMarker(roomObj.transform, new Vector3(5f, 0.1f, length * 0.5f), "Spawn_2"));
            spawns.Add(CreateMarker(roomObj.transform, new Vector3(0f, 0.1f, length * 0.75f), "Spawn_3"));

            Transform chestPoint = CreateMarker(roomObj.transform, new Vector3(0f, 0.1f, length * 0.5f), "Chest_Point");

            roomInstance.SetReferences(entConn, new List<RoomConnector> { exitConn }, spawns, chestPoint);
            return roomObj;
        }

        private static DungeonGate CreatePortcullisGate(Transform parent, Vector3 localPos, float width, float height, Material ironMat)
        {
            GameObject gateRoot = new GameObject("Portcullis_Gate");
            gateRoot.transform.SetParent(parent, false);
            gateRoot.transform.localPosition = localPos;

            GameObject barsObj = new GameObject("Portcullis_MovingBars");
            barsObj.transform.SetParent(gateRoot.transform, false);
            barsObj.transform.localPosition = Vector3.zero;

            int barCount = 7;
            float step = width / (barCount + 1);

            for (int i = 1; i <= barCount; i++)
            {
                float x = -width * 0.5f + (i * step);
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bar.name = $"Bar_{i}";
                bar.transform.SetParent(barsObj.transform, false);
                bar.transform.localPosition = new Vector3(x, height * 0.4f, 0f);
                bar.transform.localScale = new Vector3(0.09f, height * 0.4f, 0.09f);
                bar.GetComponent<Renderer>().sharedMaterial = ironMat;
                DestroyImmediate(bar.GetComponent<Collider>());
            }

            GameObject beam1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam1.name = "CrossBeam_1";
            beam1.transform.SetParent(barsObj.transform, false);
            beam1.transform.localPosition = new Vector3(0f, height * 0.25f, 0f);
            beam1.transform.localScale = new Vector3(width * 0.95f, 0.12f, 0.14f);
            beam1.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(beam1.GetComponent<Collider>());

            GameObject beam2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam2.name = "CrossBeam_2";
            beam2.transform.SetParent(barsObj.transform, false);
            beam2.transform.localPosition = new Vector3(0f, height * 0.55f, 0f);
            beam2.transform.localScale = new Vector3(width * 0.95f, 0.12f, 0.14f);
            beam2.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(beam2.GetComponent<Collider>());

            var gateCol = barsObj.AddComponent<BoxCollider>();
            gateCol.center = new Vector3(0f, height * 0.4f, 0f);
            gateCol.size = new Vector3(width, height * 0.8f, 0.4f);

            var gateComp = gateRoot.AddComponent<DungeonGate>();
            gateComp.Initialize(barsObj.transform, 0f, height * 0.85f);
            barsObj.transform.localPosition = new Vector3(0f, height * 0.85f, 0f); // starts open

            return gateComp;
        }

        private static void CreateTorch(Transform parent, Vector3 localPos, Quaternion localRot, Material ironMat, Material flameMat)
        {
            GameObject torch = new GameObject("Dungeon_Torch");
            torch.transform.SetParent(parent, false);
            torch.transform.localPosition = localPos;
            torch.transform.localRotation = localRot;

            GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bracket.name = "Sconce_Arm";
            bracket.transform.SetParent(torch.transform, false);
            bracket.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            bracket.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            bracket.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            bracket.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(bracket.GetComponent<Collider>());

            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Torch_Bowl";
            bowl.transform.SetParent(torch.transform, false);
            bowl.transform.localPosition = new Vector3(0f, 0.24f, 0.32f);
            bowl.transform.localScale = new Vector3(0.24f, 0.12f, 0.24f);
            bowl.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(bowl.GetComponent<Collider>());

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Torch_Flame";
            flame.transform.SetParent(bowl.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            flame.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
            flame.GetComponent<Renderer>().sharedMaterial = flameMat;
            DestroyImmediate(flame.GetComponent<Collider>());

            GameObject lightObj = new GameObject("Torch_PointLight");
            lightObj.transform.SetParent(bowl.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.65f, 0.24f);
            pLight.intensity = 2.4f;
            pLight.range = 10f;
            pLight.shadows = LightShadows.None;

            lightObj.AddComponent<TorchFlicker>();
        }

        private static GameObject CreateTrainingDummy(Transform parent, Vector3 localPos)
        {
            GameObject dummy = new GameObject("Training_Dummy_Target");
            dummy.transform.SetParent(parent, false);
            dummy.transform.localPosition = localPos;

            Material woodMat = DungeonMaterialFactory.CreateDummyMaterial();

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Dummy_Post";
            post.transform.SetParent(dummy.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            post.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
            post.GetComponent<Renderer>().sharedMaterial = woodMat;

            GameObject arms = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arms.name = "Dummy_Arms";
            arms.transform.SetParent(post.transform, false);
            arms.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            arms.transform.localScale = new Vector3(2.4f, 0.3f, 0.3f);
            arms.GetComponent<Renderer>().sharedMaterial = woodMat;
            DestroyImmediate(arms.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Dummy_Head";
            head.transform.SetParent(post.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = woodMat;
            DestroyImmediate(head.GetComponent<Collider>());

            var dummyComp = dummy.AddComponent<Combat.TrainingDummy>();
            dummyComp.SetReferences(post.transform, post.GetComponent<Renderer>());

            var capsuleCol = dummy.AddComponent<CapsuleCollider>();
            capsuleCol.height = 2.2f;
            capsuleCol.radius = 0.6f;
            capsuleCol.center = new Vector3(0f, 1.1f, 0f);

            return dummy;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private static void CreatePillar(Transform parent, Vector3 pos, float height, Material mat)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.localPosition = pos;
            pillar.transform.localScale = new Vector3(1.3f, height * 0.5f, 1.3f);
            pillar.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Transform CreateMarker(Transform parent, Vector3 localPos, string name)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPos;
            return marker.transform;
        }
    }
}
