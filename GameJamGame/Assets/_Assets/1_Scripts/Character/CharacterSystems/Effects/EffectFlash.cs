using System.Collections;
using UnityEngine;

public class EffectFlash : MonoBehaviour, IHitEffect
{
    [SerializeField] EffectType effectType = EffectType.Hit;
    [SerializeField] float flashDurationSeconds = 0.025f;
    SpriteRenderer[] _srs;
    WaitForSeconds _wait;
    Coroutine _coroutine = null;

    static readonly int ID_Emission = Shader.PropertyToID("_IsFlashing");
    static MaterialPropertyBlock _mpb;
    
    public EffectType EffectType => effectType;

    void Awake()
    {
        _mpb ??= new MaterialPropertyBlock();
        _wait = new WaitForSeconds(flashDurationSeconds);
    }

    public void OnHit(CharacterContext ctx, in HitInfo info)
    {
        if (_srs == null)
            _srs = ctx.GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < _srs.Length; i++)
        {
            var sr = _srs[i];
            sr.GetPropertyBlock(_mpb);
            _mpb.SetInt(ID_Emission, 1);
            sr.SetPropertyBlock(_mpb);
        }
        if(_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(CoFlashCooldown());
    }


    IEnumerator CoFlashCooldown()
    {
        yield return _wait;
        for (int i = 0; i < _srs.Length; i++)
        {
            var sr = _srs[i];
            sr.GetPropertyBlock(_mpb);
            _mpb.SetInt(ID_Emission, 0);
            sr.SetPropertyBlock(_mpb);
        }
    }

}