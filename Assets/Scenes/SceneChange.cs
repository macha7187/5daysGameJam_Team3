using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public void ChangeScene()
    {
        SeManager.PlayButtonClick();
        SceneManager.LoadScene("Stage1");
    }

}
