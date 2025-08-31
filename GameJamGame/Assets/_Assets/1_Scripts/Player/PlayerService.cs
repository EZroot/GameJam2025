using UnityEngine;

public class PlayerService : MonoBehaviour, IPlayerService
{
    private CharacterContext m_localPlayer = null;
    public CharacterContext LocalPlayer => m_localPlayer;
    public void AddPlayer(CharacterContext player)
    {
        m_localPlayer = player;
    }
}
