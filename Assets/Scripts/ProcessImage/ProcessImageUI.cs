using UnityEngine;
using UnityEngine.UI;

public class ProcessImageUI : MonoBehaviour
{
    // Options
    [SerializeField] private Button privateRetakeButton;
    [SerializeField] private Button privateSaveButton;
    public static Button retakeButton;
    public static Button saveButton;

    // Light Control
    [SerializeField] private Button privateUndoButton;
    [SerializeField] private Button privateClearButton;
    [SerializeField] private Toggle privateAnimateToggle;
    [SerializeField] private Slider privateSpeedSlider;
    public static Button undoButton;
    public static Button clearButton;
    public static Toggle animateToggle;
    public static Slider speedSlider;

    // Called when the object becomes enabled and active
    private void Awake()
    {
        // Set assigned ui to the static ui
        retakeButton = privateRetakeButton;
        saveButton = privateSaveButton;
        undoButton = privateUndoButton;
        clearButton = privateClearButton;
        animateToggle = privateAnimateToggle;
        speedSlider = privateSpeedSlider;

        // Set defualt values
        speedSlider.minValue = 0;
        speedSlider.maxValue = 2;
        speedSlider.value = 1; 
    }
}