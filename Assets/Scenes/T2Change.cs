using UnityEngine;
using UnityEngine.SceneManagement;

public class T2Change : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("T2");
    }

}


