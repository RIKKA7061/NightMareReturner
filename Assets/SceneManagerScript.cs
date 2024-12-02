using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    public string sceneName;

    public void JaeWook()
    {
        SceneManager.LoadScene(sceneName);
    }
}
