using UnityEngine;

public class BgmPengawal : MonoBehaviour
{
    private static BgmPengawal instance;

    void Awake()
    {
        // SISTEM ANTI-DUPLIKAT: Jika musik utama sudah ada yang jalan, hancurkan tiruan yang baru lahir akibat reload scene!
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Jika ini pertama kali game dinyalakan, kunci objek ini agar TIDAK hancur saat scene di-reload
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}