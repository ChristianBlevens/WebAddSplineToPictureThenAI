using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    // Called when the object becomes enabled and active
    private void Awake()
    {
        // Put this gameobject in the DontDestroyOnLoad scene
        DontDestroyOnLoad(gameObject);

        // Load the GetImage scene
        SceneManager.LoadScene("GetImage");

        // Destroy this monobehavior after the scene is loaded
        Destroy(this);
    }
}
