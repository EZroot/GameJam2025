using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "AI/States/PlayerMovementState")]
public class PlayerMovementState : StateScriptableObject
{
    private CharacterStatemachine m_stateMachine;

    public float wanderRadius = 5f;
    public float gatherRadius = 2f;
    public float wanderTimer = 3f;

    private float shootCooldown = 0.273f;
    private float shootTimer = 0f;
    private bool canShoot;
    private bool initialized = false;
    public override void Enter(CharacterStatemachine statemachine)
    {
        m_stateMachine = statemachine;

        Service.Get<IAudioService>().SetAudioVisalizerSong(AudioService.MusicChoice.GameMusic);
        Service.Get<IAudioService>().PlayAudioVisualizer();

        Service.Get<IUIService>().UIScore.gameObject.SetActive(true);
        m_stateMachine.CharacterContext.CharacterLookAt2D.SetAimMode(true);
        Service.Get<IUIService>().UIScore.OnOverflowed += UIScore_OnOverflowed;
        m_stateMachine.StartCoroutine(WaitForCamera());
        initialized = true;
    }

    IEnumerator WaitForCamera()
    {
        while (m_stateMachine.CharacterContext.CameraController == null)
            yield return null;
        m_stateMachine.CharacterContext.CameraController.SetCameraTarget(m_stateMachine.transform);
    }

    void PopPlayer()
    {
        Service.Get<IUIService>().UIScore.OnOverflowed -= UIScore_OnOverflowed;
        m_stateMachine.CharacterContext.CharacterSkills.AddDamage(1000);
        stopExecuting = true;
    }
    private int overflowLevel = 0;
    private int overflowLimit = 8;
    private bool stopExecuting = false;
    private void UIScore_OnOverflowed()
    {
        if (overflowLevel < overflowLimit)
        {
            Service.Get<IAudioService>().PlaySound("overflow", 0.45f, false);
            m_stateMachine.CharacterContext.CameraController.AddTrauma(5f);
            m_stateMachine.CharacterContext.CharacterBuffTrigger.TriggerBuff(AbilityBuff.Rage);
            switch (overflowLevel)
            {
                case 0:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("MY POWER GROWS!");
                    break;
                case 1:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("I AM UNSTOPPABLE!");
                    break;
                case 2:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("YOU CANNOT CONTAIN ME!");
                    break;
                case 3:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("I AM INFINITE!");
                    break;
                case 4:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("I DONT FEEL SO WELL!");
                    break;
                case 5:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("I'M GETTING TOO BIG");
                    break;
                case 6:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("I'M REACHING MY LIMIT");
                    break;
                default:
                    m_stateMachine.CharacterContext.CharacterSpeech.Say("I MIGHT POP");
                    break;
            }
        }
        overflowLevel += 1;
    }

    public override void Execute()
    {
        if (!initialized || stopExecuting) return;

        if(overflowLevel > overflowLimit)
        {
            Service.Get<IUIService>().UIEndScreen.gameObject.SetActive(true);
            PopPlayer();
        }

        m_stateMachine.CharacterContext.CharacterMotor2D.SetMove(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        m_stateMachine.CharacterContext.CharacterAnimation.Step(Time.deltaTime, m_stateMachine.CharacterContext.CharacterMotor2D.MoveInput, m_stateMachine.CharacterContext.CharacterMotor2D.Speed01, m_stateMachine.CharacterContext.CharacterLookAt2D.IsAiming);

        if (m_stateMachine.CharacterContext.CharacterLookAt2D.IsAiming)
        {
            m_stateMachine.CharacterContext.CharacterLookAt2D.SetLookPointToMouse();
            if (canShoot == false)
            {
                shootTimer += Time.deltaTime;
                if (shootTimer >= shootCooldown)
                {
                    //Service.Get<IAudioService>().PlaySoundRandom("rifle_gun_load");
                    canShoot = true;
                    shootTimer = 0;
                }
            }

            if (canShoot)
            {
                canShoot = false;
                //Service.Get<IAudioService>().PlaySoundRandom("rifle_gun_shoot", 0.15f);

                var speechChance = Random.Range(0, 50);
                switch (speechChance)
                {
                    case 1:
                        m_stateMachine.CharacterContext.CharacterSpeech.Say("Pew pew pew!");
                        break;
                    case 2:
                        m_stateMachine.CharacterContext.CharacterSpeech.Say("Boom.");
                        break;
                    case 3:
                        m_stateMachine.CharacterContext.CharacterSpeech.Say("On target.");
                        break;
                    case 4:
                        m_stateMachine.CharacterContext.CharacterSpeech.Say("Feeding frenzy!");
                        break;
                }
                Service.Get<IAudioService>().PlaySound("fire", 0.08f, false);

                m_stateMachine.CharacterContext.CharacterWeapon.Attack(m_stateMachine);
            }
        }
    }

    public override void Exit()
    {
        initialized = false;
        overflowLevel = 0;
        stopExecuting = false;
        Service.Get<IAudioService>().PauseAudioVisualizer();
    }
}
