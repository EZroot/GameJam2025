using System.Collections.Generic;
using UnityEngine;

public class NpcService : MonoBehaviour, INpcService
{
    private List<CharacterContext> m_npcCollection = new List<CharacterContext>();
    public List<CharacterContext> NpcCollection => m_npcCollection;

    public void AddNpc(CharacterContext npc)
    {
        m_npcCollection.Add(npc);
    }

    public void RemoveNpc(CharacterContext npc) 
    {
        m_npcCollection.Remove(npc);
    }
}
