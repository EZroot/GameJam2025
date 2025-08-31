// WaveSet.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spawning/Wave Set")]
public class WaveSet : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Min(1)] public int count = 5;
        [Min(0.01f)] public float spawnRate = 5f;   // units per second
        [Min(0f)] public float spreadRadius = 5f;   // random offset around anchor
    }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Optional override. If empty, spawner's anchors are used.")]
        public Transform[] anchors;
        public List<Entry> entries = new();
        [Min(0f)] public float preDelay = 0f;   // before this wave starts
        [Min(0f)] public float postDelay = 0f;  // after this wave completes
    }

    public List<Wave> waves = new();
}
