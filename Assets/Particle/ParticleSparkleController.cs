using UnityEngine;

public class ParticleSparkleController : MonoBehaviour
{
    [SerializeField] private bool reverse = false;
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 1f;
    [Header("Alpha")]
    [SerializeField] private float alphaOffset = 0.6f;
    [SerializeField] private float alphaAmplitude = 0.4f;
    [SerializeField] private float alphaSpeed = 1f;    
    [Header("Scale")]
    [SerializeField] private float scaleOffset = 5f;
    [SerializeField] private float scaleAmplitude = 0.5f;
    [SerializeField] private float scaleSpeed = 1f;


    
    SpriteRenderer sr;
    Color c;
    

    void Awake()
    {
        transform.Rotate(0, 0, Random.Range(0f, 90f));
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * scaleOffset * 8f;
    }

    void Update()
    {
        UpdateRotation();
        UpdateAlpha();
        UpdateScale();
    }

    private void UpdateRotation()
    {
        int direction = reverse ? -1 : 1;
        transform.Rotate(0, 0, direction * 90f * Time.deltaTime * rotationSpeed);
    }

    private void UpdateAlpha()
    {   
        int direction = reverse ? -1 : 1;
        float alpha = alphaOffset + Mathf.Sin(Time.time * alphaSpeed) * alphaAmplitude * direction;
        c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    private void UpdateScale()
    {
        int direction = reverse ? -1 : 1;
        float scale = scaleOffset + Mathf.Sin(Time.time * scaleSpeed) * scaleAmplitude * direction;
        transform.localScale = Vector3.one * scale;
    }

    public void SetColor(Color color)
    {
        c = color;
        sr.color = c;
    }

    public void Appear()
    {
        sr.enabled = true;
        transform.localScale = Vector3.zero;
    }

    public void Disappear()
    {
        sr.enabled = false;
    }
}
