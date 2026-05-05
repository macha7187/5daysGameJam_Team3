using UnityEngine;
using UnityEngine.SceneManagement;

public class T6Change : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("T6");
    }

}

