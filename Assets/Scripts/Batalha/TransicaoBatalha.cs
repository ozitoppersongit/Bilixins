using System.Collections;
using UnityEngine;
using TMPro;

public class TransicaoBatalha : MonoBehaviour
{
    public static TransicaoBatalha instancia;

    [Header("Bolinha")]
    public RectTransform bolinha;

    [Header("Configuração")]
    public float duracao = 0.5f;
    [SerializeField] GameObject telaDerrota;
    [SerializeField] TMP_Text textoTelaDerrota;

    public bool RetornandoDaDerrota { get; private set; }
    public bool TemTelaDerrota => telaDerrota != null && textoTelaDerrota != null;

    private void Awake()
    {
        instancia = this;
        Canvas canvas = bolinha.GetComponentInParent<Canvas>();
        if (canvas != null) canvas.rootCanvas.sortingOrder = 200;

        bolinha.localScale = Vector3.zero;
    }

    void OnDestroy()
    {
        if (instancia != this) return;
        if (RetornandoDaDerrota) Time.timeScale = 1f;
        instancia = null;
    }

    public void MostrarDerrota(BattleScript batalha, GatilhoDeEncontro encontro, CameraSeguirPlayer camera)
    {
        RetornandoDaDerrota = true;
        Time.timeScale = 0f;
        Cutscenes cutscene = encontro.CutsceneResponsavel;
        if (cutscene != null) cutscene.InterromperParaDerrota();
        StartCoroutine(ExecutarDerrota(batalha, encontro, camera, cutscene));
    }

    IEnumerator ExecutarDerrota(BattleScript batalha, GatilhoDeEncontro encontro, CameraSeguirPlayer camera, Cutscenes cutscene)
    {
        yield return Abrir();
        GameObject tela = Instantiate(telaDerrota);
        tela.GetComponent<Canvas>().sortingOrder = 150;
        TMP_Text mensagem = System.Array.Find(tela.GetComponentsInChildren<TMP_Text>(true),
            texto => texto.name == textoTelaDerrota.name);
        batalha.gameObject.SetActive(false);
        Destroy(batalha.gameObject);
        yield return Fechar();

        for (float tempo = 0f; tempo < 3f; tempo += Time.unscaledDeltaTime)
        {
            int pontos = 1 + Mathf.FloorToInt(tempo / 0.35f) % 3;
            if (mensagem != null) mensagem.text = "Retornando à missão" + new string('.', pontos);
            yield return null;
        }

        yield return Abrir();
        Destroy(tela);
        bool retornou = cutscene != null
            ? cutscene.RetornarAposDerrota(encontro)
            : encontro != null && encontro.ReposicionarAposDerrota();
        if (!retornou)
        {
            MapaGerador mapa = FindAnyObjectByType<MapaGerador>();
            if (mapa != null && mapa.PlayerInstanciado != null)
            {
                GatilhoDeEncontro.ReposicionarJogador(mapa.PlayerInstanciado.transform, mapa.PoseInicialJogador);
            }
        }

        if (cutscene == null && camera != null) camera.enabled = true;
        yield return Fechar();
        if (encontro != null) encontro.ProtegerAposRetorno();
        RetornandoDaDerrota = false;
        Time.timeScale = 1f;
    }

    public IEnumerator Abrir()
    {
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso = tempo / duracao;

            bolinha.localScale = Vector3.Lerp(
                Vector3.zero,
                Vector3.one * 50f,
                progresso
            );

            yield return null;
        }

        bolinha.localScale = Vector3.one * 50f;
    }

    public IEnumerator Fechar()
    {
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;

            float progresso = tempo / duracao;

            bolinha.localScale = Vector3.Lerp(
                Vector3.one * 50f,
                Vector3.zero,
                progresso
            );

            yield return null;
        }

        bolinha.localScale = Vector3.zero;
    }

public void FecharDepoisDaVitoria()
{
    StartCoroutine(FecharDepoisDaVitoriaCoroutine());
}

private IEnumerator FecharDepoisDaVitoriaCoroutine()
{
    yield return null;

    yield return StartCoroutine(Fechar());
}

public void FecharDepoisDaDerrota()
{
    StartCoroutine(FecharDepoisDaDerrotaCoroutine());
}

private IEnumerator FecharDepoisDaDerrotaCoroutine()
{
    yield return null;

    yield return StartCoroutine(Fechar());
}

    public void FecharDepoisDaFuga()
{
    StartCoroutine(FecharDepoisDaFugaCoroutine());
}

private IEnumerator FecharDepoisDaFugaCoroutine()
{
    // Espera a destruição da batalha acontecer
    yield return null;

    yield return StartCoroutine(Fechar());
}
}
