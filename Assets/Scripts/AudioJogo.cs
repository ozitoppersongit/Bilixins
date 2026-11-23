using UnityEngine;
using UnityEngine.Audio;

public class AudioJogo : MonoBehaviour
{
    public static AudioJogo Instance { get; private set; }
    [SerializeField] AudioMixer mixer;
    [SerializeField] AudioMixerGroup musicas;
    [SerializeField] AudioMixerGroup efeitos;
    [SerializeField] AudioClip faixaCena;
    [SerializeField] AudioClip faixaBatalha;
    [SerializeField] AudioClip lixoColetado;
    [SerializeField] AudioClip danoOrganico, danoMetal, danoVidro, danoPlastico, danoPapel;
    [SerializeField] MenuPausa menuPausa;

    AudioSource cena;
    AudioSource combate;
    AudioSource sfx;
    AudioSource tocando;
    BattleScript batalha;
    bool iniciouCena;
    bool iniciouCombate;

    void Awake()
    {
        Instance = this;
        cena = CriarFonte(musicas, faixaCena, true);
        combate = CriarFonte(musicas, faixaBatalha, true);
        sfx = CriarFonte(efeitos, null, false);
    }

    void Start()
    {
        if (mixer != null)
            mixer.SetFloat("SFX", Mathf.Log10(Mathf.Clamp(PlayerPrefs.GetFloat("SFX", 0.75f), 0.0001f, 1f)) * 20f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    AudioSource CriarFonte(AudioMixerGroup grupo, AudioClip faixa, bool loop)
    {
        AudioSource fonte = gameObject.AddComponent<AudioSource>();
        fonte.playOnAwake = false;
        fonte.spatialBlend = 0f;
        fonte.outputAudioMixerGroup = grupo;
        fonte.clip = faixa;
        fonte.loop = loop;
        return fonte;
    }

    public void IniciarBatalha(BattleScript atual)
    {
        batalha = atual;
    }

    public void EncerrarBatalha(BattleScript atual)
    {
        if (batalha != atual) return;
        batalha = null;
        combate.Stop();
        iniciouCombate = false;
        if (tocando == combate) tocando = null;
    }

    void LateUpdate()
    {
        bool suspenso = (menuPausa != null && menuPausa.EstaPausado) ||
            (TransicaoBatalha.instancia != null && TransicaoBatalha.instancia.RetornandoDaDerrota) ||
            (ModoInfinito.Instance != null && ModoInfinito.Instance.Encerrado);
        AudioSource proxima = suspenso ? null : batalha != null && combate.clip != null ? combate : cena;
        if (proxima == tocando) return;
        if (tocando != null) tocando.Pause();
        tocando = proxima;
        if (tocando == null || tocando.clip == null) return;

        if (tocando == combate)
            Tocar(tocando, ref iniciouCombate);
        else
            Tocar(tocando, ref iniciouCena);
    }

    static void Tocar(AudioSource fonte, ref bool iniciou)
    {
        if (iniciou) fonte.UnPause();
        else
        {
            fonte.Play();
            iniciou = true;
        }
    }

    public void TocarColeta()
    {
        if (lixoColetado != null) sfx.PlayOneShot(lixoColetado);
    }

    public void TocarDano(TipoLixo tipo)
    {
        AudioClip clip = tipo switch
        {
            TipoLixo.Organico => danoOrganico,
            TipoLixo.Metal => danoMetal,
            TipoLixo.Vidro => danoVidro,
            TipoLixo.Plastico => danoPlastico,
            TipoLixo.Papel => danoPapel,
            _ => null
        };
        if (clip != null) sfx.PlayOneShot(clip);
    }
}
