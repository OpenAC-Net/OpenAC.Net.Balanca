# Demo Avalonia

Demo multiplataforma no padrão visual do OpenAC.Net.NFSe.Nacional, usando
Avalonia 12.1.4 e .NET 8. Requer .NET SDK 10 para compilar a solução completa
(incluindo os testes).

```sh
cd src
dotnet run --project OpenAC.Net.Balanca.Demo
```

Em **Configuração**, escolha o protocolo, transporte serial ou TCP e os
parâmetros do equipamento. A porta serial pode ser digitada manualmente
(`COM1` no Windows ou `/dev/ttyUSB0` no Linux). O botão Detectar lista as portas
identificadas. Baud rate, bits, paridade, parada e controle de fluxo devem
coincidir com a configuração da balança.

Use **Conectar**, depois **Ler peso**. **Monitorar transmissão** acompanha os
modelos que transmitem continuamente. **Leituras e retornos** mostra horário,
peso, estado e dados brutos com os caracteres de controle escapados.

**Simulador** funciona sem equipamento: selecione um protocolo na configuração
e informe uma resposta, por exemplo `\x0201234\x03` para Toledo (1,234 kg).
Instabilidade e outros estados são exibidos por nome, sem serem tratados como kg.
A simulação não abre a porta do dispositivo.

**Salvar configuração** persiste as opções no diretório de dados do usuário:
`OpenAC.Net.Balanca/demo.json`. A aplicação carrega esse arquivo ao iniciar.

Em Genérica e Marte, informe a posição inicial (começando em 1) e o tamanho do
campo de peso. Consulte [protocolos](../docs/protocolos.md) para os formatos e
limites da validação.

As operações de comunicação executam fora da thread da interface. Retornos do
monitor são encaminhados pelo Dispatcher do Avalonia. Ao fechar a janela,
a aplicação aguarda a operação em andamento e libera o dispositivo.
