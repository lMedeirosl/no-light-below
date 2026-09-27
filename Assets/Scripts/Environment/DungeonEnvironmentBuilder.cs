using System.Collections.Generic;
using UnityEngine;
using NoLightBelow.Dungeon;

namespace NoLightBelow.Environment
{
    public class DungeonEnvironmentBuilder : MonoBehaviour
    {
        [Header("Room Dimensions")]
        [SerializeField] private float wallHeight = 5f;
        [SerializeField] private float wallThickness = 1.0f;

        public struct DungeonDuoRooms
        {
            public GameObject SpawnRoom;
            public GameObject CombatRoom;
            public DungeonGate IntermediateGate;
            public DungeonGate ExitGate;
            public RoomCombatController CombatController;
        }

        public DungeonDuoRooms BuildDuoRoomDungeon(Transform parent)
        {
            DungeonDuoRooms duo = new DungeonDuoRooms();

            Material floorMat = DungeonMaterialFactory.CreateDungeonFloorMaterial();
            Material wallMat = DungeonMaterialFactory.CreateStoneWallMaterial();
            Material ironMat = DungeonMaterialFactory.CreateRustyIronMaterial();
            Material flameMat = DungeonMaterialFactory.CreateTorchFlameMaterial();

            // =========================================================================
            // 1. SALA 1: SALA DE NASCIMENTO (SPAWN ROOM) - Centro em Z = -10
            // =========================================================================
            GameObject spawnRoom = new GameObject("Sala_1_Nascimento");
            if (parent != null) spawnRoom.transform.SetParent(parent, false);
            duo.SpawnRoom = spawnRoom;

            float sWidth = 18f;
            float sLength = 18f;
            float sCenterZ = -10f;

            // Floor & Ceiling
            CreateBox(spawnRoom.transform, "Spawn_Floor", new Vector3(0f, -0.5f, sCenterZ), new Vector3(sWidth, 1f, sLength), floorMat);
            CreateBox(spawnRoom.transform, "Spawn_Ceiling", new Vector3(0f, wallHeight + 0.5f, sCenterZ), new Vector3(sWidth, 1f, sLength), wallMat);

            // South Wall (Back) with Altar
            CreateBox(spawnRoom.transform, "Wall_South", new Vector3(0f, wallHeight * 0.5f, sCenterZ - (sLength * 0.5f)), new Vector3(sWidth, wallHeight, wallThickness), wallMat);
            // East & West Walls
            CreateBox(spawnRoom.transform, "Wall_West", new Vector3(-sWidth * 0.5f, wallHeight * 0.5f, sCenterZ), new Vector3(wallThickness, wallHeight, sLength), wallMat);
            CreateBox(spawnRoom.transform, "Wall_East", new Vector3(sWidth * 0.5f, wallHeight * 0.5f, sCenterZ), new Vector3(wallThickness, wallHeight, sLength), wallMat);

            // North Wall with Archway Doorway (left and right wall sections leaving 4.5m opening in center)
            float openingWidth = 4.5f;
            float sideWallWidth = (sWidth - openingWidth) * 0.5f;
            float sideWallOffset = (openingWidth + sideWallWidth) * 0.5f;
            float doorZ = sCenterZ + (sLength * 0.5f); // Z = -1

            CreateBox(spawnRoom.transform, "Wall_North_Left", new Vector3(-sideWallOffset, wallHeight * 0.5f, doorZ), new Vector3(sideWallWidth, wallHeight, wallThickness), wallMat);
            CreateBox(spawnRoom.transform, "Wall_North_Right", new Vector3(sideWallOffset, wallHeight * 0.5f, doorZ), new Vector3(sideWallWidth, wallHeight, wallThickness), wallMat);
            // Lintel over door
            CreateBox(spawnRoom.transform, "Door_Lintel", new Vector3(0f, wallHeight - 0.4f, doorZ), new Vector3(openingWidth, 0.8f, wallThickness + 0.4f), wallMat);

            // Spawn Room Torches
            CreateTorch(spawnRoom.transform, new Vector3(-sWidth * 0.5f + 0.6f, 2.2f, sCenterZ), Quaternion.Euler(0f, 90f, 0f), ironMat, flameMat);
            CreateTorch(spawnRoom.transform, new Vector3(sWidth * 0.5f - 0.6f, 2.2f, sCenterZ), Quaternion.Euler(0f, -90f, 0f), ironMat, flameMat);

            // Dark Altar at South Wall of Spawn
            CreateAltar(spawnRoom.transform, new Vector3(0f, 0f, sCenterZ - (sLength * 0.5f) + 2.5f), wallMat, ironMat, flameMat);

            // =========================================================================
            // 2. PORTÃO INTERMEDIÁRIO (DUNGEON GATE) - Z = -1
            // =========================================================================
            duo.IntermediateGate = CreatePortcullisGate(parent != null ? parent : spawnRoom.transform, new Vector3(0f, 0f, doorZ), openingWidth, wallHeight, ironMat);

            // =========================================================================
            // 3. SALA 2: CÂMARA DE COMBATE (COMBAT ROOM) - Centro em Z = 15
            // =========================================================================
            GameObject combatRoom = new GameObject("Sala_2_CamaraDeCombate");
            if (parent != null) combatRoom.transform.SetParent(parent, false);
            duo.CombatRoom = combatRoom;

            float cWidth = 24f;
            float cLength = 30f;
            float cCenterZ = 14f;

            // Floor & Ceiling
            CreateBox(combatRoom.transform, "Combat_Floor", new Vector3(0f, -0.5f, cCenterZ), new Vector3(cWidth, 1f, cLength), floorMat);
            CreateBox(combatRoom.transform, "Combat_Ceiling", new Vector3(0f, wallHeight + 0.5f, cCenterZ), new Vector3(cWidth, 1f, cLength), wallMat);

            // East & West Walls
            CreateBox(combatRoom.transform, "Combat_Wall_West", new Vector3(-cWidth * 0.5f, wallHeight * 0.5f, cCenterZ), new Vector3(wallThickness, wallHeight, cLength), wallMat);
            CreateBox(combatRoom.transform, "Combat_Wall_East", new Vector3(cWidth * 0.5f, wallHeight * 0.5f, cCenterZ), new Vector3(wallThickness, wallHeight, cLength), wallMat);

            // South Wall (facing door)
            float cSouthSideWidth = (cWidth - openingWidth) * 0.5f;
            float cSouthSideOffset = (openingWidth + cSouthSideWidth) * 0.5f;
            CreateBox(combatRoom.transform, "Combat_Wall_South_Left", new Vector3(-cSouthSideOffset, wallHeight * 0.5f, doorZ), new Vector3(cSouthSideWidth, wallHeight, wallThickness), wallMat);
            CreateBox(combatRoom.transform, "Combat_Wall_South_Right", new Vector3(cSouthSideOffset, wallHeight * 0.5f, doorZ), new Vector3(cSouthSideWidth, wallHeight, wallThickness), wallMat);

            // North Wall (Far exit)
            float exitZ = cCenterZ + (cLength * 0.5f);
            CreateBox(combatRoom.transform, "Combat_Wall_North_Left", new Vector3(-cSouthSideOffset, wallHeight * 0.5f, exitZ), new Vector3(cSouthSideWidth, wallHeight, wallThickness), wallMat);
            CreateBox(combatRoom.transform, "Combat_Wall_North_Right", new Vector3(cSouthSideOffset, wallHeight * 0.5f, exitZ), new Vector3(cSouthSideWidth, wallHeight, wallThickness), wallMat);
            CreateBox(combatRoom.transform, "Combat_Exit_Lintel", new Vector3(0f, wallHeight - 0.4f, exitZ), new Vector3(openingWidth, 0.8f, wallThickness + 0.4f), wallMat);

            // Exit Gate (Leading deeper)
            duo.ExitGate = CreatePortcullisGate(combatRoom.transform, new Vector3(0f, 0f, exitZ), openingWidth, wallHeight, ironMat);
            duo.ExitGate.CloseGate(); // Starts closed until cleared!

            // 4 Pillars in Combat Chamber
            float pX = 6.5f;
            float pZOffset = 7.5f;
            CreatePillar(combatRoom.transform, new Vector3(pX, wallHeight * 0.5f, cCenterZ + pZOffset), wallHeight, wallMat);
            CreatePillar(combatRoom.transform, new Vector3(-pX, wallHeight * 0.5f, cCenterZ + pZOffset), wallHeight, wallMat);
            CreatePillar(combatRoom.transform, new Vector3(pX, wallHeight * 0.5f, cCenterZ - pZOffset), wallHeight, wallMat);
            CreatePillar(combatRoom.transform, new Vector3(-pX, wallHeight * 0.5f, cCenterZ - pZOffset), wallHeight, wallMat);

            // Combat Chamber Torches
            CreateTorch(combatRoom.transform, new Vector3(-cWidth * 0.5f + 0.6f, 2.2f, cCenterZ - 6f), Quaternion.Euler(0f, 90f, 0f), ironMat, flameMat);
            CreateTorch(combatRoom.transform, new Vector3(cWidth * 0.5f - 0.6f, 2.2f, cCenterZ - 6f), Quaternion.Euler(0f, -90f, 0f), ironMat, flameMat);
            CreateTorch(combatRoom.transform, new Vector3(-cWidth * 0.5f + 0.6f, 2.2f, cCenterZ + 6f), Quaternion.Euler(0f, 90f, 0f), ironMat, flameMat);
            CreateTorch(combatRoom.transform, new Vector3(cWidth * 0.5f - 0.6f, 2.2f, cCenterZ + 6f), Quaternion.Euler(0f, -90f, 0f), ironMat, flameMat);

            // Hanging Chains & Cages
            CreateHangingChain(combatRoom.transform, new Vector3(-3.5f, wallHeight, cCenterZ + 3.5f), 2.2f, ironMat);
            CreateHangingChain(combatRoom.transform, new Vector3(3.5f, wallHeight, cCenterZ - 3.5f), 1.8f, ironMat);

            // Trigger & Combat Controller on Sala 2
            var combatTriggerObj = new GameObject("Trigger_CombatEncounter");
            combatTriggerObj.transform.SetParent(combatRoom.transform, false);
            combatTriggerObj.transform.localPosition = new Vector3(0f, 1.5f, 4f);
            var trigCol = combatTriggerObj.AddComponent<BoxCollider>();
            trigCol.isTrigger = true;
            trigCol.size = new Vector3(openingWidth * 1.5f, 4f, 5f);

            var combatCtrl = combatTriggerObj.AddComponent<RoomCombatController>();
            duo.CombatController = combatCtrl;

            // Spawn points for skeletons in Sala 2
            var spawns = new List<Transform>();
            spawns.Add(CreateSpawnPoint(combatRoom.transform, new Vector3(-5f, 0.1f, cCenterZ + 2f), "Spawn_Left"));
            spawns.Add(CreateSpawnPoint(combatRoom.transform, new Vector3(5f, 0.1f, cCenterZ + 2f), "Spawn_Right"));
            spawns.Add(CreateSpawnPoint(combatRoom.transform, new Vector3(0f, 0.1f, cCenterZ + 8f), "Spawn_Center"));

            combatCtrl.ConfigureRoom(duo.IntermediateGate, duo.ExitGate, spawns, new Vector3(0f, 0f, cCenterZ));

            // Apply macabre lighting
            ApplyAtmosphere();

            return duo;
        }

