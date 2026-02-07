using UnityEngine;

/// <summary>
/// This is the base class for all panels
/// </summary>
/// <typeparam name="T"></typeparam>
public class BasePanel<T> : MonoBehaviour where T:MonoBehaviour
{
    /// <summary>
    /// singleton mode
    /// </summary>
    private static T instance;
    public static T Instance => instance;

    protected virtual void Awake()
    {
        // duplication check
        if (instance != null && instance != this as T)
        {
            Destroy(gameObject);
            return;
        }

        instance = this as T;
    }

    // reset instance after switching scene
    protected virtual void OnDestroy()
    {
        if (instance == this as T)
        {
            instance = null;
        }
    }

    /// <summary>
    /// functions in common only
    /// </summary>
    public virtual void ShowMe()
    {
        this.gameObject.SetActive(true);
    }

    public virtual void HideMe()
    {
        this.gameObject.SetActive(false);
    }
}
