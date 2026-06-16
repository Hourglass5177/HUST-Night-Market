using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public GameObject loanPopup;

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
}