using UnityEngine;

// Um Bilixin do mundo 3D pode ter uma animação própria entre "o player encostou" e "a batalha abre".
// O GatilhoDeEncontro chama IniciarAntesDaBatalha, espera o tempo devolvido e só então abre a batalha.
// Se o player fugir ou perder, o gatilho chama VoltarAoNormal para o Bilixin retomar o que fazia.
public interface IAntesDaBatalha
{
    // Começa a animação (ex: ataque virado para o player) e devolve quantos segundos ela dura.
    float IniciarAntesDaBatalha(Transform player);
    void VoltarAoNormal();
}
