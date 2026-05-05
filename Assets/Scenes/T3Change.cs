using UnityEngine;
using UnityEngine.SceneManagement;

public class T3Change : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("T3");
    }

}

