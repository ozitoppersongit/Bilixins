using UnityEngine;

public class GerenciadorBatalha : MonoBehaviour
{
    public static GerenciadorBatalha instancia;

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
