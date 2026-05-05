using UnityEngine;
using UnityEngine.SceneManagement;

public class T5Change : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("T5");
    }

}


