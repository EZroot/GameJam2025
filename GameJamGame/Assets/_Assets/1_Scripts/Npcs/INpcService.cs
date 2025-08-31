using System.Collections.Generic;
using UnityEngine;

public interface INpcService : IService
{
    List<CharacterContext> NpcCollection { get; }
    void AddNpc(CharacterContext npc);
    void RemoveNpc(CharacterContext npc);
}
