using UnityEngine;

public class CharacterContext : MonoBehaviour, IRaycastHit
{
    public TeamType TeamType = TeamType.Neutral;
    public CharacterSkills CharacterSkills;
    public CharacterMotor2D CharacterMotor2D;
    public CharacterLookAt2D CharacterLookAt2D;
    public CharacterRaycast2D CharacterRaycast2D;
    public CharacterEffects CharacterEffects;
    public CharacterAnimation CharacterAnimation;
    public CharacterInventory CharacterInventory;
    public CharacterSpeech CharacterSpeech;
    public CharacterWeapon CharacterWeapon;
    public CharacterBuffTrigger CharacterBuffTrigger;
    public PaperDollContext PaperDoll;

    //private Vector3 currentScale = new Vector3(0.05f,0.05f,0.05f);

    //public Vector3 CurrentScale
    //{
    //    get => currentScale;
    //    set => currentScale = value;
    //}

    public CameraController CameraController => cameraController;
    public TeamType Team => TeamType;

    private CameraController cameraController;
    private HitInfo prevHitInfo;
    public HitInfo PrevHitInfo => prevHitInfo;

    private void Awake()
    {
        cameraController = FindAnyObjectByType<CameraController>();
    }

    private void Start()
    {
        switch(TeamType)
        {
            case TeamType.Neutral:
                break;
            case TeamType.Player:
                Service.Get<IPlayerService>().AddPlayer(this);
                break;
            case TeamType.Enemy:
                Service.Get<INpcService>().AddNpc(this);
                break;
        }
        PaperDoll.CacheDefaultSortOrder();
    }

    public void OnRaycastHit(RaycastHit2D hit, Vector2 raydir, int damage)
    {
        var dmg = damage;

        var result = Random.Range(0, 6);

        // If next frame is death, roll for chance to pop head or show gore
        if (CharacterSkills.GetSkill(SkillType.Health).GetCurrentWorkValue() - dmg <= 0)
        {

        }

        Service.Get<IAudioService>().PlaySound("impact", 0.55f, false);

        //Service.Get<IAudioService>().PlaySoundRandom("flesh_impact_group");

        // Almost always play voice 
        if (result > 0 || result < 4)
        {
            //Service.Get<IAudioService>().PlaySoundRandom("voice_human_hurt");
        }

        CharacterSkills.AddDamage(dmg); 

        //Calculate reasonable knockback
        var knockBack = Mathf.Clamp(dmg / 10f, 2f, 5f);
        Debug.Log("[Knockback] Dmg " + dmg + " Knockback force: " + knockBack);
        prevHitInfo = new HitInfo
        {
            point = hit.point,
            normal = hit.normal,
            rayDir = raydir,
            damage = dmg,
            instigator = hit.collider ? hit.collider.gameObject : null,
            surface = Surface2D.Flesh,   // decide via tags/materials
            knockback = knockBack
        };

        CharacterEffects.OnHit(prevHitInfo);
    }
}