using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectButton : MonoBehaviour
{
    public CharacterData characterData;
    public string gameSceneName = "GameScene";

    public void Select()
    {
        CharacterSelectionManager.Instance.SelectCharacter(characterData);
        SceneManager.LoadScene(gameSceneName);
    }
}
