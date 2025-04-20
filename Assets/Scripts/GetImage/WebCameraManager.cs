using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;
using TMPro;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine.SceneManagement;

public class WebCameraManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private RawImage videoFeedImage;
    [SerializeField] private Texture2D editorDefaultTexture;
    [SerializeField] private TMP_Text pixelCountText;

    // Textures
    private Texture2D videoTexture;
    private bool isCameraActive = false;
    private int currentWidth;
    private int currentHeight;

    private string lastImage;
    private bool lastLoaded;

    // JavaScript plugin imports
    [DllImport("__Internal")]
    private static extern void InitializeWebCamera(string objectName, string functionName, int width, int height);

    [DllImport("__Internal")]
    private static extern void StartWebCamera();

    [DllImport("__Internal")]
    private static extern void StopWebCamera();

    [DllImport("__Internal")]
    private static extern void ShowFilePickerDialog();

    [DllImport("__Internal")]
    private static extern void UpdateCanvasSize(int width, int height);

    private void Start()
    {
        // Create video texture
        videoTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        // Initialize UI state
        TogglePanels("options");

        // Set up slider listener and update display
        GetImageUI.maxPixelCountSlider.onValueChanged.AddListener(OnPixelCountChanged);
        UpdatePixelCountText();

        // Set up dimensions and textures
        CalculateAndInitializeDimensions();

        // Set up UI button listeners
        SetupButtonListeners();

        // Initialize camera in WebGL build
        InitializeCamera();
    }

    // UI Panel Control
    private void TogglePanels(string panel)
    {
        optionsPanel.SetActive(panel.Equals("options") ? true : false);
        cameraPanel.SetActive(panel.Equals("camera") ? true : false);
    }

    private void CalculateAndInitializeDimensions()
    {
        // Calculate dimensions based on UI and pixel count limit
        CalculateScreenDimensions(out currentWidth, out currentHeight);
        ScaleDimensions(ref currentWidth, ref currentHeight, (int)GetImageUI.maxPixelCountSlider.value);

        // Create video texture
        videoTexture.Reinitialize(currentWidth, currentHeight);

        videoFeedImage.texture = videoTexture;
        GameObject.Find("UnprocessedImage").GetComponent<RawImage>().texture = new Texture2D(currentWidth, currentHeight, TextureFormat.RGBA32, false);
    }

    // Calculate dimensions based on UI and scaling factors
    private void CalculateScreenDimensions(out int width, out int height)
    {
        RectTransform videoRect = videoFeedImage.GetComponent<RectTransform>();
        float rectWidth = videoRect.rect.width;
        float rectHeight = videoRect.rect.height;
        float scaleFactor = mainCanvas != null ? mainCanvas.scaleFactor : 1f;

        width = Mathf.RoundToInt(rectWidth * scaleFactor);
        height = Mathf.RoundToInt(rectHeight * scaleFactor);
    }

    // Scale dimensions to respect max pixel count
    private void ScaleDimensions(ref int width, ref int height, int maxPixels)
    {
        int currentPixels = width * height;
        if (currentPixels > maxPixels)
        {
            float scale = Mathf.Sqrt(maxPixels / (float)currentPixels);
            width = Mathf.RoundToInt(width * scale);
            height = Mathf.RoundToInt(height * scale);
        }
    }

    private void SetupButtonListeners()
    {
        GetImageUI.loadImageButton.onClick.AddListener(OnLoadImageClicked);
        GetImageUI.takePictureButton.onClick.AddListener(OnTakePictureClicked);
        GetImageUI.captureButton.onClick.AddListener(OnCaptureClicked);
        GetImageUI.backButton.onClick.AddListener(OnBackClicked);
    }

    private void InitializeCamera()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log($"Initializing WebGL camera with dimensions: {currentWidth}x{currentHeight}");
        InitializeWebCamera(gameObject.name, "OnWebCameraCallback", currentWidth, currentHeight);
#else
        Debug.Log("Camera functionality only available in WebGL build");
