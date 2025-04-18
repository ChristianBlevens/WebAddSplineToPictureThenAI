using UnityEngine;
using UnityEngine.UI;

public class GetImageUI : MonoBehaviour
{
    // Options Panel
    [SerializeField] private Button privateLoadImageButton;
    [SerializeField] private Button privateTakePictureButton;
    public static Button loadImageButton;
    public static Button takePictureButton;

    // Camera Panel
    [SerializeField] private Button privateBackButton;
    [SerializeField] private Button privateCaptureButton;
    [SerializeField] private Slider privateMaxPixelCountSlider;
    public static Button backButton;
    public static Button captureButton;
    public static Slider maxPixelCountSlider;

    // Called when the object becomes enabled and active
    private void Awake()
    {
        // Set assigned ui to the static ui
        loadImageButton = privateLoadImageButton;
        takePictureButton = privateTakePictureButton;
        backButton = privateBackButton;
        captureButton = privateCaptureButton;
        maxPixelCountSlider = privateMaxPixelCountSlider;

        // Set defualt values
        maxPixelCountSlider.minValue = 40960;
        maxPixelCountSlider.maxValue = 2000000;
        maxPixelCountSlider.value = 1000000; 
    }
}