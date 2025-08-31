using UnityEngine;

public interface IPlayerService : IService
{
    CharacterContext LocalPlayer { get; }
    void AddPlayer(CharacterContext ctx);
}
