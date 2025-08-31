using UnityEngine;

public interface IUIService : IService
{
    UIScore UIScore { get; }
    UIEndScreen UIEndScreen { get; }
}
