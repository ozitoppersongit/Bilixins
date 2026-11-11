// Qualquer Bilixin do mundo 3D pode ter uma animação própria de derrota: é só colocar um script que implemente isto.
// O GatilhoDeEncontro.Vencer() chama IniciarDerrota() e só destrói o inimigo depois do tempo devolvido.
public interface IDerrotaAnimada
{
    // Começa a animação de derrota e devolve quantos segundos o inimigo ainda deve ficar no mapa antes de ser destruído.
    float IniciarDerrota();
}
