
using UnityEngine;

[CreateAssetMenu(menuName = "AI/Conditions/OnInteract")]
public class OnInteractCondition : TransitionCondition
{
    [Tooltip("How far the player can be to interact")]
    public float interactRange = 3f;
    [Tooltip("Which key to press")]
    public KeyCode interactKey = KeyCode.E;

    public override bool Evaluate(CharacterStatemachine sm)
    {
        //// Only check on the key‑down frame
        //if (!Input.GetKeyDown(interactKey))
        //    return false;

        //// Get your local player and its camera transform
        //var localPlayer = Service.Get<IPlayerService>().LocalNetworkPlayer;
        //if (localPlayer == null)
        //    return false;

        //var camTransform = localPlayer.CameraController.CinemachineCamera.transform;
        //if (camTransform == null)
        //    return false;

        //// Raycast forward from the player's camera
        //if (Physics.Raycast(
        //        camTransform.position,
        //        camTransform.forward,
        //        out RaycastHit hit,
        //        interactRange))
        //{
        //    // Did we hit the same NPC that this StateMachine sits on?
        //    if (hit.collider.GetComponent<StateMachine>() == sm)
        //        return true;
        //}

        return false;
    }
}