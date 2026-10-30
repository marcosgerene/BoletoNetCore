using System;
using System.Linq;
using System.Text.RegularExpressions;
using BoletoNetCore.Enums;
using static System.String;

namespace BoletoNetCore
{
    partial class BancoSofisa : IBancoCNAB400
    {
        private const int DiasProtestoPadrao = 5;

        #region Remessa

        public string GerarHeaderRemessaCNAB400(ref int numeroArquivoRemessa, ref int numeroRegistroGeral)
        {
            try
            {
                numeroRegistroGeral++;
                var reg = new TRegistroEDI();

                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "0", '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0002, 001, 0, "1", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0003, 007, 0, "REMESSA", ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0010, 002, 0, "01", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0012, 015, 0, "COBRANCA", ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0027, 020, 0, Beneficiario.ContaBancaria.CodigoConvenio, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0047, 030, 0, Beneficiario.Nome, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0077, 003, 0, "637", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0080, 015, 0, "BANCO SOFISA SA", ' ');
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0095, 006, 0, DateTime.Now, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0101, 294, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');

                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar HEADER do arquivo de remessa do CNAB400.", ex);
            }
        }

        public string GerarDetalheRemessaCNAB400(Boleto boleto, ref int numeroRegistroGeral)
        {
            try
            {
                var chaveNFe = FormataChaveNFe(boleto);

                numeroRegistroGeral++;
                var reg = new TRegistroEDI();

                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "1", '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0002, 002, 0, boleto.Banco.Beneficiario.TipoCPFCNPJ("00"), '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0004, 014, 0, boleto.Banco.Beneficiario.CPFCNPJ, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0018, 020, 0, boleto.Banco.Beneficiario.ContaBancaria.CodigoConvenio, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0038, 025, 0, boleto.NumeroControleParticipante, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0063, 011, 0, boleto.NossoNumero + boleto.NossoNumeroDV, '0'); // faixa do banco: 10 dígitos + DV
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0074, 013, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0087, 003, 0, Empty, ' ');

