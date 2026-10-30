using System;
using BoletoNetCore.Extensions;
using static System.String;

namespace BoletoNetCore
{
    [CarteiraCodigo("121")]
    internal class BancoSofisaCarteira121 : ICarteira<BancoSofisa>
    {
        internal static Lazy<ICarteira<BancoSofisa>> Instance { get; } = new Lazy<ICarteira<BancoSofisa>>(() => new BancoSofisaCarteira121());

        private BancoSofisaCarteira121()
        {

        }

        public void FormataNossoNumero(Boleto boleto)
        {
            boleto.CarteiraImpressaoBoleto = "121";

            // O nosso número não pode ser em branco.
            if (IsNullOrWhiteSpace(boleto.NossoNumero))
                throw new Exception("Nosso Número não informado.");

            if (boleto.NossoNumero.Length > 10)
                throw new Exception($"Nosso Número ({boleto.NossoNumero}) deve conter até 10 dígitos.");

            var contaBancaria = boleto.Banco.Beneficiario.ContaBancaria;

            boleto.NossoNumero = boleto.NossoNumero.PadLeft(10, '0');
            boleto.NossoNumeroDV = (contaBancaria.Agencia + boleto.Carteira + boleto.NossoNumero).CalcularDVDaycoval();
            boleto.NossoNumeroFormatado = $"{contaBancaria.Agencia}{contaBancaria.DigitoAgencia}/{boleto.Carteira}/{boleto.NossoNumero}-{boleto.NossoNumeroDV}";
            boleto.ImprimirMensagemInstrucao = true;
            boleto.ImprimirValoresAuxiliares = false;
        }

        public string FormataCodigoBarraCampoLivre(Boleto boleto)
        {
            var contaBancaria = boleto.Banco.Beneficiario.ContaBancaria;

            return $"{contaBancaria.Agencia}{contaBancaria.CarteiraPadrao}{contaBancaria.OperacaoConta}{boleto.NossoNumero}{boleto.NossoNumeroDV}";
        }
    }
}
