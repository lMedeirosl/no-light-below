#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.AI.Navigation;
using NoLightBelow.Cards;
using NoLightBelow.Combat;
using NoLightBelow.Core;
using NoLightBelow.Dungeon;
using NoLightBelow.Environment;
using NoLightBelow.Player;
using NoLightBelow.UI;
using NoLightBelow.Dungeon.Procedural;
using UnityEngine.Rendering.Universal;

namespace NoLightBelow.Editor
{
    public static class NoLightBelowSetupWizard
    {
        [MenuItem("Tools/No Light Below/Build Playable Test Arena (Dual Room + Gate)", false, 1)]
        [MenuItem("Tools/No Light Below/Build Playable Test Arena", false, 2)]
        public static void BuildPlayableTestArena()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build No Light Below Arena");

            // 0. Clean previous setups to avoid duplicate listeners, cameras, or players
            CleanupPreviousSetup();

            // 1. Root Container
            GameObject arenaRoot = new GameObject("--- NO LIGHT BELOW TEST ARENA ---");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena Root");

            // 2. Build 3D Dual Rooms (Spawn Room + Portcullis Gate + Combat Chamber)
            var envBuilder = arenaRoot.AddComponent<DungeonEnvironmentBuilder>();
            var duo = envBuilder.BuildDuoRoomDungeon(arenaRoot.transform);

            // 3. Bake NavMesh on the dungeon floors and obstacles
            var navSurface = arenaRoot.AddComponent<NavMeshSurface>();
            navSurface.BuildNavMesh();

            // 4. Create Player Character in Sala 1 (Sala de Nascimento)
            GameObject player = CreatePlayerRig(arenaRoot.transform);
            player.transform.position = new Vector3(0f, 0.1f, -14f);

            // 5. Configure Camera
            SetupCamera(player.transform);

            // 6. Create Training Dummy in Sala 1 (Spawn Room)
            GameObject dummy = CreateTrainingDummy(duo.SpawnRoom.transform);
            dummy.transform.position = new Vector3(0f, 0.1f, -6f);

            // 7. Generate Card ScriptableObjects
            List<CardData> cardPool = CreateOrLoadSampleCards();

            // 8. Create Canvas UI (HUD + Card Reward Modal)
            CreateUI(player.GetComponent<PlayerStats>(), cardPool);

            // Selection and Ping
            Selection.activeGameObject = player;
            EditorUtility.DisplayDialog("No Light Below", 
                "Masmorra com 2 Salas Conectadas construída com sucesso!\n\n" +
                "📍 SALA 1 (NASCIMENTO):\n" +
                "• Seu cavaleiro desperta aqui em segurança com o altar e o boneco de treino.\n\n" +
                "🚪 PORTÃO DE FERRO INTERMEDIÁRIO:\n" +
                "• Começa aberto para você atravessar.\n\n" +
                "⚔️ SALA 2 (CÂMARA DE COMBATE):\n" +
                "• Ao entrar na Sala 2, o portão cai e se tranca com som pesado!\n" +
                "• Hordas de Esqueletos da Cripta despertam em ondas!\n" +
                "• Bloqueie com o ESCUDO (RMB) e ataque com a ESPADA (LMB)!\n" +
                "• Ao derrotar todos os esqueletos, o portão se abre e surge um BAÚ DE CARTAS!", "Entendido!");
        }

        [MenuItem("Tools/No Light Below/Upgrade Current Player to Iron Knight", false, 20)]
        public static void UpgradeCurrentPlayerToKnight()
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                EditorUtility.DisplayDialog("No Light Below", "Nenhum Player encontrado na cena atual. Execute 'Build Playable Test Arena' primeiro.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(player.gameObject, "Upgrade to Knight");
            var combat = player.GetComponent<PlayerCombat>();
            var (swordPivot, hitbox, shieldPivot) = KnightModelBuilder.BuildKnight(player.gameObject);

            if (combat != null)
            {
                combat.SetCombatEquipment(swordPivot, hitbox, shieldPivot);
            }

            if (player.GetComponent<PlayerHealth>() == null)
            {
                player.gameObject.AddComponent<PlayerHealth>();
            }

            // Re-apply dark atmosphere
            var env = Object.FindAnyObjectByType<DungeonEnvironmentBuilder>();
            if (env != null)
            {
                env.SendMessage("ApplyAtmosphere", UnityEngine.SendMessageOptions.DontRequireReceiver);
            }

            EditorUtility.DisplayDialog("No Light Below", 
                "Cavaleiro e Escudo atualizados com sucesso!\n\n" +
                "• Segure o BOTÃO DIREITO DO MOUSE (RMB) para levantar o escudo e bloquear!\n" +
                "• Pressione ESPAÇO para PULAR!\n" +
                "• ALT ou CTRL para Esquiva com i-frames!", "Excelente!");
        }

