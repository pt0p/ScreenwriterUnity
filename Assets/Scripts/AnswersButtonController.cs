using UnityEngine;

public class AnswersButtonController : MonoBehaviour
{
    public int btnIdx = -1;

    public void OnClicked()
    {
        foreach (var dc in GameObject.FindObjectsOfType<DialogController>())
        {
            if (dc.DialogOpen) dc.ButtonClicked(btnIdx);
        }
    }
}
