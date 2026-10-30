using BoletoNetCore.Exceptions;
using BoletoNetCore.Extensions;
using System.Collections.Generic;

namespace BoletoNetCore
{
    internal sealed partial class BancoSofisa : BancoFebraban<BancoSofisa>, IBanco, IBancoRegistroDetalheEstendido
    {
        public BancoSofisa()
        {
            Codigo = 637;
            Nome = "Banco Sofisa";
            Digito = "8";
            IdsRetornoCnab400RegistroDetalhe = new List<string> { "1" };
            RemoveAcentosArquivoRemessa = true;
        }

        public int TamanhoRegistroDetalheCNAB400 => 444;

        public void FormataBeneficiario()
        {
            var contaBancaria = Beneficiario.ContaBancaria;

            if (!CarteiraFactory<BancoSofisa>.CarteiraEstaImplementada(contaBancaria.CarteiraComVariacaoPadrao))
                throw BoletoNetCoreException.CarteiraNaoImplementada(contaBancaria.CarteiraComVariacaoPadrao);

            contaBancaria.FormatarDados("PAGÁVEL EM TODA REDE BANCÁRIA", "", "", 7);

            Beneficiario.CodigoFormatado = $"{contaBancaria.Agencia}{contaBancaria.DigitoAgencia}/{contaBancaria.Conta.PadLeft(9, '0')}";
        }

        public override string FormatarNomeArquivoRemessa(int numeroSequencial)
        {
            return numeroSequencial.ToString();
        }

        public string GerarMensagemRemessa(TipoArquivo tipoArquivo, Boleto boleto, ref int numeroRegistro)
        {
            return null;
        }
    }
}