                // Multa: 0 = sem multa, 1 = valor fixo, 2 = percentual
                if (boleto.TipoCodigoMulta == TipoCodigoMulta.Percentual && boleto.PercentualMulta > 0)
                {
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0090, 001, 0, "2", '0');
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0091, 013, 4, boleto.PercentualMulta, '0');
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0104, 002, 0, DiasParaMulta(boleto), '0');
                }
                else if (boleto.TipoCodigoMulta == TipoCodigoMulta.Valor && boleto.ValorMulta > 0)
                {
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0090, 001, 0, "1", '0');
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0091, 013, 2, boleto.ValorMulta, '0');
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0104, 002, 0, DiasParaMulta(boleto), '0');
                }
                else
                {
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0090, 001, 0, "0", '0');
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0091, 013, 2, 0, '0');
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0104, 002, 0, 0, '0');
                }

                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0106, 002, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0108, 001, 0, "6", ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0109, 002, 0, boleto.CodigoMovimentoRetorno, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0111, 010, 0, FormataSeuNumero(boleto), ' ');
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0121, 006, 0, boleto.DataVencimento, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0127, 013, 2, boleto.ValorTitulo, '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0140, 003, 0, "637", '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0143, 004, 0, 0, '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0147, 001, 0, 0, '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0148, 002, 0, "01", '0'); // Duplicata, fixo conforme o banco
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0150, 001, 0, "N", ' ');
                reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0151, 006, 0, boleto.DataEmissao, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0157, 002, 0, "00", '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0159, 002, 0, "00", '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0161, 013, 2, ValorMoraDia(boleto), '0');

                if (boleto.ValorDesconto == 0)
                    reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0174, 006, 0, "0", '0'); // Sem Desconto
                else
                    reg.Adicionar(TTiposDadoEDI.ediDataDDMMAA___________, 0174, 006, 0, boleto.DataDesconto, '0'); // Com Desconto

                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0180, 013, 2, boleto.ValorDesconto, '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0193, 013, 2, boleto.ValorIOF, '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0206, 013, 2, boleto.ValorAbatimento, '0');

                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0219, 002, 0, boleto.Pagador.TipoCPFCNPJ("00"), '0');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0221, 014, 0, boleto.Pagador.CPFCNPJ, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0235, 030, 0, boleto.Pagador.Nome, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0265, 010, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0275, 040, 0, boleto.Pagador.Endereco.FormataLogradouro(40), ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0315, 012, 0, boleto.Pagador.Endereco.Bairro, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0327, 008, 0, boleto.Pagador.Endereco.CEP.Replace("-", ""), '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0335, 015, 0, boleto.Pagador.Endereco.Cidade, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0350, 002, 0, boleto.Pagador.Endereco.UF, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0352, 030, 0, IsNullOrWhiteSpace(boleto.Avalista?.Nome) ? Beneficiario.Nome : boleto.Avalista.Nome, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0382, 004, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0386, 006, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0392, 002, 0, boleto.DiasProtesto > 0 ? boleto.DiasProtesto : DiasProtestoPadrao, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0394, 001, 0, "1", ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0401, 044, 0, chaveNFe, ' ');

                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao gerar DETALHE do arquivo CNAB400 - Registro 1.", ex);
            }
        }

        public string GerarTrailerRemessaCNAB400(int numeroRegistroGeral, decimal valorBoletoGeral, int numeroRegistroCobrancaSimples, decimal valorCobrancaSimples, int numeroRegistroCobrancaVinculada, decimal valorCobrancaVinculada, int numeroRegistroCobrancaCaucionada, decimal valorCobrancaCaucionada, int numeroRegistroCobrancaDescontada, decimal valorCobrancaDescontada)
        {
            try
            {
                numeroRegistroGeral++;
                var reg = new TRegistroEDI();
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0001, 001, 0, "9", '0');
                reg.Adicionar(TTiposDadoEDI.ediAlphaAliEsquerda_____, 0002, 393, 0, Empty, ' ');
                reg.Adicionar(TTiposDadoEDI.ediNumericoSemSeparador_, 0395, 006, 0, numeroRegistroGeral, '0');
                reg.CodificarLinha();
                return reg.LinhaRegistro;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro durante a geração do registro TRAILER do arquivo de REMESSA.", ex);
            }
        }

        /// <summary>
        /// Chave de acesso da NF-e (posições 401-444), obrigatória para o Sofisa.
        /// Nota fiscal de serviço traz o código de verificação (normalmente 9 posições) e é completada com zeros à esquerda.
        /// </summary>
        private static string FormataChaveNFe(Boleto boleto)
        {
            var chave = boleto.NFe?.ChaveAcesso?.Trim();

            if (IsNullOrEmpty(chave))
                throw new Exception($"Chave de acesso da nota fiscal (NFe.ChaveAcesso) é obrigatória para o Banco Sofisa. Boleto: {boleto.NumeroDocumento}.");

            if (chave.Length > 44 || !chave.All(char.IsLetterOrDigit))
                throw new Exception($"Chave de acesso da nota fiscal ({chave}) inválida: deve ter até 44 caracteres alfanuméricos. Boleto: {boleto.NumeroDocumento}.");

            return chave.PadLeft(44, '0');
        }

        private static int DiasParaMulta(Boleto boleto)
        {
            var dias = (boleto.DataMulta.Date - boleto.DataVencimento.Date).Days;
            return Math.Min(Math.Max(dias, 1), 99);
        }

        /// <summary>
        /// Seu Número (111-120): nota com 7 dígitos + "-" + parcela com 2 dígitos (ex.: 1234567-01).
        /// Aceita NumeroDocumento já no formato 1234567-01 / 1234567/01; senão usa os dígitos de NumeroDocumento
        /// e a parcela de ParcelaInformativo (aceita "1" ou "1/3"; vazio = parcela única "01").
        /// </summary>
        private static string FormataSeuNumero(Boleto boleto)
        {
            var documento = (boleto.NumeroDocumento ?? Empty).Trim();

            var completo = Regex.Match(documento, @"^(\d{1,7})[-/](\d{1,2})$");
            if (completo.Success)
                return $"{completo.Groups[1].Value.PadLeft(7, '0')}-{completo.Groups[2].Value.PadLeft(2, '0')}";

            if (!Regex.IsMatch(documento, @"^\d{1,7}$"))
                throw new Exception($"Número do documento ({documento}) inválido para o Banco Sofisa: use a nota fiscal com até 7 dígitos numéricos (ou 1234567-01).");

            var parcelaInformada = (boleto.ParcelaInformativo ?? Empty).Trim();
            var parcela = Regex.Match(parcelaInformada, @"^(\d{1,2})(/\d+)?$");
            if (parcelaInformada.Length > 0 && !parcela.Success)
                throw new Exception($"Parcela ({parcelaInformada}) inválida para o Banco Sofisa: informe até 2 dígitos (ou 1/3).");

            return $"{documento.PadLeft(7, '0')}-{(parcela.Success ? parcela.Groups[1].Value : "1").PadLeft(2, '0')}";
        }

        /// <summary>
        /// Valor diário da mora (161-173). Usa ValorJurosDia; se não informado, calcula a partir de PercentualJurosDia.
        /// </summary>
        private static decimal ValorMoraDia(Boleto boleto)
        {
            if (boleto.ValorJurosDia > 0)
                return boleto.ValorJurosDia;
            return boleto.PercentualJurosDia > 0
                ? Math.Round(boleto.ValorTitulo * boleto.PercentualJurosDia / 100, 2)
                : 0;
        }

        #endregion

        #region Retorno

        public override void LerHeaderRetornoCNAB400(string registro)
        {
            try
            {
                if (registro.Substring(0, 9) != "02RETORNO")
                    throw new Exception("O arquivo não é do tipo \"02RETORNO\"");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler HEADER do arquivo de RETORNO / CNAB 400.", ex);
            }
        }

        public void LerDetalheRetornoCNAB400Segmento1(ref Boleto boleto, string registro)
        {
            try
            {
                // Uso da Empresa
                boleto.NumeroControleParticipante = registro.Substring(37, 25).Trim();

                // Carteira
                boleto.Carteira = registro.Substring(82, 3);
                boleto.TipoCarteira = TipoCarteira.CarteiraCobrancaSimples;

                // Identificação do Título no Banco (10 dígitos + DV)
                boleto.NossoNumero = registro.Substring(62, 10);
                boleto.NossoNumeroDV = registro.Substring(72, 1);
                boleto.NossoNumeroFormatado = $"{boleto.Banco.Beneficiario.ContaBancaria.Agencia}{boleto.Banco.Beneficiario.ContaBancaria.DigitoAgencia}/{boleto.Carteira}/{boleto.NossoNumero}-{boleto.NossoNumeroDV}";

                // Identificação de Ocorrência
                boleto.CodigoMovimentoRetorno = registro.Substring(108, 2);
                boleto.CodigoMotivoOcorrencia = registro.Substring(377, 2);
                boleto.DescricaoMovimentoRetorno = DescricaoOcorrenciaCnab400(boleto.CodigoMovimentoRetorno, boleto.CodigoMotivoOcorrencia);

                // Número do Documento
                boleto.NumeroDocumento = registro.Substring(116, 10).Trim();
                boleto.EspecieDocumento = AjustaEspecieCnab400(registro.Substring(173, 2));

                // Valores do Título
                boleto.ValorTitulo = Convert.ToDecimal(registro.Substring(152, 13)) / 100;
                boleto.ValorTarifas = Convert.ToDecimal(registro.Substring(175, 13)) / 100;
                boleto.ValorOutrasDespesas = 0;
                boleto.ValorIOF = Convert.ToDecimal(registro.Substring(214, 13)) / 100;
                boleto.ValorAbatimento = Convert.ToDecimal(registro.Substring(227, 13)) / 100;
                boleto.ValorDesconto = Convert.ToDecimal(registro.Substring(240, 13)) / 100;
                boleto.ValorPagoCredito = Convert.ToDecimal(registro.Substring(253, 13)) / 100;
                boleto.ValorJurosDia = Convert.ToDecimal(registro.Substring(266, 13)) / 100;
                boleto.ValorOutrosCreditos = 0;

                // Data Ocorrência no Banco
                boleto.DataProcessamento = Utils.ToDateTime(Utils.ToInt32(registro.Substring(110, 6)).ToString("##-##-##"));

                // Data Vencimento do Título
                boleto.DataVencimento = Utils.ToDateTime(Utils.ToInt32(registro.Substring(146, 6)).ToString("##-##-##"));

                // Data do Crédito
                boleto.DataCredito = Utils.ToDateTime(Utils.ToInt32(registro.Substring(385, 6)).ToString("##-##-##"));

                // Registro Retorno
                boleto.RegistroArquivoRetorno = boleto.RegistroArquivoRetorno + registro + Environment.NewLine;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler detalhe do arquivo de RETORNO / CNAB 400.", ex);
            }
        }

        public void LerDetalheRetornoCNAB400Segmento2(ref Boleto boleto, string registro)
        {
            throw new NotImplementedException();
        }

        public void LerDetalheRetornoCNAB400Segmento4(ref Boleto boleto, string registro)
        {
            throw new NotImplementedException();
        }

        public void LerDetalheRetornoCNAB400Segmento7(ref Boleto boleto, string registro)
        {
            throw new NotImplementedException();
        }

        public void LerTrailerRetornoCNAB400(string registro)
        {
        }

        private string DescricaoOcorrenciaCnab400(string codigo, string codigoRejeicao)
        {
            switch (codigo)
            {
                case "01": return "Entrada Aceita na CIP";
                case "02": return "Entrada Confirmada";
                case "03": return "Entrada Rejeitada - " + EntradaDescricaoRejeicaoCnab400(codigoRejeicao);
                case "05": return "Campo Livre Alterado";
                case "06": return "Liquidação Normal";
                case "08": return "Liquidação em Cartório";
                case "09": return "Baixa Automática";
                case "10": return "Baixa por ter sido liquidado";
                case "12": return "Confirma Abatimento";
                case "13": return "Abatimento Cancelado";
                case "14": return "Vencimento Alterado";
                case "15": return "Baixa Rejeitada - " + BaixaDescricaoRejeicaoCnab400(codigoRejeicao);
                case "16": return "Instrução Rejeitada - " + InstrucoesDescricaoRejeicaoCnab400(codigoRejeicao);
                case "19": return "Confirma Recebimento de Ordem de Protesto";
                case "20": return "Confirma Recebimento de Ordem de Sustação";
                case "22": return "Seu Número Alterado";
                case "23": return "Título Enviado para Cartório";
                case "24": return "Confirma Recebimento de Ordem de Não Protestar";
                case "28": return "Débito de Tarifas/Custas - Correspondentes";
                case "40": return "Tarifa de Entrada (debitada na Liquidação)";
                case "43": return "Baixado por ter sido Protestado";
                case "96": return "Tarifa sobre Instruções - Mês Anterior";
                case "97": return "Tarifa sobre Baixas - Mês Anterior";
                case "98": return "Tarifa sobre Entradas - Mês Anterior";
                case "99": return "Tarifa sobre Instruções de Protesto/Sustação - Mês Anterior";
                default: return "";
            }
        }

        private string EntradaDescricaoRejeicaoCnab400(string codigo)
        {
            switch (codigo)
            {
                case "03": return "CEP inválido – Não temos cobrador – Cobrador não Localizado";
                case "04": return "Sigla do Estado inválida";
                case "05": return "Data de Vencimento inválida ou fora do prazo mínimo";
                case "06": return "Código do Banco inválido";
                case "08": return "Nome do sacado não informado";
                case "10": return "Logradouro não informado";
                case "14": return "Registro em duplicidade";
                case "19": return "Data de desconto inválida ou maior que a data de vencimento";
                case "20": return "Valor de IOF não numérico";
                case "21": return "Movimento para título não cadastrado no sistema";
                case "22": return "Valor de desconto + abatimento maior que o valor do título";
                case "25": return "CNPJ ou CPF do sacado inválido (aceito com restrições)";
                case "26": return "Espécie de documento inválida";
                case "27": return "Data de emissão do título inválida";
                case "28": return "Seu número não informado";
                case "29": return "CEP é igual a espaço ou zeros; ou não numérico";
                case "30": return "Valor do título não numérico ou inválido";
                case "36": return "Valor de permanência (mora) não numérico";
                case "37": return "Valor de permanência inconsistente, pois, dentro de um mês, será maior que o valor do título";
                case "38": return "Valor de desconto/abatimento não numérico ou inválido";
                case "39": return "Valor de abatimento não numérico";
                case "42": return "Título já existente em nossos registros. Nosso número não aceito";
                case "43": return "Título enviado em duplicidade nesse movimento";
                case "44": return "Título zerado ou em branco; ou não numérico na remessa";
                case "46": return "Título enviado fora da faixa de Nosso Número, estipulada para o cliente";
                case "51": return "Tipo/Número de Inscrição Sacador/Avalista inválido";
                case "52": return "Sacador/Avalista não informado";
                case "53": return "Prazo de vencimento do título excede ao da contratação";
                case "54": return "Banco informado não é nosso correspondente 140-142";
                case "55": return "Banco correspondente informado não cobra este CEP ou não possui faixas de CEP cadastradas";
                case "56": return "Nosso número no correspondente não foi informado";
                case "57": return "Remessa contendo duas instruções incompatíveis – não protestar e dias de protesto ou prazo para protesto inválido";
                case "58": return "Entradas Rejeitadas – Reprovado no Represamento para Análise";
                case "60": return "CNPJ/CPF do sacado inválido – título recusado";
                case "87": return "Excede prazo máximo entre emissão e vencimento";
                case "AA": return "Serviço de cobrança inválido";
                case "AB": return "Serviço de \"0\" ou \"5\" e banco cobrador <> zeros";
                case "AE": return "Título não possui abatimento";
                case "AI": return "Nossa carteira inválida";
                case "AJ": return "Modalidade com bancos correspondentes inválida";
                case "AL": return "Sacado impedido de entrar nesta cobrança";
                case "AU": return "Data da ocorrência inválida";
                case "AV": return "Valor da tarifa de cobrança inválida";
                case "AX": return "Título em pagamento parcial";
                case "BC": return "Análise gerencial - sacado inválido p/ operação crédito";
                case "BD": return "Análise gerencial - sacado inadimplente";
                case "BE": return "Análise gerencial - sacado difere do exigido";
                case "BF": return "Análise gerencial - vencto excede vencto da operação de crédito";
                case "BG": return "Análise gerencial - sacado com baixa liquidez";
                case "BH": return "Análise gerencial - sacado excede concentração";
                case "CC": return "Valor de IOF incompatível com a espécie documento";
                case "CD": return "Efetivação de protesto sem agenda válida";
                case "CE": return "Título não aceito - pessoa física";
                case "CF": return "Excede prazo máximo da entrada ao vencimento";
                case "CG": return "Título não aceito – por análise gerencial";
                case "CH": return "Título em espera – em análise pelo banco";
                case "CJ": return "Análise gerencial - vencimento do título abaixo prazo curto";
                case "CK": return "Análise gerencial - vencimento do título abaixo prazo longo";
                case "CS": return "Título rejeitado pela checagem de duplicatas";
                case "DA": return "Análise gerencial – Entrada de Título Descontado com limite cancelado";
                case "DB": return "Análise gerencial – Entrada de Título Descontado com limite vencido";
                case "DC": return "Análise gerencial - cedente com limite cancelado";
                case "DD": return "Análise gerencial – cedente é sacado e teve seu limite cancelado";
                case "DE": return "Análise gerencial - apontamento no Serasa";
                case "DG": return "Endereço sacador/avalista não informado";
                case "DH": return "CEP do sacador/avalista não informado";
                case "DI": return "Cidade do sacador/avalista não informado";
                case "DJ": return "Estado do sacador/avalista inválido ou não informado";
                case "DM": return "Cliente sem Código de Flash cadastrado no cobrador";
                case "DN": return "Título Descontado com Prazo ZERO – Recusado";
                case "DP": return "Data de Referência menor que a Data de Emissão do Título";
                case "DT": return "Nosso Número do Correspondente não deve ser informado";
                case "EB": return "HSBC não aceita endereço de sacado com mais de 38 caracteres";
                case "G1": return "Endereço do sacador incompleto (Lei 12.039)";
                case "G2": return "Sacador impedido de movimentar";
                case "G3": return "Concentração de CEP não permitida";
                case "G4": return "Valor do título não permitido";
                case "KE": return "Título possui caracteres inválidos";
                default: return "";
            }
        }

        private string BaixaDescricaoRejeicaoCnab400(string codigo)
        {
            switch (codigo)
            {
                case "05": return "Solicitação de baixa para título já baixado ou liquidado";
                case "06": return "Solicitação de baixa para título não registrado no sistema";
                case "08": return "Solicitação de baixa para título em float";
                default: return "";
            }
        }

        private string InstrucoesDescricaoRejeicaoCnab400(string codigo)
        {
            switch (codigo)
            {
                case "04": return "Data de vencimento não numérica ou inválida";
                case "05": return "Data de vencimento inválida ou fora do prazo mínimo";
                case "14": return "Registro em duplicidade";
                case "19": return "Data de desconto inválida ou maior que a data de vencimento";
                case "20": return "Campo livre não informado";
                case "21": return "Título não registrado no sistema";
                case "22": return "Título baixado ou liquidado";
                case "26": return "Espécie de documento inválida";
                case "27": return "Instrução não aceita, por não ter sido emitida ordem de protesto ao cartório";
                case "28": return "Título tem instrução de cartório ativa";
                case "29": return "Título não tem instrução de carteira ativa";
                case "30": return "Existe instrução de não protestar, ativa para o título";
                case "36": return "Valor de permanência (mora) não numérico";
                case "37": return "Título Descontado – Instrução não permitida para a carteira";
                case "38": return "Valor do abatimento não numérico ou maior que a soma do valor do título + permanência + multa";
                case "39": return "Título em cartório";
                case "40": return "Instrução recusada – Reprovado no Represamento para Análise";
                case "44": return "Título zerado ou em branco; ou não numérico na remessa";
                case "51": return "Tipo/Número de Inscrição Sacador/Avalista inválido";
                case "53": return "Prazo de vencimento do título excede ao da contratação";
                case "57": return "Remessa contendo duas instruções incompatíveis – não protestar e dias de protesto ou prazo para protesto inválido";
                case "AA": return "Serviço de cobrança inválido";
                case "AE": return "Título não possui abatimento";
                case "AG": return "Movimento não permitido – Título à vista ou contra apresentação";
                case "AH": return "Cancelamento de valores inválidos";
                case "AI": return "Nossa carteira inválida";
                case "AK": return "Título pertence a outro cliente";
                case "AU": return "Data da ocorrência inválida";
                case "AY": return "Título deve estar em aberto e vencido para acatar protesto";
                case "CB": return "Título possui protesto efetivado/a efetivar hoje";
                case "CT": return "Título já baixado";
                case "CW": return "Título já transferido";
                case "DO": return "Título em Prejuízo";
                case "99": return "Ocorrência desconhecida na remessa";
                default: return "";
            }
        }

        private TipoEspecieDocumento AjustaEspecieCnab400(string codigoEspecie)
        {
            switch (codigoEspecie)
            {
                case "01": return TipoEspecieDocumento.DM;
                case "02": return TipoEspecieDocumento.NP;
                case "03": return TipoEspecieDocumento.CH;
                case "04": return TipoEspecieDocumento.LC;
                case "05": return TipoEspecieDocumento.RC;
                case "08": return TipoEspecieDocumento.NS;
                case "12": return TipoEspecieDocumento.DS;
                default: return TipoEspecieDocumento.OU;
            }
        }

        #endregion
    }
}
