// CharacterGlow.cs
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterGlow : MonoBehaviour
{
    static readonly int ID_Emission = Shader.PropertyToID("_Color");
    SpriteRenderer[] _srs;
    static MaterialPropertyBlock _mpb;

    void Awake()
    {
        _srs = GetComponentsInChildren<SpriteRenderer>(true);
        _mpb ??= new MaterialPropertyBlock();
    }

    public void SetGlow(Color c, float intensity)
    {
        var em = c * Mathf.Max(0f, intensity);
        for (int i = 0; i < _srs.Length; i++)
        {
            var sr = _srs[i];
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(ID_Emission, em);
            sr.SetPropertyBlock(_mpb);
        }
    }
}
