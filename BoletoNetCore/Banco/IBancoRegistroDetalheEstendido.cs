namespace BoletoNetCore
{
    /// <summary>
    /// Banco cujo registro de detalhe da remessa CNAB400 tem mais de 400 posições (ex.: Sofisa, com 444).
    /// Header, trailer e retorno continuam com 400 posições.
    /// </summary>
    public interface IBancoRegistroDetalheEstendido
    {
        int TamanhoRegistroDetalheCNAB400 { get; }
    }
}
