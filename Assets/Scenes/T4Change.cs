using UnityEngine;
using UnityEngine.SceneManagement;

public class T4Change : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("T4");
    }

}

