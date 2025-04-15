using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class WebCameraManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Button loadImageButton;
    [SerializeField] private Button takePictureButton;
    [SerializeField] private Button captureButton;
    [SerializeField] private Button backButton;
    [SerializeField] private RawImage videoFeedImage;
    [SerializeField] private RawImage resultImage;

    [Header("Settings")]
    [SerializeField] private int imageWidth = 640;
    [SerializeField] private int imageHeight = 480;

    // Textures for storing camera feed and result
    private Texture2D videoTexture;
    private Texture2D resultTexture;
    private bool isCameraActive = false;

    // Import JavaScript functions
    [DllImport("__Internal")]
    private static extern void InitializeWebCamera(string objectName, string functionName, int width, int height);

    [DllImport("__Internal")]
    private static extern void StartWebCamera();

    [DllImport("__Internal")]
    private static extern void StopWebCamera();

    [DllImport("__Internal")]
    private static extern void CaptureWebCameraImage();

    [DllImport("__Internal")]
    private static extern void ShowFilePickerDialog();

    private void Start()
    {
        // Initialize UI
        optionsPanel.SetActive(true);
        cameraPanel.SetActive(false);
        resultPanel.SetActive(false);

        // Create textures for video feed and result image
        videoTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        resultTexture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);

        // Assign textures to RawImages
        videoFeedImage.texture = videoTexture;
        resultImage.texture = resultTexture;

        // Setup buttons
        loadImageButton.onClick.AddListener(OnLoadImageClicked);
        takePictureButton.onClick.AddListener(OnTakePictureClicked);
        captureButton.onClick.AddListener(OnCaptureClicked);
        backButton.onClick.AddListener(OnBackClicked);

        // Initialize WebCamera in WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Initializing WebGL camera");
        InitializeWebCamera(gameObject.name, "OnWebCameraCallback", imageWidth, imageHeight);
#endif
    }

    private void OnLoadImageClicked()
    {
        Debug.Log("Load image clicked");
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowFilePickerDialog();
#else
        Debug.Log("Load image functionality only available in WebGL build");
#endif
    }

    private void OnTakePictureClicked()
    {
        Debug.Log("Take picture clicked");
        optionsPanel.SetActive(false);
        cameraPanel.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        isCameraActive = true;
        StartWebCamera();
#else
        Debug.Log("Camera functionality only available in WebGL build");
        // For testing in editor
        StartCoroutine(SimulateCameraInEditor());
#endif
    }

    private void OnCaptureClicked()
    {
        Debug.Log("Capture clicked");
#if UNITY_WEBGL && !UNITY_EDITOR
        CaptureWebCameraImage();
#else
        // For testing in editor, just use the current videoTexture as result
        resultTexture.SetPixels(videoTexture.GetPixels());
        resultTexture.Apply();

        cameraPanel.SetActive(false);
        resultPanel.SetActive(true);
        isCameraActive = false;
#endif
    }

    private void OnBackClicked()
    {
        Debug.Log("Back clicked");
        resultPanel.SetActive(false);
        cameraPanel.SetActive(false);
        optionsPanel.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        isCameraActive = false;
        StopWebCamera();
#endif
    }

    // Receives callbacks from JavaScript
    public void OnWebCameraCallback(string message)
    {
        Debug.Log("Camera callback: " + (message.Length > 50 ? message.Substring(0, 50) + "..." : message));

        if (message.StartsWith("CAMERA_READY"))
        {
            Debug.Log("Camera is ready");
        }
        else if (message.StartsWith("CAMERA_ERROR:"))
        {
            string errorMsg = message.Substring("CAMERA_ERROR:".Length);
            Debug.LogError("Camera error: " + errorMsg);
            OnBackClicked();
        }
        else if (message.StartsWith("IMAGE_CAPTURED:") || message.StartsWith("IMAGE_LOADED:"))
        {
            string prefix = message.StartsWith("IMAGE_CAPTURED:") ? "IMAGE_CAPTURED:" : "IMAGE_LOADED:";
            string base64Data = message.Substring(prefix.Length);

            ProcessImageData(base64Data, true);
        }
    }

    // Receives video frames from JavaScript
    public void OnWebCameraFrame(string base64Data)
    {
        if (!isCameraActive) return;

        ProcessImageData(base64Data, false);
    }

    private void ProcessImageData(string base64Data, bool isCapture)
    {
        try
        {
            byte[] imageData = Convert.FromBase64String(base64Data);

            // Determine which texture to update
            Texture2D targetTexture = isCapture ? resultTexture : videoTexture;

            // Load image data into texture
            targetTexture.LoadImage(imageData);
            targetTexture.Apply();

            // If this is a captured image, show result panel
            if (isCapture)
            {
                isCameraActive = false;
                cameraPanel.SetActive(false);
                resultPanel.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
                StopWebCamera();
#endif
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error processing image data: " + e.Message);
        }
    }

    // For testing in Unity Editor
    private IEnumerator SimulateCameraInEditor()
    {
        // Create a test pattern
        Color[] colors = new Color[imageWidth * imageHeight];
        for (int y = 0; y < imageHeight; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                if ((x / 50) % 2 == (y / 50) % 2)
                    colors[y * imageWidth + x] = Color.black;
                else
                    colors[y * imageWidth + x] = new Color(0.8f, 0.8f, 0.8f);

                // Add a moving element
                int centerX = imageWidth / 2;
                int centerY = imageHeight / 2;
                float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                if (distance < 50)
                    colors[y * imageWidth + x] = Color.red;
            }
        }

        // Simulate camera frames
        while (isCameraActive)
        {
            // Rotate colors for animation effect
            Color lastColor = colors[colors.Length - 1];
            for (int i = colors.Length - 1; i > 0; i--)
            {
                colors[i] = colors[i - 1];
            }
            colors[0] = lastColor;

            videoTexture.SetPixels(colors);
            videoTexture.Apply();

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnDestroy()
    {
        isCameraActive = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        StopWebCamera();
#endif
    }

#if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        isCameraActive = false;
    }
#endif
}