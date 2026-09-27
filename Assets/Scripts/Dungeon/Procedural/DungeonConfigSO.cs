using System.Collections.Generic;
using UnityEngine;

namespace NoLightBelow.Dungeon.Procedural
{
    [CreateAssetMenu(fileName = "NewDungeonConfig", menuName = "No Light Below/Dungeon/Dungeon Config")]
    public class DungeonConfigSO : ScriptableObject
    {
        [Header("Floor Structure")]
        [Tooltip("Quantidade de salas geradas por ciclo/andar da mega-dungeon.")]
        public int RoomsPerFloor = 8;

        [Tooltip("Distância máxima em salas ativas à frente do jogador.")]
        public int MaxActiveRoomsAhead = 4;

        [Tooltip("Quantidade de salas antigas atrás do jogador mantidas antes de serem descartadas pelo pool.")]
        public int MaxRoomsBehindToKeep = 2;

        [Header("Available Rooms Database")]
        public List<RoomDataSO> AvailableRooms = new List<RoomDataSO>();

        [Header("Difficulty & Roguelike Depth Scaling")]
        [Tooltip("Curva de multiplicação de vida dos inimigos por profundidade (Eixo X = Profundidade/Sala, Eixo Y = Multiplicador).")]
        public AnimationCurve EnemyHealthScaling = AnimationCurve.Linear(1f, 1.0f, 25f, 3.5f);

        [Tooltip("Curva de multiplicação de dano dos inimigos por profundidade.")]
        public AnimationCurve EnemyDamageScaling = AnimationCurve.Linear(1f, 1.0f, 25f, 2.8f);

        [Tooltip("Probabilidade (0 a 1) de gerar uma sala Elite conforme a profundidade avança.")]
        public AnimationCurve EliteChanceScaling = AnimationCurve.Linear(1f, 0.05f, 20f, 0.45f);

        public RoomDataSO GetRandomWeightedRoom(int currentDepth, RoomType preferredType = RoomType.NormalCombat)
        {
            var validCandidates = new List<RoomDataSO>();
            int totalWeight = 0;

            foreach (var room in AvailableRooms)
            {
                if (room == null || !room.IsDepthValid(currentDepth)) continue;

                // Priority to preferred type if matches
                if (room.Type == preferredType)
                {
                    validCandidates.Add(room);
                    totalWeight += room.SpawnWeight;
                }
            }

            // Fallback to any valid room if preferred list is empty
            if (validCandidates.Count == 0)
            {
                foreach (var room in AvailableRooms)
                {
                    if (room != null && room.IsDepthValid(currentDepth) && room.Type != RoomType.Spawn)
                    {
                        validCandidates.Add(room);
                        totalWeight += room.SpawnWeight;
                    }
                }
            }

            if (validCandidates.Count == 0) return null;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var candidate in validCandidates)
            {
                cumulative += candidate.SpawnWeight;
                if (roll < cumulative)
                {
                    return candidate;
                }
            }

            return validCandidates[0];
        }

        public float GetHealthMultiplier(int depth) => EnemyHealthScaling.Evaluate(depth);
        public float GetDamageMultiplier(int depth) => EnemyDamageScaling.Evaluate(depth);
        public float GetEliteChance(int depth) => Mathf.Clamp01(EliteChanceScaling.Evaluate(depth));
    }
}
