using UnityEngine;

[System.Serializable]
public class StateMachineData
{
    public bool IsAlive = true;

    public StateMachineData()
    {
        IsAlive = true;
        // Default constructor initializes IsAlive to true
    }

    public StateMachineData(bool isAlive)
    {
        this.IsAlive = isAlive;
    }
}
