using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    public abstract bool Aplicar(BattleScript batalha);
}