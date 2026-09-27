using UnityEngine;

namespace NoLightBelow.Dungeon.Procedural
{
    [CreateAssetMenu(fileName = "NewRoomData", menuName = "No Light Below/Dungeon/Room Data")]
    public class RoomDataSO : ScriptableObject
    {
        [Header("Room Identity")]
        public string RoomId = "Room_Standard";
        public string DisplayName = "Câmara da Cripta";
        public RoomType Type = RoomType.NormalCombat;

        [Header("Prefab & Geometry")]
        [Tooltip("Prefab da sala contendo o RoomInstance e seus RoomConnectors.")]
        public GameObject RoomPrefab;

        [Tooltip("Dimensões aproximadas da sala (Largura, Altura, Comprimento) para verificação rápida de colisão/espaço.")]
        public Vector3 ApproximateBoundsSize = new Vector3(24f, 6f, 28f);

        [Header("Spawning Rules")]
        [Range(1, 100)]
        [Tooltip("Peso relativo de seleção desta sala.")]
        public int SpawnWeight = 10;

        [Tooltip("Profundidade/andar mínimo da run para esta sala começar a aparecer.")]
        public int MinDepthLevel = 1;

        [Tooltip("Profundidade/andar máximo para esta sala (0 = infinito).")]
        public int MaxDepthLevel = 0;

        [Header("Combat Configuration (para salas de combate/elite)")]
        public int TotalWaves = 2;
        public int BaseEnemiesPerWave = 2;
        public bool HasEliteEnemy = false;

        public bool IsDepthValid(int currentDepth)
        {
            if (currentDepth < MinDepthLevel) return false;
            if (MaxDepthLevel > 0 && currentDepth > MaxDepthLevel) return false;
            return true;
        }
    }
}
