using UnityEngine;

[AddComponentMenu("Bloom Rock Puzzle/Flow Visual Animator")]
public class FlowVisualAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Vector2 localMotion = Vector2.zero;
    [SerializeField] private float motionSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.15f;
    [SerializeField] private float phaseOffset;

    private Color baseColor = Color.white;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;

        if (targetRenderer != null)
        {
            baseColor = targetRenderer.color;
        }
    }

    public void Configure(SpriteRenderer renderer, Color color, Vector2 motion, float speed, float pulse, float phase)
    {
        targetRenderer = renderer;
        baseColor = color;
        localMotion = motion;
        motionSpeed = speed;
        pulseAmount = pulse;
        phaseOffset = phase;
        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;

        Apply(0f);
    }

    private void Update()
    {
        Apply(Time.time);
    }

    private void Apply(float time)
    {
        float cycle = Mathf.Repeat(time * Mathf.Max(0.01f, motionSpeed) + phaseOffset, 1f);
        float pulse = 1f + Mathf.Sin((time * Mathf.Max(0.01f, motionSpeed) + phaseOffset) * Mathf.PI * 2f) * pulseAmount;

        transform.localPosition = baseLocalPosition + (Vector3)(localMotion * (cycle - 0.5f));
        transform.localScale = baseLocalScale * Mathf.Max(0.01f, pulse);

        if (targetRenderer == null)
        {
            return;
        }

        Color color = baseColor;
        float fadeInOut = Mathf.Sin(cycle * Mathf.PI);
        color.a *= Mathf.Lerp(0.45f, 1f, fadeInOut);
        targetRenderer.color = color;
    }
}
