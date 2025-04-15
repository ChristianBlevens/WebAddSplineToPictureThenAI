using UnityEngine;
using UnityEngine.UI;

public class ChristmasLightUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button loadImageButton;
    [SerializeField] private Button detectEdgesButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Toggle animateToggle;
    [SerializeField] private Slider spacingSlider;
    [SerializeField] private Slider scaleSlider;
    [SerializeField] private Slider speedSlider;

    [Header("References")]
    [SerializeField] private ChristmasLightController mainController;

    private void Start()
    {
        if (mainController == null)
        {
            mainController = FindFirstObjectByType<ChristmasLightController>();
        }

        // Set up button listeners
        if (loadImageButton != null)
        {
            loadImageButton.onClick.AddListener(OpenImageFileBrowser);
        }

        if (detectEdgesButton != null)
        {
            detectEdgesButton.onClick.AddListener(mainController.ProcessImage);
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearAll);
        }

        // Set up toggle and slider listeners
        if (animateToggle != null)
        {
            animateToggle.onValueChanged.AddListener(SetAnimationEnabled);
        }

        if (spacingSlider != null)
        {
            spacingSlider.onValueChanged.AddListener(SetLightSpacing);
        }

        if (scaleSlider != null)
        {
            scaleSlider.onValueChanged.AddListener(SetLightScale);
        }

        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(SetAnimationSpeed);
        }
    }

    private void OpenImageFileBrowser()
    {
        // This would typically use a native file browser plugin
        // For Unity Editor, we can use EditorUtility.OpenFilePanel
        // For builds, you'd need a file browser plugin

#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select House Image", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            LoadImageFromPath(path);
        }
#else
        Debug.Log("File browser not implemented for builds. Add a file browser plugin.");
#endif
    }

    private void LoadImageFromPath(string path)
    {
        // Load the texture from the file
        byte[] fileData = System.IO.File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        // Create a RawImage if needed
        if (mainController.targetImage == null)
        {
            Debug.LogError("Target image not assigned in ChristmasLightController");
            return;
        }

        // Set the texture
        mainController.targetImage.texture = texture;

        // Process the image
        mainController.ProcessImage();
    }

    private void ClearAll()
    {
        // Find the light generator
        ChristmasLightGenerator lightGen = FindFirstObjectByType<ChristmasLightGenerator>();
        if (lightGen != null)
        {
            lightGen.ClearAllLights();
        }

        // Find and destroy all splines
        ChristmasLightSpline[] splines = FindObjectsByType<ChristmasLightSpline>(FindObjectsSortMode.None);
        foreach (ChristmasLightSpline spline in splines)
        {
            Destroy(spline.gameObject);
        }
    }

    private void SetAnimationEnabled(bool enabled)
    {
        // Update settings in controller
        if (mainController != null)
        {
            mainController.animateLights = enabled;
        }

        // Apply to existing lights
        ChristmasLightAnimation[] animators = FindObjectsByType<ChristmasLightAnimation>(FindObjectsSortMode.None);
        foreach (ChristmasLightAnimation animator in animators)
        {
            animator.enabled = enabled;
        }
    }

    private void SetLightSpacing(float value)
    {
        if (mainController != null)
        {
            mainController.lightSpacing = value;
        }
    }

    private void SetLightScale(float value)
    {
        if (mainController != null)
        {
            mainController.lightScale = value;
        }
    }

    private void SetAnimationSpeed(float value)
    {
        if (mainController != null)
        {
            mainController.animationSpeed = value;
        }

        // Apply to existing lights
        ChristmasLightAnimation[] animators = FindObjectsByType<ChristmasLightAnimation>(FindObjectsSortMode.None);
        foreach (ChristmasLightAnimation animator in animators)
        {
            animator.SetAnimationProperties(value, animator.GetComponent<ChristmasLightAnimation>().timeOffset);
        }
    }
}