        private DungeonGate CreatePortcullisGate(Transform parent, Vector3 pos, float width, float height, Material ironMat)
        {
            GameObject gateRoot = new GameObject("Dungeon_Portcullis_Gate");
            gateRoot.transform.SetParent(parent, false);
            gateRoot.transform.position = pos;

            // Moving Portcullis Bars
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

            // Horizontal crossbeams
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

            // Physical Gate Collider
            var gateCol = barsObj.AddComponent<BoxCollider>();
            gateCol.center = new Vector3(0f, height * 0.4f, 0f);
            gateCol.size = new Vector3(width, height * 0.8f, 0.4f);

            var gateComp = gateRoot.AddComponent<DungeonGate>();
            gateComp.Initialize(barsObj.transform, 0f, height * 0.85f);

            // Starts open
            barsObj.transform.localPosition = new Vector3(0f, height * 0.85f, 0f);

            return gateComp;
        }

        private Transform CreateSpawnPoint(Transform parent, Vector3 localPos, string name)
        {
            GameObject sp = new GameObject(name);
            sp.transform.SetParent(parent, false);
            sp.transform.localPosition = localPos;
            return sp.transform;
        }

        private GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            return obj;
        }

        private void CreatePillar(Transform parent, Vector3 pos, float height, Material mat)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Stone_Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(1.4f, height * 0.5f, 1.4f);
            pillar.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Pillar_Cap";
            cap.transform.SetParent(pillar.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            cap.transform.localScale = new Vector3(1.3f, 0.15f, 1.3f);
            cap.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private void CreateTorch(Transform parent, Vector3 pos, Quaternion rot, Material ironMat, Material flameMat)
        {
            GameObject torch = new GameObject("Dungeon_Torch");
            torch.transform.SetParent(parent, false);
            torch.transform.position = pos;
            torch.transform.rotation = rot;

            GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bracket.name = "Sconce_Arm";
            bracket.transform.SetParent(torch.transform, false);
            bracket.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            bracket.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            bracket.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            bracket.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(bracket.GetComponent<Collider>());

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame_Ember";
            flame.transform.SetParent(torch.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.28f, 0.38f);
            flame.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
            flame.GetComponent<Renderer>().sharedMaterial = flameMat;
            DestroyImmediate(flame.GetComponent<Collider>());

            GameObject lightObj = new GameObject("Torch_PointLight");
            lightObj.transform.SetParent(flame.transform, false);
            lightObj.transform.localPosition = Vector3.zero;

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.52f, 0.18f);
            pLight.intensity = 3.2f;
            pLight.range = 15f;
            pLight.shadows = LightShadows.None;

            lightObj.AddComponent<TorchFlicker>();
        }

