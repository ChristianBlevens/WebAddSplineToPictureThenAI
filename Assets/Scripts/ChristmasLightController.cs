using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChristmasLightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public RawImage targetImage;
    [SerializeField] private RectTransform splineLayerRect;
    [SerializeField] private GameObject lightPrefab;
    [SerializeField] private Material splineMaterial;

    [Header("Edge Detection Settings")]
    [SerializeField] private float edgeDetectionThreshold = 0.1f;
    [SerializeField] private float snapDistance = 20f;
    [SerializeField] private bool visualizeEdges = false;

    [Header("Light Settings")]
    [SerializeField] public float lightSpacing = 20f;
    [SerializeField] public float lightScale = 1f;
    [SerializeField] public bool animateLights = true;
    [SerializeField] public float animationSpeed = 1f;
    [SerializeField]
    private Color[] lightColors = new Color[] {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta
    };

    [Header("Spline Settings")]
    [SerializeField] private int splineSegments = 100;
    [SerializeField] private float splineWidth = 2f;
    [SerializeField] private Color splineColor = Color.white;
    [SerializeField] private float depthInfluence = 1f;
    [SerializeField] private float maximumDepthVariation = 0.5f;

    // Components
    private EdgeDetectionUtility edgeDetector;
    private SplineEditor splineEditor;
    private ChristmasLightGenerator lightGenerator;

    // Data
    private Texture2D sourceTexture;
    private List<EdgeDetectionUtility.ImageLine> detectedLines = new List<EdgeDetectionUtility.ImageLine>();

    private void Awake()
    {
        // Initialize components
        edgeDetector = new EdgeDetectionUtility();
        edgeDetector.SetThreshold(edgeDetectionThreshold);

        // Create spline editor
        GameObject splineEditorObj = new GameObject("Spline Editor");
        splineEditorObj.transform.SetParent(transform);
        splineEditor = splineEditorObj.AddComponent<SplineEditor>();

        // Create light generator
        GameObject lightGenObj = new GameObject("Light Generator");
        lightGenObj.transform.SetParent(transform);
        lightGenerator = lightGenObj.AddComponent<ChristmasLightGenerator>();

        // Initialize editor and light generator
        splineEditor.Initialize(targetImage, splineLayerRect, lightGenerator, splineMaterial, splineColor, splineWidth);
        lightGenerator.SetSettings(lightPrefab, lightSpacing, lightScale, animateLights, animationSpeed, lightColors);
    }

    private void Start()
    {
        // If target image is set, process it
        if (targetImage != null && targetImage.texture != null)
        {
            ProcessImage();
        }
    }

    public void SetTargetImage(RawImage image)
    {
        targetImage = image;
        ProcessImage();
    }

    public void ProcessImage()
    {
        if (targetImage == null || targetImage.texture == null) return;

        // Get readable texture
        sourceTexture = GetReadableTexture((Texture2D)targetImage.texture);

        // Detect edges
        Texture2D edgeTexture = edgeDetector.DetectEdges(sourceTexture);
        detectedLines = edgeDetector.DetectLines(edgeTexture);
        edgeDetector.EstimateDepth(detectedLines, sourceTexture.height);

        // Pass lines to spline editor
        splineEditor.SetEdgeLines(detectedLines, snapDistance);

        // Visualize detected edges if needed
        if (visualizeEdges)
        {
            Texture2D visualTexture = edgeDetector.VisualizeLines(sourceTexture, detectedLines);
            targetImage.texture = visualTexture;
        }
    }

    private Texture2D GetReadableTexture(Texture2D source)
    {
        // Create a temporary RenderTexture
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        // Copy the source texture to the render texture
        Graphics.Blit(source, renderTexture);

        // Create a readable texture
        Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);

        // Store the active render texture
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;

        // Read pixels from render texture to the readable texture
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();

        // Restore the active render texture
        RenderTexture.active = previous;

        // Release the temporary render texture
        RenderTexture.ReleaseTemporary(renderTexture);

        return readableTexture;
    }
}