using UnityEngine;

public class KeepAlive : MonoBehaviour
{
    private static KeepAlive instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(this.gameObject); 
    }
}