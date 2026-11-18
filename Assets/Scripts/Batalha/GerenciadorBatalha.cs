using UnityEngine;

public class GerenciadorBatalha : MonoBehaviour
{
    public static GerenciadorBatalha instancia;

    private void Awake()
    {
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
