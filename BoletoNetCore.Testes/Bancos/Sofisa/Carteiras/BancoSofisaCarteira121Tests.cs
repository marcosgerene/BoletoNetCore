using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BoletoNetCore.Testes
{
    [TestFixture]
    [Category("Banco Sofisa Carteira 121")]
    public class BancoSofisaCarteira121
    {
        readonly IBanco _banco;
        public BancoSofisaCarteira121()
        {
            var contaBancaria = new ContaBancaria
            {
                Agencia = "0001",
                DigitoAgencia = "9",
                Conta = "146645",
                DigitoConta = "7",
                CarteiraPadrao = "121",
                OperacaoConta = "0276049",
                TipoCarteiraPadrao = TipoCarteira.CarteiraCobrancaSimples,
                TipoFormaCadastramento = TipoFormaCadastramento.ComRegistro,
                TipoImpressaoBoleto = TipoImpressaoBoleto.Empresa
            };
            _banco = Banco.Instancia(Bancos.BancoSofisa);
            _banco.Beneficiario = TestUtils.GerarBeneficiario("9192132", "", "190600919213200", contaBancaria);
            _banco.FormataBeneficiario();
        }

        [Test]
        public void Sofisa_121_REM400()
        {
            TestUtils.TestarHomologacao(_banco, TipoArquivo.CNAB400, nameof(BancoSofisaCarteira121), 3, true, "N", 23111889, b => b.NumeroDocumento = "1234567");
        }

        [Test]
        public void Sofisa_121_REM400_header_trailer_com_400_e_detalhe_com_444_posicoes()
        {
            var boletos = TestUtils.GerarBoletos(_banco, 3, "N", 23111889);
            var linhas = GerarRemessa(boletos);

            Assert.That(linhas, Has.Length.EqualTo(5));
            Assert.That(linhas[0], Has.Length.EqualTo(400), "Header");
            Assert.That(linhas[0].Substring(76, 3), Is.EqualTo("637"));
            Assert.That(linhas[0].Substring(79, 15), Is.EqualTo("BANCO SOFISA SA"));
            foreach (var detalhe in linhas.Skip(1).Take(3))
            {
                Assert.That(detalhe, Has.Length.EqualTo(444), "Detalhe");
                Assert.That(detalhe.Substring(107, 1), Is.EqualTo("6"));
                Assert.That(detalhe.Substring(139, 3), Is.EqualTo("637"));
                Assert.That(detalhe.Substring(142, 4), Is.EqualTo("0000"));
                Assert.That(detalhe.Substring(146, 1), Is.EqualTo("0"));
                Assert.That(detalhe.Substring(147, 2), Is.EqualTo("01"));
                Assert.That(detalhe.Substring(149, 1), Is.EqualTo("N"));
                Assert.That(detalhe.Substring(391, 2), Is.EqualTo("05"), "Dias de protesto padrão do banco");
                Assert.That(detalhe.Substring(400, 44), Is.EqualTo("12345678901234567890123456789012345678901234"));
            }
            Assert.That(linhas[4], Has.Length.EqualTo(400), "Trailer");
            Assert.That(linhas[4].Substring(394, 6), Is.EqualTo("000005"));
        }

        [Test]
        public void Sofisa_121_REM400_nosso_numero_da_remessa_com_dv_e_especie_fixa()
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 23111889);
            boletos[0].EspecieDocumento = TipoEspecieDocumento.DS;

            var detalhe = GerarRemessa(boletos)[1];

            Assert.That(detalhe.Substring(62, 11), Is.EqualTo(boletos[0].NossoNumero + boletos[0].NossoNumeroDV));
            Assert.That(detalhe.Substring(147, 2), Is.EqualTo("01"));
        }

        [TestCase("1234567", "", "1234567-01")]
        [TestCase("1234567", "1/3", "1234567-01")]
        [TestCase("1234567", "2/3", "1234567-02")]
        [TestCase("1234567", "12", "1234567-12")]
        [TestCase("15961", "3", "0015961-03")]
        [TestCase("1234567-01", "", "1234567-01")]
        [TestCase("1234567/2", "", "1234567-02")]
        public void Sofisa_121_REM400_seu_numero_nota_com_7_digitos_e_parcela_com_2(string documento, string parcela, string esperado)
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 23111889);
            boletos[0].NumeroDocumento = documento;
            boletos[0].ParcelaInformativo = parcela;

            Assert.That(GerarRemessa(boletos, false)[1].Substring(110, 10), Is.EqualTo(esperado));
        }

        [TestCase("BB000001A", "1/3")]
        [TestCase("12345678", "1")]
        [TestCase("1234567", "A1")]
        [TestCase("1234567", "123")]
        public void Sofisa_121_REM400_seu_numero_invalido_deve_lancar_excecao(string documento, string parcela)
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 23111889);
            boletos[0].NumeroDocumento = documento;
            boletos[0].ParcelaInformativo = parcela;

            Assert.Throws<Exception>(() => GerarRemessa(boletos, false));
        }

        [Test]
        public void Sofisa_121_REM400_mora_diaria_usa_valor_ou_calcula_pelo_percentual()
        {
            var boletos = TestUtils.GerarBoletos(_banco, 2, "N", 23111889);
            boletos[0].ValorJurosDia = 1.23m;
            boletos[1].ValorJurosDia = 0;
            boletos[1].PercentualJurosDia = 0.5m; // título de R$ 200,00 → R$ 1,00 ao dia

            var linhas = GerarRemessa(boletos);

            Assert.That(linhas[1].Substring(160, 13), Is.EqualTo("0000000000123"));
            Assert.That(linhas[2].Substring(160, 13), Is.EqualTo("0000000000100"));
        }

        [Test]
        public void Sofisa_121_REM400_dias_de_protesto_informado_substitui_o_padrao()
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 23111889);
            boletos[0].DiasProtesto = 10;

            Assert.That(GerarRemessa(boletos)[1].Substring(391, 2), Is.EqualTo("10"));
        }

        [Test]
        public void Sofisa_121_REM400_nota_de_servico_completa_chave_com_zeros_a_esquerda()
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 23111889);
            boletos[0].NFe.ChaveAcesso = "A1B2C3D4E";

            var linhas = GerarRemessa(boletos);

            Assert.That(linhas[1].Substring(400, 44), Is.EqualTo("00000000000000000000000000000000000A1B2C3D4E"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("123456789012345678901234567890123456789012345")]
        [TestCase("1234567890-1234")]
        public void Sofisa_121_REM400_sem_chave_de_nfe_valida_deve_lancar_excecao(string chaveAcesso)
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 23111889);
            boletos[0].NFe.ChaveAcesso = chaveAcesso;

            var ex = Assert.Throws<Exception>(() => GerarRemessa(boletos));

            Assert.That(ex.ToString(), Does.Contain("NFe.ChaveAcesso").Or.Contain("inválida"));
        }

        [Test]
        public void Sofisa_121_RET400_deve_ler_detalhe_de_liquidacao()
        {
            var registro = new StringBuilder(new string(' ', 400));
            void Gravar(int posicao, string valor) => registro.Remove(posicao - 1, valor.Length).Insert(posicao - 1, valor);
            Gravar(1, "1");
            Gravar(176, new string('0', 13));
            Gravar(215, new string('0', 65)); // IOF, abatimento, desconto, valor pago e juros
            Gravar(38, "CHAVEPRIMARIA1");
            Gravar(63, "00231118897");
            Gravar(83, "121");
            Gravar(109, "06");
            Gravar(111, "161224");
            Gravar(117, "000001A");
            Gravar(147, "161224");
            Gravar(153, "0000000014150");
            Gravar(166, "637");
            Gravar(174, "01");
            Gravar(176, "0000000000350");
            Gravar(254, "0000000014150");
            Gravar(378, "00");
            Gravar(386, "171224");

            var boleto = new Boleto(_banco);
            ((IBancoCNAB400)_banco).LerDetalheRetornoCNAB400Segmento1(ref boleto, registro.ToString());

            Assert.That(boleto.NossoNumero, Is.EqualTo("0023111889"));
            Assert.That(boleto.NossoNumeroDV, Is.EqualTo("7"));
            Assert.That(boleto.CodigoMovimentoRetorno, Is.EqualTo("06"));
            Assert.That(boleto.DescricaoMovimentoRetorno, Is.EqualTo("Liquidação Normal"));
            Assert.That(boleto.NumeroDocumento, Is.EqualTo("000001A"));
            Assert.That(boleto.ValorTitulo, Is.EqualTo(141.50m));
            Assert.That(boleto.ValorTarifas, Is.EqualTo(3.50m));
            Assert.That(boleto.ValorPagoCredito, Is.EqualTo(141.50m));
            Assert.That(boleto.DataVencimento, Is.EqualTo(new DateTime(2024, 12, 16)));
            Assert.That(boleto.DataCredito, Is.EqualTo(new DateTime(2024, 12, 17)));
        }

        // Nosso número de exemplo do manual: agência 0001, carteira 121, NN 0004309540 → DV 8
        [Test]
        public void Deve_calcular_dv_do_nosso_numero_conforme_manual()
        {
            var boleto = new Boleto(_banco)
            {
                DataVencimento = new DateTime(2024, 6, 29),
                ValorTitulo = 4635.41m,
                NossoNumero = "4309540",
                NumeroDocumento = "000001A",
                EspecieDocumento = TipoEspecieDocumento.DM,
                Pagador = TestUtils.GerarPagador()
            };

            boleto.ValidarDados();

            Assert.That(boleto.NossoNumeroDV, Is.EqualTo("8"));
            Assert.That(boleto.NossoNumeroFormatado, Is.EqualTo("00019/121/0004309540-8"));
        }

        // Boleto modelo da Sofisa (modelo Boleto.pdf)
        [Test]
        public void Deve_criar_boleto_identico_ao_modelo_da_sofisa()
        {
            var boleto = new Boleto(_banco)
            {
                DataVencimento = new DateTime(2024, 6, 29),
                ValorTitulo = 4635.41m,
                NossoNumero = "0023111889",
                NumeroDocumento = "000347767",
                EspecieDocumento = TipoEspecieDocumento.DM,
                Pagador = TestUtils.GerarPagador()
            };

            boleto.ValidarDados();

            Assert.That(boleto.NossoNumeroFormatado, Is.EqualTo("00019/121/0023111889-7"));
            Assert.That(boleto.CodigoBarra.CodigoDeBarras, Is.EqualTo("63797976200004635410001121027604900231118897"));
            Assert.That(boleto.CodigoBarra.LinhaDigitavel, Is.EqualTo("63790.00117 21027.604905 02311.188979 7 97620000463541"));
        }

        [Test]
        public void Nosso_numero_com_mais_de_10_digitos_deve_lancar_excecao()
        {
            var boleto = new Boleto(_banco)
            {
                DataVencimento = new DateTime(2024, 6, 29),
                ValorTitulo = 100m,
                NossoNumero = "12345678901",
                NumeroDocumento = "000001A",
                EspecieDocumento = TipoEspecieDocumento.DM,
                Pagador = TestUtils.GerarPagador()
            };

            Assert.That(() => boleto.ValidarDados(), Throws.Exception);
        }

        public static readonly object[] Casos =
        {
            new object[] { 141.5m, "23111890", "5", "00019/121/0023111890-5", "63798993200000141500001121027604900231118905", "63790.00117 21027.604905 02311.189050 8 99320000014150", 2024, 12, 16 },
            new object[] { 2711.12m, "23111891", "3", "00019/121/0023111891-3", "63791996300002711120001121027604900231118913", "63790.00117 21027.604905 02311.189134 1 99630000271112", 2025, 1, 16 },
            new object[] { 645.39m, "23111892", "1", "00019/121/0023111892-1", "63792104200000645390001121027604900231118921", "63790.00117 21027.604905 02311.189217 2 10420000064539", 2025, 4, 5 },
            new object[] { 1000m, "23111893", "9", "00019/121/0023111893-9", "63791999900001000000001121027604900231118939", "63790.00117 21027.604905 02311.189399 1 99990000100000", 2025, 2, 21 },
            new object[] { 1000m, "23111894", "7", "00019/121/0023111894-7", "63798100000001000000001121027604900231118947", "63790.00117 21027.604905 02311.189472 8 10000000100000", 2025, 2, 22 },
            new object[] { 99.99m, "23111895", "4", "00019/121/0023111895-4", "63793138100000099990001121027604900231118954", "63790.00117 21027.604905 02311.189548 3 13810000009999", 2026, 3, 10 },
            new object[] { 15000.5m, "23111896", "2", "00019/121/0023111896-2", "63791161500015000500001121027604900231118962", "63790.00117 21027.604905 02311.189621 1 16150001500050", 2026, 10, 30 },
            new object[] { 0.01m, "23111897", "0", "00019/121/0023111897-0", "63793131200000000010001121027604900231118970", "63790.00117 21027.604905 02311.189704 3 13120000000001", 2025, 12, 31 },
            new object[] { 86400m, "23111898", "8", "00019/121/0023111898-8", "63798147800086400000001121027604900231118988", "63790.00117 21027.604905 02311.189886 8 14780008640000", 2026, 6, 15 },
        };

        [TestCaseSource(nameof(Casos))]
        public void Deve_criar_boleto_sofisa_121_com_digito_verificador_valido(decimal valorTitulo, string nossoNumero, string digitoVerificador, string nossoNumeroFormatado, string codigoDeBarras, string linhaDigitavel, int ano, int mes, int dia)
        {
            var boleto = GerarBoleto(valorTitulo, nossoNumero, ano, mes, dia);

            Assert.That(boleto.CodigoBarra.DigitoVerificador, Is.EqualTo(linhaDigitavel.Split(' ')[3]), "Dígito Verificador do código de barras");
            Assert.That(boleto.NossoNumeroDV, Is.EqualTo(digitoVerificador), $"Dígito Verificador diferente de {digitoVerificador}");
        }

        [TestCaseSource(nameof(Casos))]
        public void Deve_criar_boleto_sofisa_121_com_nosso_numero_formatado_valido(decimal valorTitulo, string nossoNumero, string digitoVerificador, string nossoNumeroFormatado, string codigoDeBarras, string linhaDigitavel, int ano, int mes, int dia)
        {
            var boleto = GerarBoleto(valorTitulo, nossoNumero, ano, mes, dia);

            Assert.That(boleto.NossoNumeroFormatado, Is.EqualTo(nossoNumeroFormatado), "Nosso número inválido");
        }

        [TestCaseSource(nameof(Casos))]
        public void Deve_criar_boleto_sofisa_121_com_codigo_de_barras_valido(decimal valorTitulo, string nossoNumero, string digitoVerificador, string nossoNumeroFormatado, string codigoDeBarras, string linhaDigitavel, int ano, int mes, int dia)
        {
            var boleto = GerarBoleto(valorTitulo, nossoNumero, ano, mes, dia);

            Assert.That(boleto.CodigoBarra.CodigoDeBarras, Is.EqualTo(codigoDeBarras), "Código de Barra inválido");
        }

        [TestCaseSource(nameof(Casos))]
        public void Deve_criar_boleto_sofisa_121_com_linha_digitavel_valida(decimal valorTitulo, string nossoNumero, string digitoVerificador, string nossoNumeroFormatado, string codigoDeBarras, string linhaDigitavel, int ano, int mes, int dia)
        {
            var boleto = GerarBoleto(valorTitulo, nossoNumero, ano, mes, dia);

            Assert.That(boleto.CodigoBarra.LinhaDigitavel, Is.EqualTo(linhaDigitavel), "Linha digitável inválida");
        }

        private Boleto GerarBoleto(decimal valorTitulo, string nossoNumero, int ano, int mes, int dia)
        {
            var boleto = new Boleto(_banco)
            {
                DataVencimento = new DateTime(ano, mes, dia),
                ValorTitulo = valorTitulo,
                NossoNumero = nossoNumero,
                NumeroDocumento = "000001A",
                EspecieDocumento = TipoEspecieDocumento.DM,
                Pagador = TestUtils.GerarPagador()
            };
            boleto.ValidarDados();
            return boleto;
        }

        private string[] GerarRemessa(Boletos boletos, bool normalizarDocumento = true)
        {
            if (normalizarDocumento)
                foreach (var boleto in boletos)
                    boleto.NumeroDocumento = "1234567";

            using (var stream = new MemoryStream())
            {
                new ArquivoRemessa(boletos.Banco, TipoArquivo.CNAB400, 1).GerarArquivoRemessa(boletos, stream);
                var texto = Encoding.GetEncoding("ISO-8859-1").GetString(stream.ToArray());
                return texto.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }

    // Dados do e-mail de implantação da Sofisa: agência 0045-0, operação 0000339, carteira 121, faixa 0098655554X a 0098715552X
    [TestFixture]
    [Category("Banco Sofisa Carteira 121")]
    public class BancoSofisaCarteira121Agencia0045
    {
        readonly IBanco _banco;
        public BancoSofisaCarteira121Agencia0045()
        {
            var contaBancaria = new ContaBancaria
            {
                Agencia = "0045",
                DigitoAgencia = "0",
                Conta = "339",
                DigitoConta = "1",
                CarteiraPadrao = "121",
                OperacaoConta = "0000339",
                CodigoConvenio = "000000721397CSE637",
                TipoCarteiraPadrao = TipoCarteira.CarteiraCobrancaSimples,
                TipoFormaCadastramento = TipoFormaCadastramento.ComRegistro,
                TipoImpressaoBoleto = TipoImpressaoBoleto.Empresa
            };
            _banco = Banco.Instancia(Bancos.BancoSofisa);
            _banco.Beneficiario = TestUtils.GerarBeneficiario("721397", "", "190600919213200", contaBancaria);
            _banco.Beneficiario.Nome = "AROMA - OLEOS ESSENCIAIS LTDA";
            _banco.FormataBeneficiario();
        }

        public static readonly object[] Casos =
        {
            new object[] { 1500m, "0098655554", "9", "00450/121/0098655554-9", "63792164600001500000045121000033900986555549", "63790.04515 21000.033908 09865.555495 2 16460000150000", 2026, 11, 30 },
            new object[] { 250.75m, "0098655555", "6", "00450/121/0098655555-6", "63797166100000250750045121000033900986555556", "63790.04515 21000.033908 09865.555560 7 16610000025075", 2026, 12, 15 },
            new object[] { 3200.10m, "0098715552", "1", "00450/121/0098715552-1", "63795168700003200100045121000033900987155521", "63790.04515 21000.033908 09871.555216 5 16870000320010", 2027, 1, 10 },
        };

        [TestCaseSource(nameof(Casos))]
        public void Deve_criar_boleto_sofisa_agencia_0045_conforme_faixa_do_banco(decimal valorTitulo, string nossoNumero, string digitoVerificador, string nossoNumeroFormatado, string codigoDeBarras, string linhaDigitavel, int ano, int mes, int dia)
        {
            var boleto = new Boleto(_banco)
            {
                DataVencimento = new DateTime(ano, mes, dia),
                ValorTitulo = valorTitulo,
                NossoNumero = nossoNumero,
                NumeroDocumento = "1234567",
                EspecieDocumento = TipoEspecieDocumento.DM,
                Pagador = TestUtils.GerarPagador()
            };
            boleto.ValidarDados();

            Assert.That(boleto.NossoNumeroDV, Is.EqualTo(digitoVerificador));
            Assert.That(boleto.NossoNumeroFormatado, Is.EqualTo(nossoNumeroFormatado));
            Assert.That(boleto.CodigoBarra.CodigoDeBarras, Is.EqualTo(codigoDeBarras));
            Assert.That(boleto.CodigoBarra.LinhaDigitavel, Is.EqualTo(linhaDigitavel));
        }

        [Test]
        public void Remessa_deve_seguir_o_layout_do_email_de_implantacao()
        {
            var boletos = TestUtils.GerarBoletos(_banco, 1, "N", 98655554);
            var boleto = boletos[0];
            boleto.NossoNumero = "0098655554";
            boleto.NumeroDocumento = "1234567";
            boleto.ParcelaInformativo = "1/1";
            boleto.Banco = _banco;
            boleto.ValidarDados();

            string[] linhas;
            using (var stream = new MemoryStream())
            {
                new ArquivoRemessa(_banco, TipoArquivo.CNAB400, 1).GerarArquivoRemessa(boletos, stream);
                linhas = Encoding.GetEncoding("ISO-8859-1").GetString(stream.ToArray()).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }

            Assert.That(linhas[0].Substring(26, 20), Is.EqualTo("000000721397CSE637  "));
            Assert.That(linhas[0].Substring(46, 30), Is.EqualTo("AROMA - OLEOS ESSENCIAIS LTDA "));
            Assert.That(linhas[0].Substring(76, 3), Is.EqualTo("637"));
            Assert.That(linhas[0].Substring(79, 15), Is.EqualTo("BANCO SOFISA SA"));
            Assert.That(linhas[1].Substring(17, 20), Is.EqualTo("000000721397CSE637  "));
            Assert.That(linhas[1].Substring(62, 11), Is.EqualTo("00986555549"));
            Assert.That(linhas[1].Substring(73, 13), Is.EqualTo(new string(' ', 13)));
            Assert.That(linhas[1].Substring(110, 10), Is.EqualTo("1234567-01"));
            Assert.That(linhas[1], Has.Length.EqualTo(444));
        }
    }
}
