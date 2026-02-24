using UnityEngine;

public class MainGameBtnSoundTrigger : MonoBehaviour
{
    public void PlaySound()
    {
        SoundManager.PlaySound(SoundType.BTN_CLICK);
    }
}
