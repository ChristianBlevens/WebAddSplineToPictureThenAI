// This minimal MonoBehaviour handles the Update calls for animation
using UnityEngine;

public class LightAnimationUpdater : MonoBehaviour
{
    private ChristmasLight light;

    public void Initialize(ChristmasLight christmasLight)
    {
        light = christmasLight;
    }

    private void Update()
    {
        if (light != null)
        {
            light.UpdateAnimation(Time.time);
        }
    }
}