        [MenuItem("Tools/No Light Below/Apply Dark & Dirty Visuals", false, 21)]
        public static void ApplyDarkAndDirtyVisuals()
        {
            // 1. Post-Processing Global Volume
            var vol = DungeonPostProcessingFactory.CreateOrUpdateGlobalVolume();

            // 2. Generate and apply PBR Normal-mapped materials
            Material floorPBR = DungeonMaterialFactory.CreateDungeonFloorMaterial();
            Material wallPBR = DungeonMaterialFactory.CreateStoneWallMaterial();
            Material ironPBR = DungeonMaterialFactory.CreateRustyIronMaterial();

            Renderer[] allRenderers = Object.FindObjectsByType<Renderer>();
            int updatedCount = 0;
            foreach (var r in allRenderers)
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("floor"))
                {
                    r.sharedMaterial = floorPBR;
                    updatedCount++;
                }
                else if (n.Contains("wall") || n.Contains("pillar") || n.Contains("ceiling") || n.Contains("lintel"))
                {
                    r.sharedMaterial = wallPBR;
                    updatedCount++;
                }
                else if (n.Contains("iron") || n.Contains("gate") || n.Contains("bar_") || n.Contains("beam"))
                {
                    r.sharedMaterial = ironPBR;
                    updatedCount++;
                }
            }

            // 3. Ensure Camera renders Post Processing
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var camData = mainCam.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null) camData = mainCam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.renderPostProcessing = true;
            }

            // 4. Re-apply dark atmosphere
            var env = Object.FindAnyObjectByType<DungeonEnvironmentBuilder>();
            if (env != null)
            {
                env.ApplyAtmosphere();
            }

            EditorUtility.DisplayDialog("No Light Below", 
                $"Visual 'Dark & Dirty' Aplicado com Sucesso!\n\n" +
                $"• Global Volume URP configurado: Bloom suave, Vignette, ACES Tonemapping e tom frio/dessaturado.\n" +
                $"• {updatedCount} superfícies atualizadas com Normal Maps PBR (fissuras de pedra, ranhuras e brilho de piso molhado).\n" +
                $"• A luz das tochas agora reage diretamente ao relevo 3D das pedras!", "Incrível!");
        }

        [MenuItem("Tools/No Light Below/Setup Infinite Procedural Dungeon Generator", false, 10)]
        [MenuItem("Tools/No Light Below/Setup Infinite Procedural Dungeon", false, 11)]
        public static void SetupInfiniteDungeonGenerator()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Infinite Dungeon Generator");

            // 0. Clean up previous setups (arena, old generator, extra listeners, canvases)
            CleanupPreviousSetup();

            GameObject genRoot = new GameObject("--- INFINITE PROCEDURAL DUNGEON ---");
            Undo.RegisterCreatedObjectUndo(genRoot, "Create Dungeon Generator Root");

            var navSurface = genRoot.AddComponent<NavMeshSurface>();
            var generator = genRoot.AddComponent<NoLightBelow.Dungeon.Procedural.DungeonGenerator>();

            // Config & Rooms Database
            NoLightBelow.Dungeon.Procedural.DungeonConfigSO config = CreateOrLoadDungeonConfig();
            var serialized = new SerializedObject(generator);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.FindProperty("navMeshSurface").objectReferenceValue = navSurface;
            serialized.ApplyModifiedProperties();

            // Player Rig placed inside the Spawn Sanctuary
            GameObject player = CreatePlayerRig(genRoot.transform);
            player.transform.position = new Vector3(0f, 0.1f, -4f);

            // Camera
            SetupCamera(player.transform);

            // Cards & UI
            List<CardData> cardPool = CreateOrLoadSampleCards();
            CreateUI(player.GetComponent<PlayerStats>(), cardPool);

            // Dark Atmosphere & Post Processing
            DungeonPostProcessingFactory.CreateOrUpdateGlobalVolume(genRoot.transform);

            Selection.activeGameObject = genRoot;
            EditorUtility.DisplayDialog("No Light Below", 
                "Gerador Procedural Infinito Configurado com Sucesso!\n\n" +
                "• Pressione PLAY para ver a masmorra gerar salas proceduralmente à frente do jogador!\n" +
                "• As portas se conectam automaticamente sem sobreposição (OverlapBox validation).\n" +
                "• Salas antigas são recicladas pelo Object Pool para economizar memória.\n" +
                "• Cada sala tranca as portas em combate e libera baús de cartas ao ser purificada!", "Sensacional!");
        }

        private static NoLightBelow.Dungeon.Procedural.DungeonConfigSO CreateOrLoadDungeonConfig()
        {
            string folder = "Assets/Data/Dungeon";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            string configPath = $"{folder}/DungeonConfig_Default.asset";
            var existingConfig = AssetDatabase.LoadAssetAtPath<NoLightBelow.Dungeon.Procedural.DungeonConfigSO>(configPath);
            if (existingConfig != null) return existingConfig;

            var config = ScriptableObject.CreateInstance<NoLightBelow.Dungeon.Procedural.DungeonConfigSO>();
            config.RoomsPerFloor = 8;
            config.MaxActiveRoomsAhead = 4;
            config.MaxRoomsBehindToKeep = 2;

            config.AvailableRooms.Add(GetOrCreateRoomData(folder, "Room_Combat_Crypt", "Cripta Esquecida", NoLightBelow.Dungeon.Procedural.RoomType.NormalCombat, 25, 1, 0, 2, 2));
            config.AvailableRooms.Add(GetOrCreateRoomData(folder, "Room_Combat_Catacomb", "Catacumba de Ossos", NoLightBelow.Dungeon.Procedural.RoomType.NormalCombat, 20, 1, 0, 3, 2));
            config.AvailableRooms.Add(GetOrCreateRoomData(folder, "Room_Elite_CryptLord", "Câmara do Algoz (Elite)", NoLightBelow.Dungeon.Procedural.RoomType.Elite, 8, 2, 0, 3, 3, hasElite: true));
            config.AvailableRooms.Add(GetOrCreateRoomData(folder, "Room_Loot_Vault", "Cofre da Masmorra", NoLightBelow.Dungeon.Procedural.RoomType.Loot, 6, 2, 0, 0, 0));

            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static NoLightBelow.Dungeon.Procedural.RoomDataSO GetOrCreateRoomData(string folder, string id, string title, NoLightBelow.Dungeon.Procedural.RoomType type, int weight, int minDepth, int maxDepth, int waves, int enemiesPerWave, bool hasElite = false)
        {
            string path = $"{folder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NoLightBelow.Dungeon.Procedural.RoomDataSO>(path);
            if (existing != null) return existing;

            var room = ScriptableObject.CreateInstance<NoLightBelow.Dungeon.Procedural.RoomDataSO>();
            room.RoomId = id;
            room.DisplayName = title;
            room.Type = type;
            room.SpawnWeight = weight;
            room.MinDepthLevel = minDepth;
            room.MaxDepthLevel = maxDepth;
            room.TotalWaves = waves;
            room.BaseEnemiesPerWave = enemiesPerWave;
            room.HasEliteEnemy = hasElite;
            room.ApproximateBoundsSize = new Vector3(24f, 6f, 28f);

            AssetDatabase.CreateAsset(room, path);
            return room;
        }

        private static GameObject CreatePlayerRig(Transform parent)
        {
            GameObject player = new GameObject("Player_Protagonist");
            player.transform.SetParent(parent);

            // Character Controller
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Player Stats, Health & Controller
            var stats = player.AddComponent<PlayerStats>();
            var health = player.AddComponent<PlayerHealth>();
            var controller = player.AddComponent<PlayerController>();
            var combat = player.AddComponent<PlayerCombat>();

            // Build High-Quality Procedural Knight Rig (Greathelm, Breastplate, Pauldrons, Greaves, Sword, Heater Shield)
            var (swordPivot, hitbox, shieldPivot) = KnightModelBuilder.BuildKnight(player);

            combat.SetCombatEquipment(swordPivot, hitbox, shieldPivot);

            return player;
        }

        private static void CleanupPreviousSetup()
        {
            string[] rootsToDestroy = new[]
            {
                "--- NO LIGHT BELOW TEST ARENA ---",
                "--- INFINITE PROCEDURAL DUNGEON ---",
                "UI_Canvas",
                "EventSystem"
            };

            foreach (var rootName in rootsToDestroy)
            {
                var obj = GameObject.Find(rootName);
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            // Remove any orphan players
            var existingPlayers = Object.FindObjectsByType<PlayerController>();
            foreach (var p in existingPlayers)
            {
                if (p != null) Undo.DestroyObjectImmediate(p.gameObject);
            }

            // Remove any orphan training dummies at scene root
            var existingDummies = Object.FindObjectsByType<TrainingDummy>();
            foreach (var d in existingDummies)
            {
                if (d != null && d.transform.parent == null) Undo.DestroyObjectImmediate(d.gameObject);
            }

            EnsureSingleAudioListener();
        }

        private static void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>();
            Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();

            AudioListener keep = null;
            if (cam != null)
            {
                keep = cam.GetComponent<AudioListener>();
            }

            if (keep == null && listeners.Length > 0)
            {
                keep = listeners[0];
            }

            foreach (var l in listeners)
            {
                if (l != keep && l != null)
                {
                    Undo.DestroyObjectImmediate(l);
                }
            }

            if (keep == null && cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
        }

        private static void SetupCamera(Transform playerTarget)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindAnyObjectByType<Camera>();
            }

            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            // Enforce single AudioListener on camera
            EnsureSingleAudioListener();

            var tpCam = cam.GetComponent<ThirdPersonCameraController>();
            if (tpCam == null)
            {
                tpCam = cam.gameObject.AddComponent<ThirdPersonCameraController>();
            }

            tpCam.SetTarget(playerTarget);
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.1f;

            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
        }

        private static GameObject CreateTrainingDummy(Transform parent)
        {
            GameObject dummy = new GameObject("Training_Dummy_Target");
            dummy.transform.SetParent(parent);

            Material woodMat = DungeonMaterialFactory.CreateDummyMaterial();

            // Post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Dummy_Post";
            post.transform.SetParent(dummy.transform);
            post.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            post.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
            post.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Cross Arms
            GameObject arms = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arms.name = "Dummy_Arms";
            arms.transform.SetParent(post.transform);
            arms.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            arms.transform.localScale = new Vector3(2.4f, 0.3f, 0.3f);
            arms.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(arms.GetComponent<Collider>());

            // Straw Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Dummy_Head";
            head.transform.SetParent(post.transform);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale = new Vector3(0.6f, 0.7f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            var dummyComp = dummy.AddComponent<TrainingDummy>();
            var serialized = new SerializedObject(dummyComp);
            serialized.FindProperty("dummyMeshTransform").objectReferenceValue = post.transform;
            serialized.FindProperty("meshRenderer").objectReferenceValue = post.GetComponent<Renderer>();
            serialized.ApplyModifiedProperties();

            // Collider on root
            var capsuleCol = dummy.AddComponent<CapsuleCollider>();
            capsuleCol.height = 2.2f;
            capsuleCol.radius = 0.6f;
            capsuleCol.center = new Vector3(0f, 1.1f, 0f);

            return dummy;
        }

        private static List<CardData> CreateOrLoadSampleCards()
        {
            string folderPath = "Assets/Data/Cards";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            var pool = new List<CardData>();

            pool.Add(GetOrCreateCard(folderPath, "Card_LaminaAlgoz", "Lâmina do Algoz", CardRarity.Common, CardCategory.Offensive,
                "O fio irregular dessa lâmina se alimenta do menor deslize do oponente.",
                new[] { new StatModifier { TargetStat = StatType.AttackPower, PercentMultiplier = 0.22f } },
                bleed: true, bleedChance: 0.35f));

            pool.Add(GetOrCreateCard(folderPath, "Card_PassoEspectral", "Passo Espectral", CardRarity.Rare, CardCategory.Agility,
                "A dungeon sussurra nos seus ouvidos que esquivar é escapar do destino.",
                new[] {
                    new StatModifier { TargetStat = StatType.MoveSpeed, PercentMultiplier = 0.18f },
                    new StatModifier { TargetStat = StatType.AttackSpeed, PercentMultiplier = 0.25f }
                },
                shockwave: true));

            pool.Add(GetOrCreateCard(folderPath, "Card_PactoDeSangue", "Pacto de Sangue", CardRarity.Cursed, CardCategory.Occult,
                "Sua própria carne é consumida, mas cada golpe rasga a essência dos seus algozes.",
                new[] {
                    new StatModifier { TargetStat = StatType.AttackPower, PercentMultiplier = 0.50f },
                    new StatModifier { TargetStat = StatType.MaxHealth, PercentMultiplier = -0.25f }
                },
                lifesteal: true, lifestealPct: 0.15f));

            pool.Add(GetOrCreateCard(folderPath, "Card_CouracaOssos", "Couraça de Ossos", CardRarity.Rare, CardCategory.Defensive,
                "Restos calcificados dos que caíram antes de você cobrem seus órgãos vitais.",
                new[] {
                    new StatModifier { TargetStat = StatType.MaxHealth, FlatValue = 40f },
                    new StatModifier { TargetStat = StatType.ArmorReduction, PercentMultiplier = 0.15f }
                }));

            pool.Add(GetOrCreateCard(folderPath, "Card_FuriaDaCripta", "Fúria da Cripta", CardRarity.Epic, CardCategory.Offensive,
                "O cheiro de ferro e cinzas desperta a sede de destruição.",
                new[] {
                    new StatModifier { TargetStat = StatType.CriticalChance, PercentMultiplier = 0.25f },
                    new StatModifier { TargetStat = StatType.CriticalDamage, PercentMultiplier = 0.50f },
                    new StatModifier { TargetStat = StatType.AttackSpeed, PercentMultiplier = 0.20f }
                }));

            AssetDatabase.SaveAssets();
            return pool;
        }

        private static CardData GetOrCreateCard(string folder, string filename, string title, CardRarity rarity, CardCategory cat, string lore, StatModifier[] mods, bool bleed = false, float bleedChance = 0f, bool lifesteal = false, float lifestealPct = 0f, bool shockwave = false)
        {
            string path = $"{folder}/{filename}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (existing != null) return existing;

            var card = ScriptableObject.CreateInstance<CardData>();
            card.CardId = filename;
            card.CardTitle = title;
            card.Rarity = rarity;
            card.Category = cat;
            card.LoreQuote = lore;
            card.Modifiers.AddRange(mods);
            card.TriggersBleedOnHit = bleed;
            card.BleedChance = bleedChance;
            card.TriggersLifestealOnHit = lifesteal;
            card.LifestealPercent = lifestealPct;
            card.ShockwaveOnDodge = shockwave;

            AssetDatabase.CreateAsset(card, path);
            return card;
        }

        private static void CreateUI(PlayerStats playerStats, List<CardData> cardPool)
        {
            // 1. Canvas
            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Event System
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // 2. Crosshair (Center)
            GameObject crosshair = new GameObject("Crosshair", typeof(Image));
            crosshair.transform.SetParent(canvasObj.transform, false);
            var chImg = crosshair.GetComponent<Image>();
            chImg.color = new Color(1f, 1f, 1f, 0.75f);
            var chRect = crosshair.GetComponent<RectTransform>();
            chRect.sizeDelta = new Vector2(6f, 6f);
            chRect.anchorMin = chRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 3. HUD Bars Container (Bottom Left)
            GameObject hudContainer = new GameObject("HUD_Container", typeof(RectTransform));
            hudContainer.transform.SetParent(canvasObj.transform, false);
            var hudRect = hudContainer.GetComponent<RectTransform>();
            hudRect.anchorMin = hudRect.anchorMax = new Vector2(0f, 0f);
            hudRect.pivot = new Vector2(0f, 0f);
            hudRect.anchoredPosition = new Vector2(40f, 40f);
            hudRect.sizeDelta = new Vector2(360f, 100f);

            // Health Slider
            Slider hpSlider = CreateSlider(hudContainer.transform, "HealthBar", new Vector2(0f, 45f), new Vector2(340f, 26f), new Color(0.15f, 0.15f, 0.15f, 0.85f), new Color(0.75f, 0.12f, 0.16f));
            Text hpText = CreateLabel(hudContainer.transform, "HP_Text", "100 / 100", new Vector2(0f, 45f), new Vector2(340f, 26f), 16, Color.white);

            // Stamina Slider
            Slider stamSlider = CreateSlider(hudContainer.transform, "StaminaBar", new Vector2(0f, 12f), new Vector2(280f, 18f), new Color(0.15f, 0.15f, 0.15f, 0.85f), new Color(0.85f, 0.62f, 0.15f));
            Text stamText = CreateLabel(hudContainer.transform, "Stam_Text", "100 / 100", new Vector2(0f, 12f), new Vector2(280f, 18f), 13, Color.white);

            // Deck Count Text
            Text cardCountText = CreateLabel(hudContainer.transform, "DeckCount", "CARTAS: 0", new Vector2(0f, 82f), new Vector2(200f, 24f), 18, new Color(0.9f, 0.82f, 0.6f));

            var hud = canvasObj.AddComponent<PlayerHUD>();
            var health = playerStats != null ? playerStats.GetComponent<PlayerHealth>() : null;
            hud.SetReferences(hpSlider, stamSlider, hpText, stamText, cardCountText, chImg, playerStats, health);

            // 4. Card Reward Modal
            GameObject modalRoot = new GameObject("Modal_CardReward", typeof(RectTransform), typeof(Image));
            modalRoot.transform.SetParent(canvasObj.transform, false);
            var modalRect = modalRoot.GetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.sizeDelta = Vector2.zero;
            modalRoot.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.88f); // Dark translucent backdrop

            // Modal Header
            CreateLabel(modalRoot.transform, "ModalTitle", "ESCOLHA SUA DÁDIVA DA CRIPTA", new Vector2(0f, 380f), new Vector2(800f, 60f), 32, new Color(0.95f, 0.85f, 0.65f), TextAnchor.MiddleCenter);

            // Cards Container
            GameObject cardsContainer = new GameObject("Cards_Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardsContainer.transform.SetParent(modalRoot.transform, false);
            var cardsRect = cardsContainer.GetComponent<RectTransform>();
            cardsRect.anchorMin = cardsRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRect.sizeDelta = new Vector2(1100f, 520f);
            cardsRect.anchoredPosition = new Vector2(0f, -30f);

            var hlg = cardsContainer.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Card Template (Prefab in scene)
            CardUI cardTemplate = CreateCardTemplate(modalRoot.transform);
            cardTemplate.gameObject.SetActive(false);

            var rewardModal = canvasObj.AddComponent<CardRewardModal>();
            rewardModal.SetReferences(modalRoot, cardsContainer.transform, cardTemplate, playerStats, cardPool);
        }

        private static CardUI CreateCardTemplate(Transform parent)
        {
            GameObject cardObj = new GameObject("Card_UI_Template", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(parent, false);
            var rect = cardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 430f);

            // Parchment Background
            var bgImg = cardObj.GetComponent<Image>();
            bgImg.color = new Color(0.14f, 0.13f, 0.12f); // Deep ancient slate/parchment

            // Frame / Border
            GameObject frameObj = new GameObject("Card_Frame", typeof(RectTransform), typeof(Image), typeof(Outline));
            frameObj.transform.SetParent(cardObj.transform, false);
            var frameRect = frameObj.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.sizeDelta = Vector2.zero;
            var frameImg = frameObj.GetComponent<Image>();
            frameImg.color = new Color(0.85f, 0.75f, 0.5f);
            var outline = frameObj.GetComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            // Category Label
            Text catText = CreateLabel(cardObj.transform, "Category", "OFENSIVO", new Vector2(0f, 180f), new Vector2(250f, 24f), 13, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);

            // Title Label
            Text titleText = CreateLabel(cardObj.transform, "Title", "Lâmina do Algoz", new Vector2(0f, 150f), new Vector2(250f, 36f), 20, Color.white, TextAnchor.MiddleCenter);

            // Artwork Placeholder
            GameObject artObj = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
            artObj.transform.SetParent(cardObj.transform, false);
            var artRect = artObj.GetComponent<RectTransform>();
            artRect.anchoredPosition = new Vector2(0f, 50f);
            artRect.sizeDelta = new Vector2(230f, 140f);
            var artImg = artObj.GetComponent<Image>();
            artImg.color = new Color(0.25f, 0.22f, 0.20f);

            // Description Label
            Text descText = CreateLabel(cardObj.transform, "Description", "+25% Dano de Ataque\n+10% Sangramento", new Vector2(0f, -60f), new Vector2(240f, 80f), 15, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter);

            // Lore Quote
            Text loreText = CreateLabel(cardObj.transform, "Lore", "\"O aço clama por sangue novo.\"", new Vector2(0f, -145f), new Vector2(240f, 50f), 12, new Color(0.6f, 0.55f, 0.5f), TextAnchor.MiddleCenter);

            var cardUI = cardObj.AddComponent<CardUI>();
            cardUI.SetReferences(frameImg, bgImg, artImg, titleText, catText, descText, loreText);

            return cardUI;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, Color fillColor)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            var rect = sliderObj.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var slider = sliderObj.GetComponent<Slider>();

            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = bgColor;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = Vector2.zero;
            faRect.anchorMax = Vector2.one;
            faRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRect = fill.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = fillColor;

            slider.targetGraphic = fillImg;
            slider.fillRect = fRect;
            return slider;
        }

        private static Text CreateLabel(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var txt = obj.GetComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = align;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return txt;
        }
    }
}
#endif
