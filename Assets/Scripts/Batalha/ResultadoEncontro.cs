public enum ResultadoBatalha { EmAndamento, Vitoria, Fuga, Derrota }

// A referência continua válida quando o inimigo é removido da cena.
public sealed class ResultadoEncontro
{
    public ResultadoBatalha Resultado { get; private set; }
    public void Concluir(ResultadoBatalha resultado)
    {
        if (Resultado == ResultadoBatalha.EmAndamento) Resultado = resultado;
    }
}
