using UnityEngine;
using UnityEngine.UI;

public class IconeVolume : MonoBehaviour
{
    [Header("Volume Sons")]
    [SerializeField] private Slider sliderSons;
    [SerializeField] private Image iconeSons;
    [SerializeField] private Sprite somNormal;
    [SerializeField] private Sprite somMutada;

    [Header("Volume Diálogos")]
    [SerializeField] private Slider sliderDialogos;
    [SerializeField] private Image iconeDialogos;
    [SerializeField] private Sprite dialogosNormal;
    [SerializeField] private Sprite dialogosMutados;

    void Update()
{
    // Sons
    Debug.Log("Sons: " + sliderSons.value);

    if (sliderSons.value <= 0.1f)
        iconeSons.sprite = somMutada;
    else
        iconeSons.sprite = somNormal;

    // Diálogos
    Debug.Log("Diálogos: " + sliderDialogos.value);

    if (sliderDialogos.value <= 0.1f)
        iconeDialogos.sprite = dialogosMutados;
    else
        iconeDialogos.sprite = dialogosNormal;
}
}