        private void CreateHangingChain(Transform parent, Vector3 startPos, float length, Material ironMat)
        {
            GameObject chain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chain.name = "Hanging_Chain";
            chain.transform.SetParent(parent, false);
            chain.transform.position = startPos - new Vector3(0f, length * 0.5f, 0f);
            chain.transform.localScale = new Vector3(0.06f, length * 0.5f, 0.06f);
            chain.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(chain.GetComponent<Collider>());

            GameObject cage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cage.name = "Gibbet_IronCage";
            cage.transform.SetParent(chain.transform, false);
            cage.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            cage.transform.localScale = new Vector3(12f, 1.2f, 12f);
            cage.GetComponent<Renderer>().sharedMaterial = ironMat;
            DestroyImmediate(cage.GetComponent<Collider>());
        }

        private void CreateAltar(Transform parent, Vector3 pos, Material stoneMat, Material ironMat, Material flameMat)
        {
            GameObject altar = new GameObject("Ominous_Altar");
            altar.transform.SetParent(parent, false);
            altar.transform.position = pos;

            GameObject baseSlab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseSlab.name = "Altar_Base";
            baseSlab.transform.SetParent(altar.transform, false);
            baseSlab.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            baseSlab.transform.localScale = new Vector3(3.4f, 0.9f, 1.8f);
            baseSlab.GetComponent<Renderer>().sharedMaterial = stoneMat;

            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Altar_Plate";
            plate.transform.SetParent(altar.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            plate.transform.localScale = new Vector3(3.8f, 0.15f, 2.1f);
            plate.GetComponent<Renderer>().sharedMaterial = ironMat;

            CreateCandle(altar.transform, new Vector3(1.4f, 1.15f, 0f), flameMat);
            CreateCandle(altar.transform, new Vector3(-1.4f, 1.15f, 0f), flameMat);
        }

        private void CreateCandle(Transform parent, Vector3 pos, Material flameMat)
        {
            GameObject candle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            candle.name = "Ritual_Candle";
            candle.transform.SetParent(parent, false);
            candle.transform.localPosition = pos;
            candle.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            DestroyImmediate(candle.GetComponent<Collider>());

            GameObject lightObj = new GameObject("Candle_Light");
            lightObj.transform.SetParent(candle.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            Light cLight = lightObj.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.color = new Color(0.95f, 0.35f, 0.15f);
            cLight.intensity = 1.6f;
            cLight.range = 5.5f;
            cLight.shadows = LightShadows.Soft;

            lightObj.AddComponent<TorchFlicker>();
        }

        public void ApplyAtmosphere()
        {
            Light[] allLights = Object.FindObjectsByType<Light>();
            foreach (var l in allLights)
            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = 0.02f;
                    l.color = new Color(0.05f, 0.05f, 0.08f);
                }
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.012f, 0.012f, 0.016f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.008f, 0.009f, 0.012f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.040f;

            // Apply URP Global Post-Processing Profile (Bloom, Vignette, ACES, Cold Wash)
            DungeonPostProcessingFactory.CreateOrUpdateGlobalVolume(transform);
        }
    }
}
