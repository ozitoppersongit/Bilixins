using UnityEngine;
using UnityEngine.UI;

public class OpcoesMenu : MonoBehaviour
{
    [Header("Câmera")]
    [SerializeField] Slider sensibilidadeCamera;
    [SerializeField] Toggle inverterEixoY;

    void Start()
    {
        if (sensibilidadeCamera != null)
        {
            sensibilidadeCamera.minValue = 0.25f;
            sensibilidadeCamera.maxValue = 3f;
            sensibilidadeCamera.SetValueWithoutNotify(CameraSeguirPlayer.SensibilidadeOpcao);
            sensibilidadeCamera.onValueChanged.AddListener(v => CameraSeguirPlayer.SensibilidadeOpcao = v);
        }

        if (inverterEixoY != null)
        {
            inverterEixoY.SetIsOnWithoutNotify(CameraSeguirPlayer.InverterYOpcao);
            inverterEixoY.onValueChanged.AddListener(v => CameraSeguirPlayer.InverterYOpcao = v);
        }
    }

    public void Voltar()
    {
        PlayerPrefs.Save();
        Destroy(gameObject);
    }
}
