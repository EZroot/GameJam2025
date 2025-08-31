using System.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("Spawn Anchors")]
    [SerializeField] Transform[] spawnAnchors;        // if empty, uses self

    [Header("Default Wave")]
    [SerializeField] GameObject defaultEnemy;
    [SerializeField, Min(1)] int startCount = 10;     // wave 0 count
    [SerializeField, Min(0.01f)] float startRate = 10f; // wave 0 spawns/sec
    [SerializeField, Min(0f)] float startSpread = 5f;

    [Header("Scaling per Wave")]
    [SerializeField, Min(1f)] float countMulPerWave = 2f; // 2x harder each wave
    [SerializeField, Min(1f)] float rateMulPerWave = 2f; // faster each wave

    [Header("Flow")]
    [SerializeField] bool autoRun = true;
    [SerializeField, Min(0f)] float gapBetweenWaves = 0.75f;

    [Header("Runtime")]
    [SerializeField, ReadOnly] int currentWaveIndex = -1;
    [SerializeField, ReadOnly] int currentWaveCount;
    [SerializeField, ReadOnly] float currentWaveRate;
    Coroutine _runner;

    void Start()
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("02_Game"));
        if (autoRun) StartNextWave();
    }

    private void LateUpdate()
    {
        var ui = Service.Get<IUIService>().UIScore;
        var npcs = Service.Get<INpcService>().NpcCollection.Count;

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Wave: {currentWaveIndex}");
        stringBuilder.AppendLine($"Enemies: {npcs}");
        ui.SetWaveText(stringBuilder.ToString());
    }

    public void StartNextWave()
    {
        if (_runner != null) return;
        currentWaveIndex++;

        // compute scaled defaults
        currentWaveCount = Mathf.Max(1, Mathf.RoundToInt(startCount * Mathf.Pow(countMulPerWave, currentWaveIndex)));
        currentWaveRate = startRate * Mathf.Pow(rateMulPerWave, currentWaveIndex);

        _runner = StartCoroutine(CoRunSimple(defaultEnemy, currentWaveCount, currentWaveRate, startSpread));
    }

    IEnumerator CoRunSimple(GameObject prefab, int count, float rate, float spread)
    {
        if (!prefab) { _runner = null; yield break; }

        float dt = Mathf.Max(0f, 1f / rate);
        var anchors = EffectiveAnchors();

        for (int i = 0; i < count; i++)
        {
            var anchor = anchors.Length > 0 ? anchors[Random.Range(0, anchors.Length)] : transform;
            Vector3 pos = anchor ? anchor.position : transform.position;
            pos += RandomInsideCircleXZ(spread);
            Instantiate(prefab, pos, Quaternion.identity);
            if (dt > 0f) yield return new WaitForSeconds(dt);
        }

        _runner = null;
        if (autoRun && gapBetweenWaves > 0f) yield return new WaitForSeconds(gapBetweenWaves);
        if (autoRun) StartNextWave();
    }

    Transform[] EffectiveAnchors()
    {
        if (spawnAnchors != null && spawnAnchors.Length > 0) return spawnAnchors;
        return new[] { transform };
    }

    static Vector3 RandomInsideCircleXZ(float radius)
    {
        if (radius <= 0f) return Vector3.zero;
        var v2 = Random.insideUnitCircle * radius;
        return new Vector3(v2.x, v2.y, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (spawnAnchors != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var a in spawnAnchors)
                if (a) Gizmos.DrawWireSphere(a.position, 1f);
        }
    }
}

// Utility to show a readonly field in inspector.
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool prev = GUI.enabled; GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = prev;
    }
}
#endif
