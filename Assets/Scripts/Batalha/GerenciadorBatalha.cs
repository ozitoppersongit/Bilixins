using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GerenciadorBatalha : MonoBehaviour
{
    public static GerenciadorBatalha instancia;

    [Header("Contagem")]
    public int bilixinsDerrotados = 0;
    public int totalBilixins = 4;
    public TextMeshProUGUI counterLabel;

    [Header("Cena de vitória")]
    public string cenaVitoria = "Vitoria";

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

        counterLabel.text = bilixinsDerrotados.ToString() + " / " + totalBilixins.ToString();
    }

    public void BilixinDerrotado()
    {
        bilixinsDerrotados++;

        Debug.Log("Bilixins derrotados: " +
                  bilixinsDerrotados + "/" + totalBilixins);

        counterLabel.text = bilixinsDerrotados.ToString() + " / " + totalBilixins.ToString();

        if (bilixinsDerrotados >= totalBilixins)
        {
            SceneManager.LoadScene(cenaVitoria);
        }
    }
}