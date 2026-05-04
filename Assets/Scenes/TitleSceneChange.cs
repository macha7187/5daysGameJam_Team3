using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneChange : MonoBehaviour
{
    public void ChangeScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

}
