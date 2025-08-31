using UnityEngine;
using UnityEngine.UI;    // or TMPro if you’re using TextMeshPro
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class AudioVisualizer_FlexibleBands : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;          // assign your AudioSource
    public int spectrumSize = 512;           // must be power of two (256, 512, 1024, …)
    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;
    [Range(0f, 1f)]
    public float smoothSpeed = 0.5f;         // larger = smoother (more lag)

    // Internals
    private float[] spectrum;                // raw FFT data
    private float[] bandRaw;                 // summed, unsmoothed band values
    private float[] bandSmooth;              // smoothed band values
    private int numBands;                    // = clamp(bands,1,spectrumSize/2)

    [Header("Band Configuration")]
    [Tooltip("How many frequency bands to split into (log-spaced).")]
    public int bands = 8;

    [Header("Gizmos (Editor)")]
    [Tooltip("Toggle to draw vertical bars for each band in the Scene view (Play mode).")]
    public bool drawGizmos = true;
    [Tooltip("Horizontal spacing between each band’s gizmo bar.")]
    public float gizmoWidth = 0.2f;
    [Tooltip("Multiplier for band amplitude → gizmo bar height.")]
    public float gizmoHeightMultiplier = 5f;
    [Tooltip("Color for drawing each band’s gizmo bar.")]
    public Color gizmoColor = Color.green;

    [System.Serializable]
    public struct ParticleAssignment
    {
        public ParticleSystem particleSystem;
        [Min(0)] public int bandStart;      // inclusive
        [Min(0)] public int bandEnd;        // inclusive
        [Tooltip("Scale factor to map summed band value into a 0..1 range. " +
                 "Resulting normalized value is then used to lerp between min/max emission and min/max speed.")]
        public float multiplier;
        public float minEmission;           // emission rate when band is at 0
        public float maxEmission;           // emission rate when band is at maximum (after multiplier)
        public float minSpeed;              // simulationSpeed when band is at 0
        public float maxSpeed;              // simulationSpeed when band is at maximum (after multiplier)
    }

    public enum TextEffectType { Shake, Scale }

    [System.Serializable]
    public struct TextAssignment
    {
        public RectTransform textRect;      // assign the RectTransform of your UI text
        [Min(0)] public int bandStart;      // inclusive
        [Min(0)] public int bandEnd;        // inclusive
        public TextEffectType effectType;
        public float multiplier;            // scale or shake strength
        public bool usePerlin;              // if true, use Perlin noise for smooth shake
        public float perlinSpeed;           // speed of Perlin noise evolution
    }

    // Cache originals for text
    private Dictionary<RectTransform, Vector2> textOriginalAnchoredPos = new Dictionary<RectTransform, Vector2>();
    private Dictionary<RectTransform, Vector3> textOriginalScale = new Dictionary<RectTransform, Vector3>();

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        spectrum = new float[spectrumSize];

        // Ensure numBands ≤ spectrumSize/2
        numBands = Mathf.Clamp(bands, 1, spectrumSize / 2);
        bandRaw = new float[numBands];
        bandSmooth = new float[numBands];

    }

    void Update()
    {
        if (!audioSource.isPlaying) return;

        audioSource.GetSpectrumData(spectrum, 0, fftWindow);
        ComputeBands_LogSpaced();

     
    }


    /// <summary>
    /// Compute log-spaced bands from ~20 Hz → Nyquist (half the output sample rate).
    /// Each band b covers freq range [lowFreq, highFreq] where
    ///    lowFreq  = fMin * (fMax/fMin)^(b    / numBands)
    ///    highFreq = fMin * (fMax/fMin)^((b+1)/ numBands)
    /// Then we convert those freqs → bin indices and sum spectrum[lowBin..highBin].
    /// </summary>
    private void ComputeBands_LogSpaced()
    {
        // Zero out raw sums
        for (int i = 0; i < numBands; i++)
            bandRaw[i] = 0f;

        // Get the output sample rate so Nyquist = sampleRate/2. Unity’s audio uses this.
        float sampleRate = AudioSettings.outputSampleRate;
        float fMax = sampleRate * 0.5f;    // Nyquist
        float fMin = 20f;                  // start at 20 Hz

        // Precompute the log ratio
        float logRatio = Mathf.Log(fMax / fMin);

        for (int b = 0; b < numBands; b++)
        {
            // Exponential interpolation for boundaries
            float fractionLow = (float)b / numBands;
            float fractionHigh = (float)(b + 1) / numBands;
            float lowFreq = fMin * Mathf.Exp(logRatio * fractionLow);
            float highFreq = fMin * Mathf.Exp(logRatio * fractionHigh);

            // Convert freq → bin index.
            // Unity: binIndex = freq / (Nyquist) * (spectrumSize)
            int lowBin = Mathf.FloorToInt(lowFreq / fMax * spectrumSize);
            int highBin = Mathf.FloorToInt(highFreq / fMax * spectrumSize);

            // Clamp to valid indices
            lowBin = Mathf.Clamp(lowBin, 0, spectrumSize - 1);
            highBin = Mathf.Clamp(highBin, 0, spectrumSize - 1);
            if (highBin < lowBin) highBin = lowBin;

            // Sum raw spectrum data from lowBin..highBin
            float sum = 0f;
            for (int i = lowBin; i <= highBin; i++)
                sum += spectrum[i];

            bandRaw[b] = sum;
            // Smooth via Lerp
            bandSmooth[b] = Mathf.Lerp(bandSmooth[b], bandRaw[b], 1f - smoothSpeed);
        }
    }

    /// <summary>
    /// Returns the current smoothed sum of bandSmooth[bandStart..bandEnd].
    /// Call this from other scripts to blend in visualizer data.
    /// </summary>
    public float GetBandValue(int bandStart, int bandEnd)
    {
        if (bandSmooth == null || bandSmooth.Length == 0)
            return 0f;

        int s = Mathf.Clamp(bandStart, 0, numBands - 1);
        int e = Mathf.Clamp(bandEnd, 0, numBands - 1);
        if (e < s) e = s;

        float sum = 0f;
        for (int i = s; i <= e; i++)
            sum += bandSmooth[i];
        return sum;
    }

    // -------------------- GIZMOS --------------------
    void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying || bandSmooth == null)
            return;

        Gizmos.color = gizmoColor;
        Vector3 origin = transform.position;

        // Draw a vertical bar for each band index
        for (int b = 0; b < numBands; b++)
        {
            float xOffset = b * gizmoWidth;
            Vector3 barBase = origin + Vector3.right * xOffset;
            float height = bandSmooth[b] * gizmoHeightMultiplier;
            Vector3 barTop = barBase + Vector3.up * height;

            Gizmos.DrawLine(barBase, barTop);
            Gizmos.DrawCube(barTop, Vector3.one * (gizmoWidth * 0.8f));
        }
    }
}
