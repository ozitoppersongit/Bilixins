using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GerenciadorBatalha : MonoBehaviour
{
    public static GerenciadorBatalha instancia;

    [Header("Contagem")]
    private Missoes m;
    public int lixoColetado;
    public int totalLixos;
    public TextMeshProUGUI counterLabel;

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
 
        m = FindAnyObjectByType<Missoes>();
    }

    private void Update()
    {
        counterLabel.text = m.coletados.ToString() + " / " + m.total.ToString();
    }
}