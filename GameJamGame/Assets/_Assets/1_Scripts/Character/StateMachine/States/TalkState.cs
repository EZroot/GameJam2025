using System;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/States/Talk")]
public class TalkState : StateScriptableObject
{
    private CharacterStatemachine m_stateMachine;

    [Header("Next State")]
    public StateScriptableObject m_nextStateOnDialogueFinished;

    [Header("Dialogue")]
    //public DialogueNode dialogueRoot;

    [Header("Rotation")]
    [Tooltip("How fast the NPC turns to face the player")]
    public float rotationSpeed = 5f;

    // internal state
    //private DialogueNode _currentNode;
    private bool _isFinished;

    public override void Enter(CharacterStatemachine statemachine)
    {
        m_stateMachine = statemachine;


        //var prevDist = 0f;
        //foreach(var player in Service.Get<IPlayerService>().NetworkPlayerCollection)
        //{
        //    var dist = Vector3.Distance(m_stateMachine.transform.position, player.transform.position);
        //    if (dist < prevDist || prevDist == 0f)
        //    {
        //        m_targetTalkingPlayer = player;
        //        prevDist = dist;
        //    }
        //}

        Debug.Log("Talk state entered");
    }

    public override void Execute()
    {
     

    }

    public override void Exit()
    {
        Debug.Log("Talk state left");
        //_isFinished = true;
        //Service.Get<IDialogueService>().StopAllDialogue();
        //Service.Get<IPlayerInputService>().SetInputState(false, false, false);
    }

}
