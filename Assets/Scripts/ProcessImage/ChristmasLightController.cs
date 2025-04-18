using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ChristmasLightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage targetImage;
    [SerializeField] private RawImage LineImage;
    [SerializeField] private RectTransform splineLayerRect;
    [SerializeField] private GameObject lightPrefab;
    [SerializeField] private Material splineMaterial;
    [SerializeField] private PlayerInput playerInput;

    [Header("Edge Detection Settings")]
    [SerializeField] private float edgeDetectionThreshold = 0.1f;
    [SerializeField] private float snapDistance = 20f;
    [SerializeField] private bool visualizeEdges = false;

    [Header("Light Settings")]
    [SerializeField] private float lightSpacing = 20f;
    [SerializeField] private float lightScale = 1f;
    [SerializeField] private bool animateLights = true;
    [SerializeField] private float animationSpeed = 1f;
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
    private Texture2D lineTexture;

    private void Awake()
    {
        GameObject rawImage = GameObject.Find("GottenImage");

        if (rawImage == null)
            targetImage.texture = new Texture2D(1000, 1000, TextureFormat.RGBA32, false);
        else
            targetImage.texture = rawImage.GetComponent<RawImage>().texture;

        // Initialize components
        edgeDetector = new EdgeDetectionUtility();
        edgeDetector.SetThreshold(edgeDetectionThreshold);

        // Get components
        splineEditor = GameObject.Find("RawImage").GetComponent<SplineEditor>();
        lightGenerator = GetComponent<ChristmasLightGenerator>();

        // Initialize editor and light generator
        splineEditor.Initialize(targetImage, lineTexture, splineLayerRect, lightGenerator, splineMaterial, splineColor, splineWidth, playerInput);
        lightGenerator.SetSettings(lightPrefab, lightSpacing, lightScale, animateLights, animationSpeed, lightColors);
    }

    private void Start()
    {
        ProcessImage();
    }

    public void ProcessImage()
    {
        if (targetImage == null || targetImage.texture == null) return;

        // Get readable texture
        sourceTexture = GetReadableTexture((Texture2D)targetImage.texture);

        // Process the image to get line texture with depth
        lineTexture = edgeDetector.ProcessImageToLineTexture(sourceTexture);

        // Pass the line texture to the spline editor
        splineEditor.SetLineTexture(lineTexture);

        // Visualize detected edges if needed
        if (visualizeEdges)
        {
            // Option to show the direct line texture
            LineImage.texture = lineTexture;
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