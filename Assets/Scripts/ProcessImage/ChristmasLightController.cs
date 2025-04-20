using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ChristmasLightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage targetImage;
    [SerializeField] private RawImage lineImage;
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
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField]
    private Color[] lightColors = new Color[] {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta
    };

    [Header("Spline Settings")]
    [SerializeField] private float splineWidth = 2f;
    [SerializeField] private Color splineColor = Color.white;

    // Components
    private EdgeDetectionUtility edgeDetector;
    private SplineManager splineManager;
    private LightManager lightManager;

    // Data
    private Texture2D sourceTexture;
    private Texture2D lineTexture;

    private void Awake()
    {
        // Get source image
        GameObject UnprocessedImage = GameObject.Find("UnprocessedImage");
        if (UnprocessedImage == null)
            targetImage.texture = (Texture2D)Resources.Load("DefaultHouse");
        else
            targetImage.texture = UnprocessedImage.GetComponent<RawImage>().texture;

        // Initialize components
        edgeDetector = new EdgeDetectionUtility();
        edgeDetector.SetThreshold(edgeDetectionThreshold);

        lightManager = new LightManager(lightPrefab, lightSpacing, lightScale, animationSpeed, lightColors);

        // Initialize spline manager with correct dependencies
        splineManager = new SplineManager(
            targetImage,
            splineLayerRect,
            splineMaterial,
            splineColor,
            splineWidth,
            snapDistance,
            playerInput,
            lightManager);
    }

    private void Start()
    {
        // Set up UI callbacks
        SetupUICallbacks();

        ProcessImage();
    }

    private void SetupUICallbacks()
    {
        ProcessImageUI.undoButton.onClick.AddListener(UndoLastSpline);

        ProcessImageUI.clearButton.onClick.AddListener(ClearAllSplines);

        ProcessImageUI.speedSlider.onValueChanged.AddListener((value) => lightManager.UpdateAnimationSpeed(value));

        ProcessImageUI.animateToggle.onValueChanged.AddListener((isOn) => lightManager.SetAnimationEnabled(isOn));

        ProcessImageUI.retakeButton.onClick.AddListener(RetakeImage);

        ProcessImageUI.saveButton.onClick.AddListener(SaveImage);
    }

    public void ProcessImage()
    {
        if (targetImage == null || targetImage.texture == null) return;

        // Get readable texture
        sourceTexture = TextureUtility.GetReadableTexture((Texture2D)targetImage.texture);

        // Process the image to get line texture with depth
        lineTexture = edgeDetector.ProcessImageToLineTexture(sourceTexture);

        // Pass the line texture to the spline manager
        splineManager.SetLineTexture(lineTexture);

        // Visualize detected edges if needed
        if (visualizeEdges && lineImage != null)
        {
            lineImage.texture = lineTexture;
        }
    }

    public void UndoLastSpline()
    {
        splineManager.RemoveLastSpline();
    }

    public void ClearAllSplines()
    {
        splineManager.ClearAllSplines();
        lightManager.ClearAllLights();
    }

    public void SetSnapDistance(float distance)
    {
        snapDistance = distance;
        splineManager.SetSnapDistance(distance);
    }

    public void SetLightSpacing(float spacing)
    {
        lightSpacing = spacing;
        lightManager.SetLightSpacing(spacing);
    }

    public void SetLightScale(float scale)
    {
        lightScale = scale;
        lightManager.SetLightScale(scale);
    }

    public void RetakeImage()
    {
        SceneManager.LoadScene("GetImage");
    }

    public void SaveImage()
    {

    }
}