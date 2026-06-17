using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    public GameObject loanPopup;
    [SerializeField] private string gameSceneName = "S02_Game_HUST_test1";
    [SerializeField] private string gamePlusSceneName = "S03_Game_square_test1";

    private void Awake()
    {
        BindButtons();
        ClosePopup();
    }

    public void OpenPopup()
    {
        if (loanPopup != null)
            loanPopup.SetActive(true);
    }

    public void ClosePopup()
    {
        if (loanPopup != null)
            loanPopup.SetActive(false);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartGamePlus()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gamePlusSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        Transform root = FindUiRoot();
        if (root == null)
        {
            return;
        }

        BindButton(FindButtonByExactName(root, "btn_NewGame"), StartGame);
        BindButton(FindButtonByExactName(root, "btn_NewGame+"), StartGamePlus);
        BindButton(FindButtonByExactName(root, "btn_QuitGame"), QuitGame);
    }

    private Transform FindUiRoot()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        return canvas == null ? null : canvas.transform;
    }

    private Button FindButtonByExactName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name)
            {
                return children[i].GetComponent<Button>();
            }
        }

        return null;
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
