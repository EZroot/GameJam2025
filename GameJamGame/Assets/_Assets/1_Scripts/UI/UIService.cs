using UnityEngine;

public class UIService : MonoBehaviour, IUIService
{
    [SerializeField] private UIScore m_uiScore;
    [SerializeField] private UIEndScreen m_endScreen;

    public UIScore UIScore => m_uiScore;

    public UIEndScreen UIEndScreen => m_endScreen;
}
