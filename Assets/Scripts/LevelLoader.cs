using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    private void Start()
    {
        LoadLatestLevel();
    }

    private void LoadLatestLevel()
    {
        // Fetch the latest unlocked level, defaulting to 1 if none is found
        int latestLevel = PlayerPrefs.GetInt("currentLevel", 1);
        // Construct the scene name using the level number
        string sceneToLoad = "Level" + latestLevel;

        // Load the scene
        SceneManager.LoadScene(sceneToLoad);
    }
}
