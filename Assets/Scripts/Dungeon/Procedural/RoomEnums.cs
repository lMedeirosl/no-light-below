namespace NoLightBelow.Dungeon.Procedural
{
    public enum RoomType
    {
        Spawn,          // Sala inicial segura
        NormalCombat,   // Sala de combate padrão (ondas de esqueletos/inimigos)
        Elite,          // Sala com inimigos mais fortes, modificadores e melhores cartas
        Trap,           // Sala com armadilhas, timing e desafio de movimentação
        Event,          // Santuários, pactos arcanos ou decisões de risco vs recompensa
        Loot,           // Sala com baús, altares de oferenda e tesouros
        Special         // Efeitos anômalos da mega-dungeon viva
    }

    public enum RoomState
    {
        Unvisited,      // Jogador ainda não entrou
        ActiveCombat,   // Portas trancadas, combate em andamento
        Cleared         // Sala purificada, portas abertas, recompensas coletadas
    }
}
