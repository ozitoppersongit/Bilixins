using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CarregamentoPartida : MonoBehaviour
{
    [SerializeField] Image barra;
    [SerializeField] TMP_Text texto;
    static CarregamentoPartida instancia;
    static GameObject prefabConfigurado;
    bool preparacaoConcluida;
    Action aoRevelar;

    public static bool EmAndamento => instancia != null;

    public static void ConfigurarPrefab(GameObject prefab)
    {
        prefabConfigurado = prefab;
    }

    public static void Carregar()
    {
        if (EmAndamento) return;
        var prefab = prefabConfigurado != null ? prefabConfigurado : Resources.Load<GameObject>("TelaCarregamento");
        if (prefab == null)
        {
            Debug.LogError("Carregamento: configure Tela Carregamento no MainMenu ou disponibilize Resources/TelaCarregamento.prefab.");
            return;
        }
        var controlador = prefab.GetComponent<CarregamentoPartida>();
        if (controlador == null)
        {
            Debug.LogError("Carregamento: o prefab foi encontrado, mas não possui CarregamentoPartida na raiz. Verifique se há Missing Script.", prefab);
            return;
        }
        if (controlador.barra == null || controlador.texto == null)
        {
            Debug.LogError("Carregamento: configure as referências Barra e Texto no prefab.", prefab);
            return;
        }
        CarregamentoPartida tela = Instantiate(prefab).GetComponent<CarregamentoPartida>();
        tela.StartCoroutine(tela.CarregarCena());
    }

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        barra.fillAmount = 0f;
    }

    IEnumerator CarregarCena()
    {
        Canvas.ForceUpdateCanvases();
        // Deixa a tela chegar à renderização antes de iniciar a operação pesada.
        yield return null;
        AsyncOperation operacao;
        try { operacao = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single); }
        catch (Exception erro)
        {
            Debug.LogError("Carregamento: não foi possível abrir Game. " + erro.Message);
            Cancelar();
            yield break;
        }
        if (operacao == null) { Cancelar(); yield break; }
        while (!operacao.isDone)
        {
            barra.fillAmount = Mathf.Min(0.9f, operacao.progress);
            yield return null;
        }
        barra.fillAmount = 0.9f;
        while (!preparacaoConcluida) yield return null;
        barra.fillAmount = 1f;
        yield return null;

        Action continuar = aoRevelar;
        aoRevelar = null;
        instancia = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
        continuar?.Invoke();
    }

    public static void PreparacaoConcluida()
    {
        if (instancia != null) instancia.preparacaoConcluida = true;
    }

    public static void AoRevelar(Action acao)
    {
        if (instancia != null) instancia.aoRevelar += acao;
        else acao?.Invoke();
    }

    public static IEnumerator AguardarConclusao()
    {
        while (EmAndamento) yield return null;
    }

    public static void Cancelar()
    {
        if (instancia == null) return;
        CarregamentoPartida tela = instancia;
        instancia = null;
        tela.aoRevelar = null;
        tela.StopAllCoroutines();
        tela.gameObject.SetActive(false);
        Destroy(tela.gameObject);
    }

    void LateUpdate()
    {
        float largura = Mathf.Min(320f, Mathf.Max(0f, ((RectTransform)transform).rect.width - 48f));
        barra.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, largura);
        texto.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, largura);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