#endif
    }

    // Button Handlers
    private void OnLoadImageClicked()
    {
        Debug.Log("Load image clicked");
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowFilePickerDialog();
#else
        ProcessImageData(Convert.ToBase64String(editorDefaultTexture.EncodeToPNG()), true);
#endif
    }

    private void OnTakePictureClicked()
    {
        Debug.Log("Take picture clicked");

        // Go to the camera panel
        TogglePanels("camera");

#if UNITY_WEBGL && !UNITY_EDITOR
        isCameraActive = true;
        StartWebCamera();
#else
        ProcessImageData(Convert.ToBase64String(editorDefaultTexture.EncodeToPNG()), true);
        Debug.Log("Camera functionality only available in WebGL build");
#endif
    }

    private void OnBackClicked()
    {
        Debug.Log("Back clicked");

        // Stop the camera
        StopCamera();

        // Go to the options panel
        TogglePanels("options");
    }

    private void StopCamera()
    {
        isCameraActive = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        StopWebCamera();
#endif
    }

    public void OnPixelCountChanged(float newValue)
    {
        // Update the pixel count text
        UpdatePixelCountText();

        // Calculate the new dimensions based on the updated pixel count
        CalculateAndInitializeDimensions();

        // Reprocess the last image with the new dimensions
        ProcessImageData(lastImage, lastLoaded);

        // Update WebGL canvas size
#if UNITY_WEBGL && !UNITY_EDITOR
        UpdateCanvasSize(currentWidth, currentHeight);
#endif
    }

    // Pixel Count Management
    private void UpdatePixelCountText()
    {
        pixelCountText.text = $"Max Pixels:\n {(int)GetImageUI.maxPixelCountSlider.value:N0}";
    }

    // JavaScript Communication
    public void OnWebCameraCallback(string message)
    {
        if (message.StartsWith("CAMERA_READY"))
        {
            Debug.Log("Camera is ready");
        }
        else if (message.StartsWith("CAMERA_ERROR:"))
        {
            string errorMsg = message.Substring("CAMERA_ERROR:".Length);
            Debug.LogError("Camera error: " + errorMsg);

            // Go back to options panel on error
            OnBackClicked();
        }
        else if (message.StartsWith("IMAGE_LOADED:"))
        {
            string base64Data = message.Substring("IMAGE_LOADED:".Length);

            // Process the image if loaded from file
            ProcessImageData(base64Data, true);
        }
    }

    public void OnWebCameraFrame(string base64Data)
    {
        if (!isCameraActive) return;
        ProcessImageData(base64Data, false);
    }

    private void ProcessImageData(string base64Data, bool isLoaded)
    {
        try
        {
            lastImage = base64Data;
            lastLoaded = isLoaded;

            byte[] imageData = Convert.FromBase64String(base64Data);

            if (!isLoaded)
            {
                // Video frames are already correctly scaled from the source
                // Just load directly without additional scaling
                videoTexture.LoadImage(imageData);
                videoTexture.Apply();
                return;
            }

            // For loaded images, we need to scale to fit the destination texture
            int targetWidth = videoTexture.width;
            int targetHeight = videoTexture.height;

            // First load into temp texture
            Texture2D tempTexture = new Texture2D(2, 2);
            tempTexture.LoadImage(imageData);

            // Create render texture for scaling to destination size
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tempTexture, rt);

            // Copy scaled pixels to video texture
            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = rt;
            videoTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            RenderTexture.active = prevRT;

            // Apply and clean up
            videoTexture.Apply();
            RenderTexture.ReleaseTemporary(rt);
            Destroy(tempTexture);

            // Open the result panel
            TogglePanels("camera");
        }
        catch (Exception e)
        {
            Debug.LogError("Error processing image data: " + e.Message);
        }
    }

    private void OnCaptureClicked()
    {
        Debug.Log("Capture clicked");

        // Copy video texture to result texture
        Graphics.CopyTexture(videoTexture, GameObject.Find("UnprocessedImage").GetComponent<RawImage>().texture);

        // Go to the process image scene
        SceneManager.LoadScene("ProcessImage");
    }

    private void OnDestroy()
    {
        StopCamera();
    }
}