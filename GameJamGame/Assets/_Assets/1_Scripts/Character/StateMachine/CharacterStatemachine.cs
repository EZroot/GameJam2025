using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class CharacterStatemachine : MonoBehaviour
{
    [System.Serializable]
    public struct StateLink
    {
        public StateScriptableObject state;
        public List<StateTransition> transitions;
    }

    [System.Serializable]
    public struct StateArgs
    {
        public Transform Target;
        public Vector3 Point;
        public object Data;

        public static StateArgs WithTarget(Transform t) => new StateArgs { Target = t };
        public static StateArgs WithPoint(Vector3 p) => new StateArgs { Point = p };
    }

    [System.Serializable]
    public struct StateTransition
    {
        public StateScriptableObject toState;
        public TransitionCondition condition;
        public bool inverseConditionResult;
    }

    [Header("Graph Data")]
    public List<StateLink> stateGraph;
    public StateScriptableObject initialState;

    [Header("Identity")]
    public string Identity = "NPC_Fillmein";

    [Header("References")]
    public CharacterContext CharacterContext;

    // Clone maps for remapping originals to instances
    private Dictionary<StateScriptableObject, StateScriptableObject> _stateCloneMap;
    private Dictionary<TransitionCondition, TransitionCondition> _condCloneMap;

    // Runtime graph of cloned links
    private List<StateLink> _clonedStateGraph;
    private IState _currentState;

    public IState CurrentState => _currentState;

    void Awake()
    {
        // 1) Instantiate clones of all state SOs and condition SOs
        _stateCloneMap = new Dictionary<StateScriptableObject, StateScriptableObject>();
        _condCloneMap = new Dictionary<TransitionCondition, TransitionCondition>();

        foreach (var link in stateGraph)
        {
            if (!_stateCloneMap.ContainsKey(link.state))
                _stateCloneMap[link.state] = Instantiate(link.state);

            foreach (var t in link.transitions)
            {
                if (!_stateCloneMap.ContainsKey(t.toState))
                    _stateCloneMap[t.toState] = Instantiate(t.toState);

                if (t.condition != null && !_condCloneMap.ContainsKey(t.condition))
                    _condCloneMap[t.condition] = Instantiate(t.condition);
            }
        }

        // 2) Patch nested SO references (e.g. TalkState.nextState)
        foreach (var kv in _stateCloneMap)
        {
            if (kv.Key is TalkState origTalk && kv.Value is TalkState cloneTalk)
            {
                var origNext = origTalk.m_nextStateOnDialogueFinished;
                if (_stateCloneMap.TryGetValue(origNext, out var cloneNext))
                    cloneTalk.m_nextStateOnDialogueFinished = cloneNext;
                else
                    Debug.LogError($"[SM] No clone found for TalkState.next: {origNext.name}");
            }
        }

        // 3) Build runtime clone graph
        _clonedStateGraph = stateGraph.Select(link => {
            var clonedTrans = link.transitions.Select(t => new StateTransition
            {
                toState = _stateCloneMap[t.toState],
                condition = (t.condition != null) ? _condCloneMap[t.condition] : null,
                inverseConditionResult = t.inverseConditionResult
            }).ToList();

            return new StateLink
            {
                state = _stateCloneMap[link.state],
                transitions = clonedTrans
            };
        }).ToList();

        // 4) Enter initial state (mapped through cloneMap)
        ChangeState(initialState);
    }

    void Update()
    {
        // Run current state's logic
        _currentState.Execute();

        // Evaluate transitions for the active cloned link
        foreach (var link in _clonedStateGraph)
        {
            if (link.state == (_currentState as StateScriptableObject))
            {
                foreach (var t in link.transitions)
                {
                    //Debug.Log($"[SM] Checking {link.state.name} -> {t.toState.name} (IsAlive={Data.IsAlive})");
                    var conditionResult = t.inverseConditionResult ? !t.condition.Evaluate(this) : t.condition.Evaluate(this);
                    if (conditionResult)
                    {
                        //Debug.Log($"[SM] Transition PASSED: {t.toState.name}");
                        ChangeState(t.toState);
                        return;
                    }
                }
            }
        }
    }

    public void ChangeState(StateScriptableObject nextSO)
    {

        // Remap original SO to its clone if needed
        if (_stateCloneMap.TryGetValue(nextSO, out var clone))
            nextSO = clone;

        Debug.Log($"[SM] Changing state from {_currentState?.GetType().Name} to {nextSO.GetType().Name}");

        // Exit previous
        _currentState?.Exit();

        // Enter new
        _currentState = nextSO as IState;
        _currentState.Enter(this);


    }

#if UNITY_EDITOR
    // Draw gizmos for current state
    private void OnDrawGizmos()
    {
        if(_currentState is WanderState wanderstate)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "Wandering");
        }
        if (_currentState is FollowState followState)
        {
            if (followState != null)
            {
                Gizmos.color = Color.green;
                if (followState != null && followState.GetType().GetField("_followTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
                {
                    var target = followState.GetType().GetField("_followTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(followState) as Transform;
                    if (target != null)
                    {
                        Gizmos.DrawLine(transform.position, target.position);
                        Gizmos.DrawWireSphere(target.position, 0.3f);
                        //draw text
                        UnityEditor.Handles.Label(target.position + Vector3.up * 0.5f, "Follow Target");
                    }
                }
            }
        }
        else if (_currentState is GatherState gatherState)
        {
            if (gatherState != null)
            {
                Gizmos.color = Color.yellow;
                var target = gatherState.GetType().GetField("target", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).GetValue(gatherState) as Transform;
                if (target != null)
                {
                    Gizmos.DrawLine(transform.position, target.position);
                    Gizmos.DrawWireSphere(target.position, 0.3f);
                    //draw text
                    UnityEditor.Handles.Label(target.position + Vector3.up * 0.5f, "Gather Target");
                }
            }
        }
        else if (_currentState is TalkState talkState)
        {
            if (talkState != null)
            {
                Gizmos.color = Color.cyan;
                var targetField = talkState.GetType().GetField("m_targetTalkingPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    var target = targetField.GetValue(talkState) as Transform;
                    if (target != null)
                    {
                        Gizmos.DrawLine(transform.position, target.position);
                        Gizmos.DrawWireSphere(target.position, 0.3f);
                    }
                }
            }
        }
        else
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

    }
#endif
    public T GetStateInstance<T>() where T : StateScriptableObject
    {
        // fast path: build once and cache if you want
        return _clonedStateGraph.Select(l => l.state).OfType<T>().FirstOrDefault();
    }

}
