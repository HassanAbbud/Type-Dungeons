using UnityEngine;
using UnityEngine.UI;

public class BackgroundBinder : MonoBehaviour
{
    [Header("UI References")]
    public Image background;

    private void Awake()
    {
        AssetManager.BindImage(background);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
