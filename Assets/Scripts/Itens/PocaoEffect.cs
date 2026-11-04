using UnityEngine;

[CreateAssetMenu(fileName = "Efeito Pocao", menuName = "Bilixin/Efeitos/Pocao")]
public class PocaoEffect : ItemEffect
{
    [Header("Cura")]
    public int quantidadeCura = 30;

    public override bool Aplicar(BattleScript batalha)
    {
        if (batalha == null)
            return false;

        return batalha.RecuperarHpJogador(quantidadeCura);
    }
}