using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleScript : MonoBehaviour
{
    public BilixinsData bilixinJogador;
    public BilixinsData bilixinInimigo;
    public Image spriteJogador;
    public Image spriteInimigo;
    
    public float power;

    [SerializeField] TextMeshProUGUI status;

    public float CalcularDano(
    BilixinsData atacante,
    BilixinsData defensor,
    float power)
{
    return ((((2f * atacante.nivel / 5f) + 2f) * power *
        ((float)atacante.ataque / defensor.defesa)) / 50f) + 2f;
}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteJogador.sprite = bilixinJogador.spr;
        spriteInimigo.sprite = bilixinInimigo.spr;
        
        status.text = "Um bilixin selvagem apareceu!";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
