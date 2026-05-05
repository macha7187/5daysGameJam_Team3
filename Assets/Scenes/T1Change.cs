using UnityEngine;
using UnityEngine.SceneManagement;

public class T1Change : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("T1");
    }

}

        
